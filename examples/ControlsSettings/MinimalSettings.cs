using AnchorChain;
using Settings;

namespace ControlsSettings.Examples;

[ACPlugin("controls-settings.example.simple", "Simple settings example", "0.1.2", after: new[] { Controls.Plugin.Id })]
[ACDependency(Controls.Plugin.Id, "0.3.0", null)]
public class MinimalSettings : IAnchorChainMod, ISettingsProvider<DisplaySettings>
{
    public string Id => "controls-settings.example.simple";
    public string Title => "Simple example";
    public DisplaySettings Defaults => new();
    public DisplaySettings Current { get; private set; } = new();

    public void TriggerEntryPoint() { }

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
