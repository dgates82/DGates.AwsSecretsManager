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
