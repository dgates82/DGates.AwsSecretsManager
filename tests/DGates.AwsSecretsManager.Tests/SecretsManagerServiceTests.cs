using System;
using System.Threading;
using System.Threading.Tasks;
using Amazon.Runtime;
using Amazon.SecretsManager;
using Amazon.SecretsManager.Model;
using Microsoft.Extensions.Logging;
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

        [Fact]
        public async Task GetSecretStringAsync_LogsOnFetch()
        {
            var mockClient = new Mock<IAmazonSecretsManager>();
            mockClient
                .Setup(c => c.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(Response("{\"ApiKey\":\"abc123\"}"));
            var mockLogger = new Mock<ILogger>();

            var service = new SecretsManagerService(new SecretsManagerSettings(), mockClient.Object, mockLogger.Object);

            await service.GetSecretStringAsync("myapp/ApiKey");

            VerifyLogged(mockLogger);
        }

        [Fact]
        public async Task GetSecretStringAsync_LogsOnRetryAfterTransientFailure()
        {
            var mockClient = new Mock<IAmazonSecretsManager>();
            mockClient
                .SetupSequence(c => c.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InternalServiceErrorException("transient failure"))
                .ReturnsAsync(Response("{\"ApiKey\":\"abc123\"}"));
            var mockLogger = new Mock<ILogger>();

            var service = new SecretsManagerService(new SecretsManagerSettings(), mockClient.Object, mockLogger.Object);

            var result = await service.GetSecretStringAsync("myapp/ApiKey");

            Assert.Equal("{\"ApiKey\":\"abc123\"}", result);
            VerifyLogged(mockLogger);
        }

        [Fact]
        public async Task GetSecretStringAsync_RetriesOnTooManyRequestsStatusCode()
        {
            var throttled = new AmazonSecretsManagerException(
                "rate exceeded", ErrorType.Unknown, "ThrottlingException", "req-id",
                (System.Net.HttpStatusCode)429);
            var mockClient = new Mock<IAmazonSecretsManager>();
            mockClient
                .SetupSequence(c => c.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(throttled)
                .ReturnsAsync(Response("{\"ApiKey\":\"abc123\"}"));

            var service = new SecretsManagerService(new SecretsManagerSettings(), mockClient.Object);

            var result = await service.GetSecretStringAsync("myapp/ApiKey");

            Assert.Equal("{\"ApiKey\":\"abc123\"}", result);
            mockClient.Verify(
                c => c.GetSecretValueAsync(It.IsAny<GetSecretValueRequest>(), It.IsAny<CancellationToken>()),
                Times.Exactly(2));
        }

        [Fact]
        public void Constructor_NoExplicitCredentialsAndNoCredentialChain_ThrowsInvalidOperationException()
        {
            using (new ClearedAwsCredentialEnvironment())
            {
                var settings = new SecretsManagerSettings { Region = "us-west-2" };

                var ex = Assert.Throws<InvalidOperationException>(() => new SecretsManagerService(settings));
                Assert.Contains("No AWS credentials found", ex.Message);
                Assert.IsType<Amazon.Runtime.AmazonClientException>(ex.InnerException);
            }
        }

        [Fact]
        public void Constructor_ExplicitAccessAndSecretKey_SkipsCredentialChainCheck()
        {
            using (new ClearedAwsCredentialEnvironment())
            {
                var settings = new SecretsManagerSettings
                {
                    Region = "us-west-2",
                    AccessKey = "AKIAFAKEEXAMPLE",
                    SecretKey = "fake-secret-key"
                };

                using (var service = new SecretsManagerService(settings))
                {
                    Assert.NotNull(service);
                }
            }
        }

        /// <summary>
        /// Clears the environment variables and AWS_PROFILE the default credential chain
        /// consults, so credential-resolution tests aren't at the mercy of whatever the host
        /// running the tests happens to have configured. Restores original values on dispose.
        /// </summary>
        private sealed class ClearedAwsCredentialEnvironment : IDisposable
        {
            private static readonly string[] Names =
            {
                "AWS_ACCESS_KEY_ID", "AWS_SECRET_ACCESS_KEY", "AWS_SESSION_TOKEN", "AWS_PROFILE"
            };

            private readonly System.Collections.Generic.Dictionary<string, string> _original =
                new System.Collections.Generic.Dictionary<string, string>();

            public ClearedAwsCredentialEnvironment()
            {
                foreach (var name in Names)
                {
                    _original[name] = Environment.GetEnvironmentVariable(name);
                    Environment.SetEnvironmentVariable(name, null);
                }
            }

            public void Dispose()
            {
                foreach (var name in Names)
                {
                    Environment.SetEnvironmentVariable(name, _original[name]);
                }
            }
        }

        private static void VerifyLogged(Mock<ILogger> mockLogger)
        {
            mockLogger.Verify(
                l => l.Log(
                    It.IsAny<LogLevel>(),
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception, string>>()),
                Times.AtLeastOnce());
        }

        private class TestSecret
        {
            public string ApiKey { get; set; }
        }
    }
}
