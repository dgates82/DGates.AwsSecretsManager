using System;
using Xunit;

namespace DGates.AwsSecretsManager.Tests
{
    public class SecretsManagerServiceFactoryTests
    {
        [Fact]
        public void Create_NullSettings_ThrowsArgumentNullException()
        {
            var ex = Assert.Throws<ArgumentNullException>(() => SecretsManagerServiceFactory.Create(null));

            Assert.Equal("settings", ex.ParamName);
        }
    }
}
