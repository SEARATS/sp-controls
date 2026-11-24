using AnchorChain;
using Noesis;
using SeaPower.Controls;
using SeaPower.SharedSettings;
using static SeaPower.SharedUi.NativeUi;

namespace ControlsSettings.Examples;

[ACPlugin("controls-settings.example.dynamic", "Dynamic settings example", "0.1.0", after: new[] { SeaPower.Controls.Plugin.Id })]
[ACDependency(SeaPower.Controls.Plugin.Id, "0.2.1", null)]
public class DynamicSettings : IAnchorChainMod, IScriptedSettingsProvider<AdvancedSettings>
{
    private enum Operation { BuildPage }
    public string Id => "controls-settings.example.dynamic";
    public string Title => "Dynamic example";
    public AdvancedSettings Defaults => new();
    public AdvancedSettings Current { get; private set; } = new();

    public void TriggerEntryPoint() { }
    public void Apply(AdvancedSettings values) => Current = values;

    public void Build(StackPanel panel, SettingsDraft<AdvancedSettings> draft)
    {
        using var activity = this.BeginActivity(Operation.BuildPage, nameof(AdvancedSettings));
        panel.Children.Add(draft.CheckEditor(values => values.ShowAdvanced, draft.Refresh));
        if (!draft.Values.ShowAdvanced) return;
        panel.Children.Add(Section("Advanced", Stack(
            FormRow("Refresh interval (s)", draft.NumberEditor(values => values.RefreshSeconds)),
            Text("This section is rebuilt from the current draft when its checkbox changes."))));
    }
}

public class AdvancedSettings
{
    [Setting("Show advanced controls")]
    public bool ShowAdvanced { get; set; }
    [Setting("Refresh interval (s)", Minimum = 1, Maximum = 30)]
    public double RefreshSeconds { get; set; } = 5;
}
