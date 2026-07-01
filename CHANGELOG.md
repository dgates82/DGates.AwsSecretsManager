# DGates.AwsSecretsManager — Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0-beta.1] - TBD

### Added
- `ISecretsManagerService` — typed secret retrieval (`GetSecretAsync<T>`), raw string retrieval, cache invalidation, and forced refresh.
- In-memory TTL cache via `ConcurrentDictionary` — configurable via `SecretsManagerSettings.CacheTtl`.
- Retry with exponential backoff via Polly 8 `ResiliencePipeline` — retries on `InternalServiceErrorException`, `LimitExceededException`, and HTTP 429.
- `LocalJsonFallbackPath` support — read secrets from a local JSON file without any AWS account.
- `ServiceUrl` support — point the client at LocalStack or any custom endpoint.
- `SecretsManagerServiceFactory` — convenience factory for DI registration.
- LocalStack Docker Compose setup with seed script for local integration testing.
- GitHub Actions CI — build, unit tests, integration tests against LocalStack, and NuGet pack.
