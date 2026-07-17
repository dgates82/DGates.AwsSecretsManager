using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using DGates.AwsSecretsManager.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Polly;
using Polly.Retry;

namespace DGates.AwsSecretsManager
{
    /// <summary>
    /// Default implementation of <see cref="ISecretsManagerService"/>.
    /// Thread-safe; intended to be registered as a singleton.
    /// </summary>
    public class SecretsManagerService : ISecretsManagerService, IDisposable
    {
        private readonly SecretsManagerSettings _settings;
        private readonly IAmazonSecretsManager _client;
        private readonly ConcurrentDictionary<string, CachedSecret> _cache =
            new ConcurrentDictionary<string, CachedSecret>();
        private readonly ResiliencePipeline<string> _retryPipeline;
        private readonly bool _ownsClient;
        private readonly ILogger _logger;

        /// <summary>
        /// Initializes a new instance using the provided settings, creating and owning an
        /// <see cref="IAmazonSecretsManager"/> client internally. <paramref name="logger"/> is
        /// optional; when omitted, logging is a no-op via <see cref="NullLogger"/>.
        /// </summary>
        public SecretsManagerService(SecretsManagerSettings settings, ILogger logger = null)
            : this(settings, BuildClient(settings), logger)
        {
            _ownsClient = true;
        }

        /// <summary>
        /// Constructor for injecting a pre-configured <see cref="IAmazonSecretsManager"/> client
        /// directly, primarily for testing. <paramref name="logger"/> is optional; when omitted,
        /// logging is a no-op via <see cref="NullLogger"/>.
        /// </summary>
        public SecretsManagerService(SecretsManagerSettings settings, IAmazonSecretsManager client, ILogger logger = null)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _client = client ?? throw new ArgumentNullException(nameof(client));

            _logger = logger ?? NullLogger.Instance;

            _retryPipeline = new ResiliencePipelineBuilder<string>()
                .AddRetry(new RetryStrategyOptions<string>
                {
                    ShouldHandle = new PredicateBuilder<string>()
                        .Handle<AmazonSecretsManagerException>(IsTransient),
                    MaxRetryAttempts = _settings.MaxRetryAttempts,
                    Delay = _settings.RetryBaseDelay,
                    BackoffType = DelayBackoffType.Exponential
                })
                .Build();
        }

        /// <inheritdoc/>
        public async Task<T> GetSecretAsync<T>(string secretName, CancellationToken cancellationToken = default)
            where T : class
        {
            var raw = await GetSecretStringAsync(secretName, cancellationToken).ConfigureAwait(false);
            try
            {
                return Deserialize<T>(raw);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize secret {SecretName} into type {TypeName}"
                    , secretName, typeof(T).Name);
                throw;
            }
        }

        /// <inheritdoc/>
        public async Task<string> GetSecretStringAsync(
            string secretName, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(secretName))
            {
                throw new ArgumentException("Secret name must be provided.", nameof(secretName));
            }

            var now = DateTimeOffset.UtcNow;

            if (_cache.TryGetValue(secretName, out var cached) && !cached.IsExpired(now))
            {
                _logger.LogDebug("Cache hit for secret {SecretName}", secretName);
                return cached.RawValue;
            }

            _logger.LogInformation("Cache miss or expired for secret {SecretName}, fetching", secretName);
            var raw = await FetchRawAsync(secretName, cancellationToken).ConfigureAwait(false);
            _cache[secretName] = new CachedSecret(raw, now + _settings.CacheTtl);
            return raw;
        }

        /// <inheritdoc/>
        public async Task<T> RefreshSecretAsync<T>(string secretName, CancellationToken cancellationToken = default)
            where T : class
        {
            _logger.LogInformation("Refreshing secret {SecretName}, bypassing cache", secretName);
            var raw = await FetchRawAsync(secretName, cancellationToken).ConfigureAwait(false);
            _cache[secretName] = new CachedSecret(raw, DateTimeOffset.UtcNow + _settings.CacheTtl);

            try
            {
                return Deserialize<T>(raw);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to deserialize secret {SecretName} into type {TypeName}"
                    , secretName, typeof(T).Name);
                throw;
            }
        }

        /// <inheritdoc/>
        public void InvalidateCache(string secretName)
        {
            _logger.LogDebug("Invalidating cache for secret {SecretName}", secretName);
            _cache.TryRemove(secretName, out _);
        }

        private async Task<string> FetchRawAsync(string secretName, CancellationToken cancellationToken)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(_settings.LocalJsonFallbackPath))
                {
                    return FetchFromLocalJsonFallback(secretName);
                }

                return await _retryPipeline.ExecuteAsync(async ct =>
                {
                    var response = await _client.GetSecretValueAsync(new GetSecretValueRequest
                    {
                        SecretId = secretName
                    }, ct).ConfigureAwait(false);

                    return response.SecretString;
                }, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to fetch secret {SecretName}", secretName);
                throw;
            }
        }

        private string FetchFromLocalJsonFallback(string secretName)
        {
            _logger.LogInformation("Using local JSON fallback for secret {SecretName}", secretName);
            if (!File.Exists(_settings.LocalJsonFallbackPath))
            {
                throw new FileNotFoundException(
                    $"Local JSON fallback file not found at '{_settings.LocalJsonFallbackPath}'.");
            }

            var json = File.ReadAllText(_settings.LocalJsonFallbackPath);

            var root = JObject.Parse(json);

            if (!root.TryGetValue(secretName, out var token))
            {
                throw new ResourceNotFoundException(
                    $"Secret '{secretName}' not found in local JSON fallback file.");
            }

            return token.ToString(Formatting.None);
        }

        private static T Deserialize<T>(string raw) where T : class
        {
            try
            {
                return JsonConvert.DeserializeObject<T>(raw);
            }
            catch (JsonException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to deserialize secret into type '{typeof(T).Name}'. " +
                    "Verify the secret's JSON shape matches the target type.", ex);
            }
        }

        private static bool IsTransient(AmazonSecretsManagerException ex)
        {
            // Throttling and 5xx-class errors are worth retrying; access/permission
            // and not-found errors are not.
            return ex is InternalServiceErrorException
                || ex is LimitExceededException
                || ex.StatusCode == (System.Net.HttpStatusCode)429;
        }

        private static IAmazonSecretsManager BuildClient(SecretsManagerSettings settings)
        {
            var config = new AmazonSecretsManagerConfig();

            if (!string.IsNullOrWhiteSpace(settings.ServiceUrl))
            {
                config.ServiceURL = settings.ServiceUrl;
                config.UseHttp = settings.ServiceUrl.StartsWith(
                    "http://", StringComparison.OrdinalIgnoreCase);
                if (!string.IsNullOrWhiteSpace(settings.Region))
                {
                    config.AuthenticationRegion = settings.Region;
                }
            }
            else if (!string.IsNullOrWhiteSpace(settings.Region))
            {
                config.RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(settings.Region);
            }

            if (!string.IsNullOrWhiteSpace(settings.AccessKey) &&
                !string.IsNullOrWhiteSpace(settings.SecretKey))
            {
                return new AmazonSecretsManagerClient(settings.AccessKey, settings.SecretKey, config);
            }

            // Falls back to the default AWS credential chain.
            return new AmazonSecretsManagerClient(config);
        }

        /// <summary>
        /// Disposes the underlying <see cref="IAmazonSecretsManager"/> client if it was created
        /// internally; no-op if an external client was injected.
        /// </summary>
        public void Dispose()
        {
            if (_ownsClient)
            {
                _client.Dispose();
            }
        }
    }
}
