# Controls & Settings

Shared native UI controls and settings pages for Sea Power mods.
The [Workshop item](https://steamcommunity.com/sharedfiles/filedetails/?id=3839698527)
is named Controls & Settings. This repository owns its source and packaging.

| Module | Purpose |
|---|---|
| [SeaPower.SharedUi](src/SeaPower.SharedUi/README.md) | Native controls, layouts, window hosting, and the widget gallery |
| [SeaPower.SharedSettings](src/SeaPower.SharedSettings/README.md) | Typed settings providers, draft editors, and persistence |
| [SeaPower.Controls](src/SeaPower.Controls/README.md) | Anchor Chain plugin that builds both modules into SeaPower.Controls.dll |

## Build

Install the .NET 10 SDK, Sea Power, and Anchor Chain, then run from this checkout.

```powershell
dotnet build SpControls.slnx -c Release -p:DeployToGame=false
dotnet pack src/SeaPower.SharedUi -c Release -p:DeployToGame=false -o artifacts/nuget
dotnet pack src/SeaPower.SharedSettings -c Release -p:DeployToGame=false -o artifacts/nuget
```

The defaults use the Steam installation under Program Files and Anchor Chain's
Workshop item. Override `SeaPowerDir` and `AnchorChainDll` when needed.
The runtime DLL is written to `src/SeaPower.Controls/bin/Release/`.
Its Workshop package is staged under `artifacts/workshop/ControlsAndSettings`.
Builds deploy by default on Windows, so use `-p:DeployToGame=false` for build checks.
Workshop publication requires a separate explicit upload.

The [mod-author guide](src/SeaPower.Controls/README.md) describes the public DLL,
dependency declarations, and typed settings models. The
[examples](examples/ControlsSettings) compile against that DLL.
The two source packages also support embedding the modules in a consumer.

## History

The repository retains the UI and settings history extracted from
`agm-114/crimean-tom`, followed by its pending working-tree changes.
See [extraction details](docs/history.md) and the
[original-to-extracted commit map](docs/commit-map.txt).
Current changes are recorded in the [change notes](docs/controls-settings/changelog.md).
