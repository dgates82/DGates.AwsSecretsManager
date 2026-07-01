# Contributing

Issues and PRs are welcome. For significant changes — new features, API surface changes,
or anything that touches the retry/caching strategy — please open an issue first to discuss
the approach before investing time in a PR.

## Prerequisites

- [.NET SDK 8.x](https://dotnet.microsoft.com/download) (build tooling)
- [Docker](https://docs.docker.com/get-docker/) (required for integration tests)
- AWS CLI (required to seed LocalStack — `pip install awscli`)

### Linux / macOS only
- [Mono](https://www.mono-project.com/download/stable/) (required to run net48 tests outside Windows)
## Building

```sh
dotnet build DGates.AwsSecretsManager.sln --configuration Release
```

## Running tests

```sh
# Unit tests — no Docker required
dotnet test --filter "Category!=Integration"

# Integration tests — requires LocalStack running and seeded
docker compose up -d
./docker/seed-secrets.sh
dotnet test --filter "Category=Integration"
docker compose down -v
```

See [docs/LOCAL_DEV.md](./docs/LOCAL_DEV.md) for full local dev setup.

## Guidelines

- Target framework is **net48**. Do not use APIs unavailable on .NET Framework 4.8.
- All public members require XML doc comments. Use `<inheritdoc/>` on interface implementations.
- Keep the existing exception handling strategy: only `InternalServiceErrorException`, `LimitExceededException`, and HTTP 429 are treated as transient.
- PRs should include tests covering the changed behaviour.
- Update [CHANGELOG.md](./CHANGELOG.md) if your change affects public API or observable behaviour.
