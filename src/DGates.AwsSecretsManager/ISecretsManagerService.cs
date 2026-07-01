using System.Threading;
using System.Threading.Tasks;

namespace DGates.AwsSecretsManager
{
    /// <summary>
    /// Retrieves secrets from AWS Secrets Manager, deserialized into strongly typed objects,
    /// with in-memory caching and automatic refresh-on-expiry.
    /// </summary>
    public interface ISecretsManagerService
    {
        /// <summary>
        /// Retrieves a secret by name and deserializes it into <typeparamref name="T"/>.
        /// Returns a cached value if one exists and has not expired.
        /// </summary>
        Task<T> GetSecretAsync<T>(string secretName, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Retrieves the raw secret string by name, bypassing typed deserialization.
        /// Returns a cached value if one exists and has not expired.
        /// </summary>
        Task<string> GetSecretStringAsync(string secretName, CancellationToken cancellationToken = default);

        /// <summary>
        /// Forces a fresh fetch from AWS Secrets Manager, bypassing and replacing any cached value.
        /// </summary>
        Task<T> RefreshSecretAsync<T>(string secretName, CancellationToken cancellationToken = default) where T : class;

        /// <summary>
        /// Removes a secret from the in-memory cache, if present.
        /// </summary>
        void InvalidateCache(string secretName);
    }
}
