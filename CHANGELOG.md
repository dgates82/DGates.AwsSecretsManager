# DGates.AwsSecretsManager — Changelog
All notable changes to this project will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.0.0-beta.4] - 2026-07-18
### Added
- Fail-fast credential validation — when no explicit `AccessKey`/`SecretKey` is set, `SecretsManagerService`/`SecretsManagerServiceFactory.Create` now validates the AWS SDK's default credential chain at construction time and throws `InvalidOperationException` if no credential source resolves, instead of failing several calls deep on the first `GetSecretAsync`.
### Documentation
- Expanded README with architecture overview, design trade-off rationale, and "when not to use this library" guidance
- Documented credential resolution order and the new fail-fast behavior

## [1.0.0-beta.3] - 2026-07-16
### Added
- Optional structured logging via `Microsoft.Extensions.Logging.Abstractions` — pass an `ILogger` to `SecretsManagerService`'s constructors or `SecretsManagerServiceFactory.Create`. Defaults to a no-op `NullLogger` when omitted, so existing callers are unaffected.

Supersedes 1.0.0-beta.1 and 1.0.0-beta.2, both of which shipped incomplete or incorrect logging support and were unlisted from NuGet shortly after publishing.

## [0.1.0-beta.1] - 2025-07-05
### Added
- `ISecretsManagerService` — typed secret retrieval (`GetSecretAsync<T>`), raw string retrieval, cache invalidation, and forced refresh.
- In-memory TTL cache via `ConcurrentDictionary` — configurable via `SecretsManagerSettings.CacheTtl`.
- Retry with exponential backoff via Polly 8 `ResiliencePipeline` — retries on `InternalServiceErrorException`, `LimitExceededException`, and HTTP 429.
- `LocalJsonFallbackPath` support — read secrets from a local JSON file without any AWS account.
- `ServiceUrl` support — point the client at LocalStack or any custom endpoint.
- `SecretsManagerServiceFactory` — convenience factory for DI registration.
- LocalStack Docker Compose setup with seed script for local integration testing.
- GitHub Actions CI — build, unit tests, integration tests against LocalStack, and NuGet pack.
