# Releasing

## One-time setup

1. On [NuGet.org](https://www.nuget.org), go to your account's **Trusted Publishing**
   settings and add a new trusted publisher, linking it to this GitHub repo and the
   `release.yml` workflow file.
2. In this repo, add a repository secret named `NUGET_USER` containing your NuGet.org
   profile name — your username, visible in your NuGet.org account settings, not an API
   key or email address.
3. No other secrets are needed. `release.yml`'s `publish` job requests `id-token: write`
   permission and exchanges a short-lived OIDC token for a NuGet API key at publish time
   via `NuGet/login`, pinned to a commit SHA — nothing long-lived is stored in the repo.

## Publishing a release

1. Update `CHANGELOG.md` with the release notes.
2. Commit and push.
3. Tag the commit with the version:
```sh
   git tag v0.1.0
   git push origin v0.1.0
```
4. The release workflow runs automatically, builds, tests, and pushes to NuGet.org.
