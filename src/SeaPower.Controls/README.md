# Controls & Settings

Controls is a library for Sea Power mod authors. Build in-game windows, tabs,
tables, search filters, sliders, and progress bars using Sea Power’s native UI
more easily.

Settings allows you to easily add settings pages to your mods.

Version 0.2.2 (unreleased). See [change notes](../../docs/controls-settings/changelog.md).

Subscribe to [Controls & Settings](https://steamcommunity.com/sharedfiles/filedetails/?id=3839698527)
on the Steam Workshop and enable it in Sea Power's Mod Manager. This is an
Anchor Chain mod, also supported by Keel. Reference
`SeaPower.Controls.dll` from the installed item in your mod
project. Set `Private` to `false` and list Controls & Settings as a Workshop dependency
so players install the shared copy. Your project still needs the game's Noesis
and Seapower-Scripts references.

```xml
<Reference Include="SeaPower.Controls" HintPath="$(ControlsDll)" Private="false" />
```

Use the shared DLL instead of importing SharedUi or SharedSettings source into
the same mod. The assembly and namespace names remain stable after the rename.
Declare the dependency and load order so the library starts first under both
Anchor Chain and Keel:

```csharp
[ACPlugin("example.my-mod", "My Mod", "1.0.0", after: new[] { SeaPower.Controls.Plugin.Id })]
[ACDependency(SeaPower.Controls.Plugin.Id, "0.2.2", null)]
```

## Settings with minimal code

Implement `ISettingsProvider<YourSettings>` on your existing `IAnchorChainMod` class.
Your settings are a concrete model with bool, int, float, double, and int-backed
enum properties. Annotate the properties with `[Setting]`; the library generates
the editors. Gameplay code reads the same concrete model it receives in `Apply`.
Controls & Settings discovers the interface and registers your page after your
entry point completes. It uses the loader's existing instance; it does not create
another instance of your plugin. No Harmony patches, registration call, Noesis
layout, or persistence code are needed in your mod.

```csharp
using AnchorChain;
using SeaPower.SharedSettings;

[ACPlugin("example.my-mod", "My Mod", "1.0.0", after: new[] { SeaPower.Controls.Plugin.Id })]
[ACDependency(SeaPower.Controls.Plugin.Id, "0.2.2", null)]
public class Plugin : IAnchorChainMod, ISettingsProvider<DisplaySettings>
{
    public string Id => "example.my-mod";
    public string Title => "My Mod";
    public DisplaySettings Defaults => new();
    public DisplaySettings Current { get; private set; } = new();
    public void TriggerEntryPoint() { /* Start your mod here. */ }
    public void Apply(DisplaySettings values) => Current = values;
}

public enum DetailLevel { Low, Normal, High }

public class DisplaySettings
{
    [Setting("Show labels")]
    public bool ShowLabels { get; set; } = true;
    [Setting("Refresh interval (s)", Minimum = 1, Maximum = 30)]
    public int RefreshSeconds { get; set; } = 5;
    [Setting("Detail level")]
    public DetailLevel Detail { get; set; } = DetailLevel.Normal;
}
```

Use a unique ID, normally your plugin GUID; it is also your settings filename.
`Apply` receives active values after startup and after the player accepts changes.
It should update your runtime state without rebuilding or restarting your plugin.
`[Setting]` supports a `Help` tooltip. Persistence maps properties to INI values
internally; the provider and gameplay code use typed properties. This version
does not provide string or key-binding settings.

## Scripted settings

Implement `IScriptedSettingsProvider<YourSettings>` instead to supply
`Build(StackPanel panel, SettingsDraft<YourSettings> draft)`, along with the same
typed `Defaults`, `Apply`, `Id` and `Title` members.
Build any native Noesis controls and dynamic sections you need.
`draft.CheckEditor`, `NumberEditor`, and `ChoiceEditor` wire up values and default-mode
editability using typed selectors, for example `draft.NumberEditor(values => values.RefreshSeconds)`.
`draft.Values` returns a typed snapshot. Wrap custom controls in `draft.Editor`,
change values with `draft.Set(values => values.ShowLabels, enabled)`, and call
`draft.Refresh()` to queue a rebuild after the current native input event. Put custom
validation messages in `draft.Errors`; any error blocks Accept before pages save.

The [minimal](../../examples/ControlsSettings/MinimalSettings.cs) and
[dynamic](../../examples/ControlsSettings/DynamicSettings.cs) examples compile against
the public DLL. The example project is a development fixture, not part of the
Workshop payload. For providers outside Anchor Chain plugins, or registrations
created later, call `GameplaySettings.Register(provider)` on the game's UI thread
before opening Options. Reflection is confined to provider discovery and the
model/persistence boundary. For existing scripted integrations, the overload
`GameplaySettings.Register(title, settings, build)` accepts a `ModSettings` and
builder directly, without requiring your own Harmony instance; it retains the
older low-level value store for the existing mods' dynamic catalogs.

Pages appear under **Options → Gameplay**, beside the game's General page.
Players can keep shipped defaults, edit custom values, or reset their draft.
Accept validates and saves; Back and Revert discard changes. Files remain under
`CrimeanTom/Settings/<id>.ini` in Unity's persistent data directory for compatibility
with existing mods. Framework updates preserve those files.

## Native UI controls

For optional Keel diagnostic scopes, import `SeaPower.Controls` and call
`this.BeginActivity(operation, detail1, detail2)` from your Anchor Chain plugin.
The extension infers ownership from `[ACPlugin]`, accepts `ModActivityKind` or
your own operation enum, and takes `params string[]` details. It is a no-op when
the installed loader has no activity API. Control characters are recorded as
visible escapes; avoid putting private player-entered values in diagnostic text.

```csharp
using static SeaPower.SharedUi.NativeUi;

var opacity = Range(70, 0, 100, 5, value => UpdateOpacity(value));
var display = Stack(Text("Display options"), SliderField("Opacity", opacity, value => $"{value}%"));
var window = Window("MyModDisplay", "Display", Tabs(("General", display)));
```

Call `SeaPower.SharedUi.WidgetGallery.Show(host)` from a button or command to
open an interactive widget gallery in a mission or the Encyclopedia.
`WidgetGallery.CreatePage()` returns the same tabs for embedding in your own
window.

Use `VerticalRange` for the game's speed or depth slider appearance, including
tick marks and an optional reversed direction. `SliderEntry` pairs the slider
with a number field and units. Pass value and label pairs to mark particular
notches on a horizontal or vertical slider.

```csharp
var speed = VerticalRange(18, 0, 36, 6);
var entry = SliderEntry(speed, "kts", (0, "Stop"), (12, "Cruise"), (36, "Flank"));
```

Use `RangeValue(bar, value => $"{value:0} t")` for a live progress readout
with custom units or formatting. `SegmentedProgress.Refresh` accepts an
optional label for each segment in addition to its tooltip.

The Native controls tab also shows a toolbar,
menu, popup, rich text, and an RGB picker made with native sliders. `ShowFilePicker` opens the
game's file picker and gives your mod the selected path.

Build and open the window on the game's UI thread through the native window
host. The DLL exposes the existing `SeaPower.SharedUi` types directly.
Helpers return native Noesis controls, so callers can use their properties,
styles, and events. For widgets and templates without a dedicated helper, use
the native Noesis type directly or `NativeUi.FromXaml<T>()` with your mod's XAML.
Game-specific screens still require their own view models and data.
For an option that compiles the controls into each mod, use the
[SeaPower.SharedUi source package](../SeaPower.SharedUi/README.md).

## Build and package

```powershell
dotnet build src/SeaPower.Controls/SeaPower.Controls.csproj -c Release -p:DeployToGame=false
```

The build stages `SeaPower.Controls.dll`, this README, Workshop metadata and the
thumbnail in `artifacts/workshop/ControlsAndSettings`. Gallery images and captured
settings pages from `content/workshop/controls-previews/` are staged separately in
`artifacts/workshop/ControlsAndSettings-previews`. Capture real installed settings
pages using the [crimean-tom capture tools](https://github.com/agm-114/crimean-tom/blob/main/scripts/mod-icons.md).
Store the thumbnail in `content/workshop/controls/thumbnail.jpg` and gallery
images in `content/workshop/controls-previews/`, then build again to stage them.
Publishing is a separate explicit Workshop operation.
