# DGates.AwsSecretsManager

A lightweight, typed wrapper around AWS Secrets Manager for .NET Framework 4.8 applications.

## Why

The official AWS SDK gives you raw `GetSecretValueAsync` calls and string blobs. This library adds:

- **Typed retrieval** — deserialize secrets directly into your own POCOs
- **In-memory caching with TTL** — avoid hammering Secrets Manager on every request
- **Refresh-on-expiry** — transparent re-fetch when a cached secret expires
- **Retry with backoff** — resilient against transient AWS throttling/network errors
- **DI registration extension** — one-line wire-up for `Unity`, `Autofac`, or plain `IServiceCollection`-style containers
- **Optional local JSON fallback** — develop without any AWS account at all

## Status

Early development (`0.1.x`). API surface may shift before `1.0.0`. See [CHANGELOG.md](https://github.com/dgates82/DGates.AwsSecretsManager/blob/main/CHANGELOG.md).

## Install

```sh
dotnet add package DGates.AwsSecretsManager
```

## Quick start

```csharp
var settings = new SecretsManagerSettings
{
    Region = "us-west-2",
    CacheTtl = TimeSpan.FromMinutes(10)
};

var secretsService = new SecretsManagerService(settings);

var apiKey = await secretsService.GetSecretAsync<MyApiKeySecret>("myapp/ApiKey");
```

See the [examples repo](https://github.com/dgates82/DGates.AwsSecretsManager.Examples) for a working console sample, including local development against [LocalStack](https://www.localstack.cloud/) (no real AWS account required). An ASP.NET MVC example is coming soon.

## Local development & testing

This repo includes a `docker-compose.yml` that spins up LocalStack with the Secrets Manager service enabled, plus a seed script for test secrets. See [docs/LOCAL_DEV.md](https://github.com/dgates82/DGates.AwsSecretsManager/blob/main/docs/LOCAL_DEV.md).

## License

MIT — see [LICENSE](https://github.com/dgates82/DGates.AwsSecretsManager/blob/main/LICENSE).

## Contributing

Issues and PRs welcome. See [CONTRIBUTING.md](https://github.com/dgates82/DGates.AwsSecretsManager/blob/main/CONTRIBUTING.md).
