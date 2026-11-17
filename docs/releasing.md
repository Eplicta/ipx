# Releasing

A new version of the package is published when a version tag is pushed. Nothing is published by an ordinary push, so a release is always a decision.

## What the workflows do

| Workflow | When | What |
|---|---|---|
| `Build` | Every push to `main` and every pull request | Restores, builds, runs the tests on .NET 8, 9 and 10 on Linux and on Windows, and packs the package as a build artifact. |
| `Release` | A tag that starts with `v` | Reads the version from the tag, builds, tests, packs, publishes the package to nuget.org and creates a GitHub release with the packages, the specification and the schema attached. |

## Publishing a version

1. Update `CHANGELOG.md` and, when the format changed, the specification and the schema.
2. Merge to `main` and wait for `Build` to pass.
3. Tag the commit and push the tag. The tag is `v` followed by the version:

   ```
   git tag v1.0.0
   git push origin v1.0.0
   ```

   A version with a hyphen, such as `v1.0.0-preview.1`, is published as a prerelease on nuget.org and on GitHub.

The release fails without publishing anything when the tag is not of the form `v<major>.<minor>.<patch>` with an optional `-<prerelease>`, or when a test fails.

## One-time setup

The `Release` workflow needs a repository secret named `NUGET_API_KEY`: a key from nuget.org that is allowed to push the package `Eplicta.Ipx`. Create the secret under *Settings, Secrets and variables, Actions*. A package id can only be pushed by a key whose glob pattern covers it, and nuget.org keys expire, so set a reminder for the expiry date.

## Versions

The package follows semantic versioning, and its major version follows the major version of the format:

| Change | Package version |
|---|---|
| A fix that changes no behaviour a user could rely on | patch |
| New typed properties or classes, or support for a new minor version of the format | minor |
| Support for a new major version of the format, or a change that breaks existing code | major |

Version 1.x of the package reads and writes IPX 1.x. A new major version of the format is a new major version of the package.
