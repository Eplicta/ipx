# Releasing

A new version of the package is published to nuget.org each time a change to the code or the schema is merged to `master`. There is no step in between: merging is the release.

## What the workflows do

| Workflow | When | What |
|---|---|---|
| `Build` | Every push to `master` and every pull request | Restores, builds, runs the tests on .NET 8, 9, 10 and 11 on Linux and on Windows, and packs the package as a build artifact. Nothing is published. |
| `Publish` | A push to `master` that changes `src/`, `schema/`, `version.txt` or `Directory.Build.props`, or a manual run | Works out the version, builds, tests, packs both packages (the library and the `ipx` tool), publishes them to nuget.org and creates a GitHub release with the packages, the specification and the schema attached. |

A change that only touches the documentation, the specification text, the examples or the tests does not publish. Because pull requests run `Build` and not `Publish`, a pull request is where the tests have to pass before the merge; protect `master` and require the `Build` checks to make that a rule.

## Versions

The version is worked out from `version.txt` and the number of the `Publish` run, so every publish gets a new version and nobody edits a version for an ordinary change.

| `version.txt` holds | Published as | On nuget.org |
|---|---|---|
| `1.0-preview` | `1.0.0-preview.<run>` | A prerelease, for work towards the next version. |
| `1.0` | `1.0.<run>` | A stable version. |

To release a new minor or major version, change `version.txt` in the pull request, for example to `1.1` or `2.0`. The run number keeps counting, so after a change from `1.0` to `1.1` the next version is `1.1.<run>` and still higher than every earlier one.

The package follows semantic versioning, and its major version follows the major version of the format:

| Change | Package version |
|---|---|
| A fix that changes no behaviour a user could rely on | the patch number, which changes by itself |
| New typed properties or classes, or support for a new minor version of the format | raise the minor in `version.txt` |
| Support for a new major version of the format, or a change that breaks existing code | raise the major in `version.txt` |

Version 1.x of the package reads and writes IPX 1.x. A new major version of the format is a new major version of the package.

Moving from `1.0-preview` to `1.0` is how the first stable version is published. Do it when the specification is no longer a draft; a stable version cannot be taken back, only unlisted.

## Publishing an exact version

The version worked out from `version.txt` always ends in the number of the run, so `version.txt` set to `1.0` publishes `1.0.7`, not `1.0.0`. To publish an exact version, such as the first stable `1.0.0`, run the `Publish` workflow by hand and give the version:

```
gh workflow run publish.yml --repo Eplicta/ipx --ref master -f version=1.0.0
```

The version must be `<major>.<minor>.<patch>` with an optional `-<prerelease>`, and the run must be on `master`. The workflow builds, tests, publishes and creates the GitHub release exactly as for an automatic version. A version with a hyphen is a prerelease on nuget.org and on GitHub.

After a stable release, set `version.txt` to the next preview, such as `1.1-preview`, so that the automatic versions that follow are `1.1.0-preview.N` and sort above `1.0.0`.

## One-time setup

The `Publish` workflow publishes with nuget.org Trusted Publishing, so there is no API key to store and nothing that expires.

1. On nuget.org, under *Trusted Publishing*, create a policy with the package owner that owns the packages, the repository owner `Eplicta`, the repository `ipx`, the workflow file `publish.yml`, the scope *Push new packages and package versions*, and the glob pattern `Eplicta.Ipx*`, which covers both `Eplicta.Ipx` and `Eplicta.Ipx.Tool`. Leave the environment empty.
2. In the repository, under *Settings, Secrets and variables, Actions, Variables*, add a variable named `NUGET_USER` that holds the nuget.org profile name of the account that created the policy. It is the profile name, not the e-mail address.

While the repository is private, a new policy is active for seven days and becomes permanent after its first successful publish. If seven days pass without one, activate the policy again on nuget.org.

The workflow asks GitHub for a short-lived token for the run (`id-token: write`), exchanges it for a nuget.org key that is valid for one hour and only for the packages the policy covers, and pushes with it.

## When a publish fails

* **A test fails:** nothing is published. Fix it and merge again.
* **The push to nuget.org fails:** the run stops before the GitHub release. Re-run the failed job from the Actions page. The run number is the same, so the version is the same, and `--skip-duplicate` makes a second attempt harmless if the package did get through.
* **A published version is wrong:** nuget.org cannot delete a version. Unlist it on nuget.org and merge a fix, which publishes a higher version.
