# DGates.AwsSecretsManager — Changelog
All notable changes to this project will be documented in this file.
The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- CodeQL static analysis (`.github/workflows/codeql.yml`), analyzing the `csharp` language
  via GitHub's Advanced Setup, independent of SonarQube Cloud. Path exclusions cover
  `bin`/`obj`. Runs on push to `main`/`release/**`, on PRs, weekly on a schedule, and via
  `workflow_dispatch`.

### Fixed
- `docs/RELEASING.md` described the old static `NUGET_API_KEY` secret setup; rewritten to
  match `release.yml`'s actual Trusted Publishing (OIDC) flow via `NUGET_USER`.

## [1.1.0] - 2026-09-16
### Added
- `SonarAnalyzer.CSharp` as a build-time Roslyn analyzer (`PrivateAssets=all`, never flows
  to consumers).
- SonarQube Cloud static analysis, wired into CI's `build-and-test` job via
  `dotnet-sonarscanner` and gated on the quality gate result, with coverage
  (`dotnet test --collect:"XPlat Code Coverage"` across both unit and integration test
  runs) fed into the scan via `sonar.cs.cobertura.reportsPaths`. Explicit
  `sonar.branch.name` for non-PR triggers, and `SONAR_PROJECT_KEY`/`SONAR_ORG` repo
  variables instead of hardcoded literals.
### Fixed
- `NuGet/login@v1` pinned to a commit SHA (SonarQube Cloud finding).
- `coverlet.collector` added to the test project - `--collect:"XPlat Code Coverage"` in CI
  was silently a no-op without it, so the first Sonar scan reported 0% coverage.
- `SecretsManagerService` sealed (no code implements or extends it, so the standard
  `IDisposable` dispose pattern is unnecessary ceremony - SonarQube S3881).
- Removed three log-then-rethrow catch blocks in `SecretsManagerService` that added no
  context beyond what the propagated exception already carries (SonarQube S2139 - either log
  and handle, or rethrow, not both).
- Named the magic `429` status-code cast in the transient-error check (SonarQube S109);
  `HttpStatusCode.TooManyRequests` isn't available on `net48`, so this is a local
  `const HttpStatusCode` rather than the newer BCL enum member.
- Added braces and put the `ArgumentNullException` throw on its own line in
  `SecretsManagerServiceFactory.Create`'s guard clause (SonarQube S121/S122).

## [1.0.0] - 2026-07-23
### Added
- Optional structured logging via `Microsoft.Extensions.Logging.Abstractions` —
  pass an `ILogger` to `SecretsManagerService` constructors or
  `SecretsManagerServiceFactory.Create`. Defaults to a no-op `NullLogger` when omitted,
  so existing callers are unaffected.
### Changed
- Fail-fast AWS credential validation — `SecretsManagerService` and
  `SecretsManagerServiceFactory.Create` now validate credential availability during
  construction. Applications receive a clear `InvalidOperationException` immediately
  instead of discovering missing AWS credentials on the first secret request.
### Documentation
- Expanded README with architecture overview, design trade-off rationale, and
  "when not to use this library" guidance.
- Documented credential resolution order and the fail-fast behavior.

Stable release. Supersedes 1.0.0-beta.1 through 1.0.0-beta.4.

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
