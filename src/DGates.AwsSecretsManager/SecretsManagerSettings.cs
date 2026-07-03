using System;

namespace DGates.AwsSecretsManager
{
    /// <summary>
    /// Configuration for <see cref="SecretsManagerService"/>.
    /// <para>
    /// <b>Credential resolution:</b> if <see cref="AccessKey"/> and <see cref="SecretKey"/>
    /// are not set, the underlying AWS SDK falls back to its standard credential chain in this
    /// order: environment variables (<c>AWS_ACCESS_KEY_ID</c> / <c>AWS_SECRET_ACCESS_KEY</c>),
    /// the <c>&lt;appSettings&gt;</c> keys <c>AWSAccessKey</c> / <c>AWSSecretKey</c> in
    /// web.config or app.config, the shared credentials file (<c>~/.aws/credentials</c>),
    /// and finally the EC2/ECS instance metadata service (IAM role).
    /// For most .NET Framework applications not hosted on AWS, set <see cref="AccessKey"/> and
    /// <see cref="SecretKey"/> explicitly, or populate the web.config <c>&lt;appSettings&gt;</c>
    /// keys so the credential chain can resolve them automatically.
    /// </para>
    /// </summary>
    public class SecretsManagerSettings
    {
        /// <summary>
        /// AWS region the secrets live in, e.g. "us-west-2". If not set, the SDK looks for
        /// the <c>AWS_REGION</c> environment variable or the <c>AWSRegion</c> app.config key.
        /// </summary>
        public string Region { get; set; }

        /// <summary>
        /// Optional explicit AWS access key. If not set, the SDK credential chain resolves
        /// credentials automatically — see the class-level remarks for resolution order.
        /// </summary>
        public string AccessKey { get; set; }

        /// <summary>
        /// Optional explicit AWS secret key. Required when <see cref="AccessKey"/> is set.
        /// If not set, the SDK credential chain resolves credentials automatically.
        /// </summary>
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
