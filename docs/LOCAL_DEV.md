# DGates.AwsSecretsManager — Local Development

This repo's tests and example app can run entirely without a real AWS account using
[LocalStack](https://www.localstack.cloud/).

## Setup

1. Start LocalStack — secrets are seeded automatically once the container is healthy:
   ```sh
   docker compose up -d
   ```
2. Point your `SecretsManagerSettings` at LocalStack:
   ```csharp
   var settings = new SecretsManagerSettings
   {
       Region = "us-west-2",
       ServiceUrl = "http://localhost:4566",
       AccessKey = "test",
       SecretKey = "test"
   };
   ```

## Running tests

```sh
# Unit tests only (no Docker required)
dotnet test --filter "Category!=Integration"

# Integration tests (requires LocalStack running + seeded, see above)
dotnet test --filter "Category=Integration"

# Everything
dotnet test
```

## Tearing down

```sh
docker compose down -v
```

## Building a local NuGet package

Use this to test the package locally before publishing, or to reference it from the examples repo.

```sh
dotnet pack DGates.AwsSecretsManager.sln --configuration Release /p:Version=0.1.0 --output ./nupkg
```

To reference the local package from another project, add a local NuGet source:

```sh
dotnet nuget add source /path/to/DGates.AwsSecretsManager/nupkg --name DGatesLocal
```

Then reference it normally in the consuming project's `.csproj`:

```xml
<PackageReference Include="DGates.AwsSecretsManager" Version="0.1.0" />
```

## Developing without Docker

Docker Desktop's Linux-container support (needed for LocalStack) generally works fine on native
Windows via WSL2, but isn't available in some environments — most notably a Windows VM nested
inside another hypervisor without virtualization passthrough.

In that case:
- **Unit tests still run** — they don't depend on Docker or LocalStack at all.
- **Integration tests (`Category=Integration`) require LocalStack and can't be skipped or faked.**
  They exist specifically to verify the real `ServiceUrl`/AWS SDK code path, so pointing at
  `LocalJsonFallbackPath` instead wouldn't actually test that path — it would just confirm the
  fallback branch works, which is a different (and already separately tested) code path.
- If you need to verify integration behavior without Docker, the more faithful option is pointing
  `SecretsManagerSettings` at a real, disposable AWS Secrets Manager secret with a narrow IAM
  policy, rather than substituting the JSON fallback.

`LocalJsonFallbackPath` is meant for *consumers* of this library who want to develop their own
app without any AWS/LocalStack dependency at all (see the examples repo's MvcExample for a
working example) — not as a stand-in for this repo's own integration test suite.
