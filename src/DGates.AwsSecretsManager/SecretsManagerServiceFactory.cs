using System;

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
        /// Register the *returned instance* as a singleton in your DI container, e.g.:
        /// <code>
        /// var service = SecretsManagerServiceFactory.Create(settings);
        /// container.RegisterInstance&lt;ISecretsManagerService&gt;(service); // Unity
        /// builder.RegisterInstance(service).As&lt;ISecretsManagerService&gt;().SingleInstance(); // Autofac
        /// </code>
        /// </summary>
        public static ISecretsManagerService Create(SecretsManagerSettings settings)
        {
            if (settings == null) throw new ArgumentNullException(nameof(settings));
            return new SecretsManagerService(settings);
        }
    }
}
