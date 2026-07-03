# DGates.AwsSecretsManager — Local Development

This repo's tests and example app can run entirely without a real AWS account using
[LocalStack](https://www.localstack.cloud/).

## Setup

1. Start LocalStack:
   ```sh
   docker compose up -d
   ```
2. Wait for it to report healthy, then seed test secrets:
   ```sh
   ./docker/seed-secrets.sh
   ```
   The seed script populates placeholder values for all secrets. To inject real API keys
   (e.g. a real OpenWeatherMap key for the examples app), create a local override file
   that is gitignored:
   ```sh
   # docker/seed-secrets.local.sh — create this file locally, never commit it
   #!/usr/bin/env bash
   source "$(dirname "$0")/seed-secrets.sh"

   create_or_update_secret "dev/DGates.AwsSecretsManager.Examples/OpenWeatherMap" \
     '{"Url":"https://api.openweathermap.org/data/2.5/weather","ApiKey":"YOUR_REAL_KEY_HERE"}'
   ```
   Then run it instead of (or after) the main seed script:
   ```sh
   chmod +x ./docker/seed-secrets.local.sh
   ./docker/seed-secrets.local.sh
   ```
3. Point your `SecretsManagerSettings` at LocalStack:
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
