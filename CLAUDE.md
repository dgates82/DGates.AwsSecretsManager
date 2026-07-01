# CLAUDE.md - Guardrails for DGates.AwsSecretsManager

## Commands
* Build: `dotnet build DGates.AwsSecretsManager.sln --configuration Release`
* Unit Tests: `dotnet test --filter "Category!=Integration"`
* Integration Tests: `dotnet test --filter "Category=Integration"`

## Active Regressions to Avoid (.NET 4.8 / AWSSDK 3.7.x)
* **Strict net48**: Do not use modern .NET features. No `System.Text.Json`, no `IHttpClientFactory`, no `HttpStatusCode.TooManyRequests` (use `(HttpStatusCode)429`).
* **AWSSDK Exceptions**: Only use `InternalServiceErrorException`, `LimitExceededException`, `ResourceNotFoundException`, `InvalidParameterException`, `InvalidRequestException`. (`ServiceUnavailableException` does not exist).
* **XML Docs**: Public members require XML comments. Use `<inheritdoc/>` on implementations.

## Response Rules (Token Savings)
1. Return code changes **ONLY** as git-style diffs or minimal block replacements. Never reprint unedited sections.
2. Omit conversational filler, change summaries, greetings, and architectural commentary.
3. For build/test errors, isolate only filename, line number, and error string. Discard all structural logs.
