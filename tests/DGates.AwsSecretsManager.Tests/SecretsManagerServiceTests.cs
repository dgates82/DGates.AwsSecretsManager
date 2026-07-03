using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Moq;
using Xunit;

namespace DGates.AwsSecretsManager.Tests
{
    public class SecretsManagerServiceTests
    {
        private static GetSecretValueResponse Response(string json) =>
            new GetSecretValueResponse { SecretString = json };

        [Fact]
        public async Task GetSecretAsync_DeserializesIntoTypedObject()
        {
            var mockClient = new Mock<IAmazonSecretsManager>();
            mockClient
                .Setup(c => 
                    c.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), 
                        It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response("{\"ApiKey\":\"abc123\"}"));

            var service = new SecretsManagerService(new SecretsManagerSettings(), mockClient.Object);

            var result = await service.GetSecretAsync<TestSecret>("myapp/ApiKey");

            Assert.Equal("abc123", result.ApiKey);
        }

        [Fact]
        public async Task GetSecretStringAsync_UsesCacheOnSecondCall()
        {
            var mockClient = new Mock<IAmazonSecretsManager>();
            mockClient
                .Setup(c => c.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response("{\"ApiKey\":\"abc123\"}"));

            var service = new SecretsManagerService(
                new SecretsManagerSettings { CacheTtl = TimeSpan.FromMinutes(10) },
                mockClient.Object);

            await service.GetSecretStringAsync("myapp/ApiKey");
            await service.GetSecretStringAsync("myapp/ApiKey");

            mockClient.Verify(
                c => c.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [Fact]
        public async Task RefreshSecretAsync_BypassesCache()
        {
            var mockClient = new Mock<IAmazonSecretsManager>();
            mockClient
                .SetupSequence(c => c.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response("{\"ApiKey\":\"first\"}"))
                .ReturnsAsync(Response("{\"ApiKey\":\"second\"}"));

            var service = new SecretsManagerService(new SecretsManagerSettings(), mockClient.Object);

            await service.GetSecretAsync<TestSecret>("myapp/ApiKey");
            var refreshed = await service.RefreshSecretAsync<TestSecret>("myapp/ApiKey");

            Assert.Equal("second", refreshed.ApiKey);
        }

        [Fact]
        public async Task InvalidateCache_ForcesRefetchOnNextCall()
        {
            var mockClient = new Mock<IAmazonSecretsManager>();
            mockClient
                .Setup(c => c.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response("{\"ApiKey\":\"abc123\"}"));

            var service = new SecretsManagerService(new SecretsManagerSettings(), mockClient.Object);

            await service.GetSecretStringAsync("myapp/ApiKey");
            service.InvalidateCache("myapp/ApiKey");
            await service.GetSecretStringAsync("myapp/ApiKey");

            mockClient.Verify(
                c => c.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public async Task GetSecretStringAsync_ThrowsOnNullOrEmptyName()
        {
            var service = new SecretsManagerService(new SecretsManagerSettings(), new Mock<IAmazonSecretsManager>().Object);

            await Assert.ThrowsAsync<ArgumentException>(() => service.GetSecretStringAsync(""));
        }

        private class TestSecret
        {
            public string ApiKey { get; set; }
        }
    }
}
