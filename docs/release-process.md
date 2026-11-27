# Releases: merge often, release on purpose

The same process as [Keel](https://github.com/SEARATS/RatsSeaPowerKeel/blob/main/docs/community/releases.md) and Flotilla in the SEARATS org. Merging only collects work; a release is a snapshot we choose to make.

## Every merge

- Merge into `main` as often as needed. Nothing is uploaded.
- Each PR adds its bullets under `## Unreleased` in [the change notes](controls-settings/changelog.md).
- No version bump in a PR: the versions below stay at the last release.

| Where the version lives | What |
|---|---|
| `src/Controls/Controls.csproj` + `[ACPlugin]` in `src/Controls/Plugin.cs` | Controls.dll, the Workshop item |
| `src/UI/UI.csproj` | the SP.Controls.UI source package |
| `src/Settings/Settings.csproj` | the SP.Controls.Settings source package |
| `content/workshop/*/_info.ini` | `[Compatibility]`: the game versions the item declares |

## Release day

One release owner runs it on their own PC, start to finish.

| # | Step | What |
|---|---|---|
| 1 | Decide | "Today we release", who owns it, the number (table below) |
| 2 | Snapshot | PR "Release x.y.z": `## Unreleased` becomes `## x.y.z - <date>`, the versions above set, a fresh empty `## Unreleased`; merge it; tag `vx.y.z` on the merge commit |
| 3 | Build from the tag | `git submodule update --init --recursive`, Release build with `-p:DeployToGame=false`, both `dotnet pack` steps (README "Build") |
| 4 | Checks | build against the game's current main branch; every member the DLL uses still exists there; every program file through VirusTotal |
| 5 | Test round | in the test VM, when code changed since the last tested build; findings fixed, back to step 2 with the same number |
| 6 | Ship together | Workshop upload of `artifacts/workshop/ControlsAndSettings` from the tag, and the GitHub release of the same tag (DLL + packages attached, notes from the change notes) |
| 7 | Tell people | the release owner posts; mod authors who depend on it hear about breaking changes first |

## Which number

| Bump | When |
|---|---|
| x.y.**z+1** | only fixes |
| x.**y+1**.0 | new controls, settings or options |
| **x+1**.0.0 | breaks mods that use it (removed or renamed API, settings format) |

## Never

- Bump a version in a normal PR.
- Upload to the Workshop a build that is not from a release tag.
- Ship to the Workshop without the GitHub release, or the other way round.
- Skip the virus scan on release day.
