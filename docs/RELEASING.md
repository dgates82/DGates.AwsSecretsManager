# Releasing

## One-time setup

1. Create a NuGet.org account and generate an API key scoped to the `DGates.AwsSecretsManager` package.
2. Add it as a GitHub Actions secret:
    - Go to Settings → Secrets and variables → Actions → New repository secret
    - Name: `NUGET_API_KEY`
    - Value: your NuGet API key

## Publishing a release

1. Update `CHANGELOG.md` with the release notes.
2. Commit and push.
3. Tag the commit with the version:
```sh
   git tag v0.1.0
   git push origin v0.1.0
```
4. The release workflow runs automatically, builds, tests, and pushes to NuGet.org.