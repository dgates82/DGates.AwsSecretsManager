using System;

namespace DGates.AwsSecretsManager
{
    /// <summary>
    /// Configuration for <see cref="SecretsManagerService"/>.
    /// </summary>
    public class SecretsManagerSettings
    {
        /// <summary>AWS region the secrets live in, e.g. "us-west-2".</summary>
        public string Region { get; set; }

        /// <summary>
        /// Optional explicit access key. If null, falls back to the default AWS credential
        /// chain (environment variables, IAM role, shared credentials file, etc).
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>Optional explicit secret key. See <see cref="AccessKey"/> remarks.</summary>
        public string SecretKey { get; set; }

        /// <summary>
        /// How long a successfully retrieved secret stays valid in the in-memory cache
        /// before it is considered stale and re-fetched. Default: 10 minutes.
        /// </summary>
        public TimeSpan CacheTtl { get; set; } = TimeSpan.FromMinutes(10);

        /// <summary>Maximum retry attempts on transient AWS errors. Default: 3.</summary>
        public int MaxRetryAttempts { get; set; } = 3;

        /// <summary>Base delay for exponential backoff between retries. Default: 200ms.</summary>
        public TimeSpan RetryBaseDelay { get; set; } = TimeSpan.FromMilliseconds(200);

        /// <summary>
        /// Optional override of the AWS Secrets Manager service endpoint, e.g.
        /// "http://localhost:4566" when running against LocalStack. Leave null for real AWS.
        /// </summary>
        public string ServiceUrl { get; set; }

        /// <summary>
        /// When set, secrets are read from this local JSON file instead of AWS, keyed by
        /// secret name. Intended for local development without any AWS account.
        /// Example file shape: { "myapp/ApiKey": { "ApiKey": "local-dev-value" } }
        /// </summary>
        public string LocalJsonFallbackPath { get; set; }
    }
}
