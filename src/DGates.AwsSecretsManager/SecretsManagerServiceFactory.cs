using System;
using Microsoft.Extensions.Logging;

namespace DGates.AwsSecretsManager
{
    /// <summary>
    /// Minimal container-agnostic registration helper. .NET Framework apps commonly use
    /// Unity, Autofac, or Ninject rather than Microsoft.Extensions.DependencyInjection, so this
    /// avoids forcing one container as a dependency. Wire <see cref="ISecretsManagerService"/>
    /// as a singleton in your container of choice using this factory.
    /// </summary>
    public static class SecretsManagerServiceFactory
    {
        /// <summary>
        /// Creates a singleton-ready <see cref="ISecretsManagerService"/> instance from settings.
        /// <paramref name="logger"/> is optional; when omitted, logging is a no-op. Register the
        /// *returned instance* as a singleton in your DI container, e.g.:
        /// <code>
        /// var service = SecretsManagerServiceFactory.Create(settings, logger);
        /// container.RegisterInstance&lt;ISecretsManagerService&gt;(service); // Unity
        /// builder.RegisterInstance(service).As&lt;ISecretsManagerService&gt;().SingleInstance(); // Autofac
        /// </code>
        /// If no explicit <see cref="SecretsManagerSettings.AccessKey"/>/<see cref="SecretsManagerSettings.SecretKey"/>
        /// are set, this validates the AWS SDK's default credential chain immediately and throws
        /// <see cref="InvalidOperationException"/> if no credential source resolves.
        /// </summary>
        public static ISecretsManagerService Create(SecretsManagerSettings settings, ILogger logger = null)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            return new SecretsManagerService(settings, logger);
        }
    }
}
