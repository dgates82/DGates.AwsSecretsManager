# DGates.AwsSecretsManager

[![CI](https://github.com/dgates82/DGates.AwsSecretsManager/actions/workflows/ci.yml/badge.svg)](https://github.com/dgates82/DGates.AwsSecretsManager/actions/workflows/ci.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=dgates_awssecretsmanager&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=dgates_awssecretsmanager)
[![CodeQL](https://github.com/dgates82/DGates.AwsSecretsManager/actions/workflows/codeql.yml/badge.svg)](https://github.com/dgates82/DGates.AwsSecretsManager/actions/workflows/codeql.yml)

A lightweight, typed wrapper around AWS Secrets Manager for .NET Framework 4.8 applications
that want modern Secrets Manager features without migrating off .NET Framework.

## Why

The AWS SDK provides low-level access to Secrets Manager. DGates.AwsSecretsManager adds a higher-level developer experience with:

- **Typed retrieval** — deserialize secrets directly into your own POCOs
- **In-memory caching with configurable TTL** — avoid unnecessary Secrets Manager calls
- **Automatic refresh after cache expiry** — transparently reload expired secrets
- **Retry with backoff** — resilient against transient AWS throttling and network failures
- **Container-agnostic dependency injection** — works with Unity, Autofac, or any DI container
- **Local development support** — develop and test without AWS infrastructure, via LocalStack or local JSON mode

## Features

| Feature | Supported |
|---------|:---------:|
| Typed secret retrieval | ✅ |
| In-memory caching | ✅ |
| Configurable cache TTL | ✅ |
| Automatic cache refresh | ✅ |
| Polly v8 retry policy | ✅ |
| Local JSON development | ✅ |
| LocalStack development | ✅ |
| Optional Microsoft.Extensions.Logging integration | ✅ |
| .NET Framework 4.8 | ✅ |

## Install

```sh
dotnet add package DGates.AwsSecretsManager
```

## Credentials

`SecretsManagerSettings` resolves AWS credentials in this order:

1. Explicit `AccessKey`/`SecretKey` set on the settings object
2. The AWS SDK's default credential chain — environment variables (`AWS_ACCESS_KEY_ID`/`AWS_SECRET_ACCESS_KEY`), `appSettings` keys (`AWSAccessKey`/`AWSSecretKey`) in web.config/app.config, the shared credentials file, or an attached IAM role

If neither resolves, `SecretsManagerService`/`SecretsManagerServiceFactory.Create` throws `InvalidOperationException` immediately, rather than surfacing a generic auth error several calls deep on the first `GetSecretAsync`.

## Quick start

```csharp
public class MyApiKeySecret
{
    public string ApiKey { get; set; }
}

var settings = new SecretsManagerSettings
{
    Region = "us-west-2",
    CacheTtl = TimeSpan.FromMinutes(10)
};

// Throws InvalidOperationException here if no credentials can be resolved — see Credentials above.
var secretsService = new SecretsManagerService(settings);

var apiKey = await secretsService.GetSecretAsync<MyApiKeySecret>("myapp/ApiKey");
```

See the [examples repository](https://github.com/dgates82/DGates.AwsSecretsManager.Examples) for working examples, including:

- Console application
- ASP.NET MVC 5 application
- LocalStack integration
- Local JSON development without AWS

## How it works

A secret request flows through three layers:

1. **Cache check** — if a non-expired entry exists for the secret name, it's returned
   immediately. No network call.
2. **Fetch with retry** — on a miss or expiry, the AWS SDK call is executed inside a
   Polly retry pipeline. Retries apply only to transient failures; the cache is only
   populated after a successful fetch.
3. **Local JSON mode** — if `LocalJsonFallbackPath` is configured, AWS is bypassed entirely
   and secrets are read from the local JSON file instead.

## Local development

There are multiple ways to develop and test locally.

### LocalStack

The repository includes a `docker-compose.yml` that starts LocalStack with AWS Secrets Manager enabled and seeds test secrets.

See [docs/LOCAL_DEV.md](https://github.com/dgates82/DGates.AwsSecretsManager/blob/main/docs/LOCAL_DEV.md) for setup instructions and guidance on when to use Local JSON versus LocalStack.

### Local JSON mode

Set `LocalJsonFallbackPath` to bypass AWS entirely and load secrets from a local JSON file.

This is ideal when:

- Working offline
- Developing without AWS credentials
- Running CI environments without Docker
- Quickly testing configuration changes

## Logging

Logging is completely optional.

Pass an `ILogger` implementation to log:

- Cache hits and misses
- Secret fetches
- Retry attempts
- Failures

If no logger is supplied (or `null` is passed), the library automatically uses `NullLogger`.

```csharp
using Microsoft.Extensions.Logging;

var logger = loggerFactory.CreateLogger<SecretsManagerService>();

var secretsService = new SecretsManagerService(settings, logger);

// or via the factory
var secretsService = SecretsManagerServiceFactory.Create(settings, logger);
```

## Design decisions

### Typed retrieval via Newtonsoft.Json

Secrets in AWS Secrets Manager are typically stored as JSON. Deserializing directly into a POCO removes repetitive parsing code and makes secret structures explicit in your application.

Because this library targets .NET Framework 4.8, Newtonsoft.Json is the appropriate choice over `System.Text.Json`.

### Lightweight caching with ConcurrentDictionary

Each Secrets Manager request incurs network latency and cost.

A lightweight `ConcurrentDictionary` cache provides thread-safe, in-memory caching without introducing the additional dependencies and complexity of `Microsoft.Extensions.Caching.Memory`.

Cache expiry is evaluated when a secret is requested. Expired entries are transparently refreshed.

### Retry only transient failures

Built on Polly v8, retry logic is intentionally limited to transient failures such as:

- `InternalServiceErrorException`
- `LimitExceededException`
- HTTP 429 (Too Many Requests)

Configuration problems such as `ResourceNotFoundException` or `InvalidParameterException` are not retried because they require developer intervention rather than another network attempt.

### Container-agnostic dependency injection

.NET Framework applications commonly use a variety of dependency injection containers including Unity, Autofac, and StructureMap.

Rather than coupling the library to a specific container, `SecretsManagerServiceFactory.Create(...)` returns a plain `ISecretsManagerService` that can be registered with any container.

### Opt-in logging

Logging defaults to `NullLogger`, so existing applications require no changes.

Supplying an `ILogger<SecretsManagerService>` enables structured logging of cache activity, retries, fetches, and failures while keeping logging entirely optional.

### Zero-infrastructure local development

`LocalJsonFallbackPath` (see [Local development](#local-development) above) keeps onboarding
simple and supports environments without container support.

For a deeper discussion of these design decisions and the trade-offs behind them, see the accompanying blog post:

https://dev.to/dgates82/modern-secrets-for-legacy-net-building-a-typed-cached-aws-secrets-manager-library-2mpp

## When not to use this library

- **Apps on modern .NET (Core, 5+)** — consider using the AWS SDK's
  Secrets Manager caching client or the built-in .NET caching ecosystem.
  This library exists specifically to bring similar ergonomics to .NET Framework 4.8.

## Status

Stable release. Semantic versioning is followed for all releases.

See [CHANGELOG.md](https://github.com/dgates82/DGates.AwsSecretsManager/blob/main/CHANGELOG.md) for release history,
or the [v1.0.0 release post](https://dev.to/dgates82/dgatesawssecretsmanager-100-stable-59a7) for
what changed going stable.

## Part of a small ecosystem

| Project | What it is | Reach for it when |
| --- | --- | --- |
| **DGates.AwsSecretsManager** (you are here) | Typed, cached AWS Secrets Manager wrapper for .NET Framework 4.8 | you're on .NET Framework and want typed, cached secrets without hand-rolling it |
| [DGates.AwsSecretsManager.Examples](https://github.com/dgates82/DGates.AwsSecretsManager.Examples) | Console and ASP.NET MVC 5 example apps using this library | you want a working example before wiring it into your own app |

More from dgates82: [DGates.Identity.Jwt2Fa](https://github.com/dgates82/DGates.Identity.Jwt2Fa),
[DGates.Identity.NotificationProviders](https://github.com/dgates82/DGates.Identity.NotificationProviders),
[angular-dotnet-auth-template](https://github.com/dgates82/angular-dotnet-auth-template),
[dotnet-nuget-release-template](https://github.com/dgates82/dotnet-nuget-release-template), and
[dgates-mock-servers](https://github.com/dgates82/dgates-mock-servers) — a separate identity/auth
portfolio, same author. This package isn't scaffolded from `dotnet-nuget-release-template`; it
predates it and targets net48 only.

## License

MIT — see [LICENSE](https://github.com/dgates82/DGates.AwsSecretsManager/blob/main/LICENSE).

## Contributing

Issues and pull requests are welcome.

See [CONTRIBUTING.md](https://github.com/dgates82/DGates.AwsSecretsManager/blob/main/CONTRIBUTING.md).

