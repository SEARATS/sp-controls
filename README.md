# Controls & Settings

Shared native UI controls and settings pages for Sea Power mods.
The [Workshop item](https://steamcommunity.com/sharedfiles/filedetails/?id=3839698527)
is named Controls & Settings. This repository owns its source and packaging.

| Module | Purpose |
|---|---|
| [UI](src/UI/README.md) | Native controls, layouts, window hosting, and the widget gallery |
| [Settings](src/Settings/README.md) | Typed settings providers, draft editors, and persistence |
| [Controls](src/Controls/README.md) | Anchor Chain plugin that builds both modules into Controls.dll |

## Build

Install the .NET 10 SDK, Sea Power, and Anchor Chain, then run from this checkout.

```powershell
git submodule update --init --recursive
dotnet build SpControls.slnx -c Release -p:DeployToGame=false
dotnet pack src/UI -c Release -p:DeployToGame=false -o artifacts/nuget
dotnet pack src/Settings -c Release -p:DeployToGame=false -o artifacts/nuget
```

The defaults use the Steam installation under Program Files and Anchor Chain's
Workshop item. Override `SeaPowerDir` and `AnchorChainDll` when needed.
The runtime DLL is written to `src/Controls/bin/Release/`.
It embeds bindings from the pinned [sp-internals submodule](external/sp-internals)
for the native Options menu hooks.
Its Workshop package is staged under `artifacts/workshop/ControlsAndSettings`.
Builds deploy by default on Windows, so use `-p:DeployToGame=false` for build checks.
Workshop publication requires a separate explicit upload.

## Releases

Merges collect under `## Unreleased` in the [change notes](docs/controls-settings/changelog.md) and never bump a version.
A release is a deliberate snapshot: version, tag, build from the tag, Workshop and GitHub together.
Steps: [docs/release-process.md](docs/release-process.md).

## Mod authors

The [mod-author guide](src/Controls/README.md) describes the public DLL,
dependency declarations, and typed settings models. The
[examples](examples/ControlsSettings) compile against that DLL.
The two source packages also support embedding the modules in a consumer.
