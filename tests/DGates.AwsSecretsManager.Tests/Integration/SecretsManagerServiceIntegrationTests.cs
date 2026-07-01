using System.Threading.Tasks;
using Xunit;

namespace DGates.AwsSecretsManager.Tests.Integration
{
    /// <summary>
    /// Requires LocalStack running locally (see docker-compose.yml in the repo root) with
    /// Secrets Manager enabled and the seed script run. Skipped automatically if LocalStack
    /// is unreachable, so this won't break local `dotnet test` runs for contributors without
    /// Docker running. CI explicitly starts LocalStack before invoking this trait.
    /// </summary>
    [Trait("Category", "Integration")]
    public class SecretsManagerServiceIntegrationTests
    {
        private const string LocalStackUrl = "http://localhost:4566";
        private const string SeededSecretName = "test/SampleSecret";

        private static SecretsManagerSettings LocalStackSettings() => new SecretsManagerSettings
        {
            Region = "us-west-2",
            ServiceUrl = LocalStackUrl,
            AccessKey = "test",
            SecretKey = "test"
        };

        [SkippableFact]
        public async Task GetSecretAsync_RetrievesSeededSecretFromLocalStack()
        {
            Skip.IfNot(await IsLocalStackUp(), "LocalStack is not running on localhost:4566.");

            var service = new SecretsManagerService(LocalStackSettings());

            var result = await service.GetSecretAsync<SampleSecret>(SeededSecretName);

            Assert.False(string.IsNullOrEmpty(result.Value));
        }

        private static async Task<bool> IsLocalStackUp()
        {
            try
            {
                using (var http = new System.Net.Http.HttpClient { Timeout = System.TimeSpan.FromSeconds(5) })
                {
                    var response = await http.GetAsync(LocalStackUrl + "/_localstack/health");
                    return response.IsSuccessStatusCode;
                }
            }
            catch
            {
                return false;
            }
        }

        private class SampleSecret
        {
            public string Value { get; set; }
        }
    }
}
