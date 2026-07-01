using System;

namespace DGates.AwsSecretsManager.Internal
{
    /// <summary>
    /// Wraps a cached raw secret string with its expiry time.
    /// Internal: typed deserialization happens on read, not on cache storage,
    /// so a single cached entry can serve multiple requested types.
    /// </summary>
    internal sealed class CachedSecret
    {
        public string RawValue { get; }
        public DateTimeOffset ExpiresAt { get; }

        public CachedSecret(string rawValue, DateTimeOffset expiresAt)
        {
            RawValue = rawValue;
            ExpiresAt = expiresAt;
        }

        public bool IsExpired(DateTimeOffset now) => now >= ExpiresAt;
    }
}
