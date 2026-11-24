using Noesis;

namespace SeaPower.SharedSettings;

public interface ISettingsProvider
{
    string Id { get; }
    string Title { get; }
}

/// <summary>Declare a concrete model; annotated properties become editors.</summary>
public interface ISettingsProvider<T> : ISettingsProvider where T : class, new()
{
    T Defaults { get; }
    void Apply(T values);
}

public interface IScriptedSettingsProvider<T> : ISettingsProvider<T> where T : class, new()
{
    void Build(StackPanel panel, SettingsDraft<T> draft);
}

[AttributeUsage(AttributeTargets.Property)]
public class SettingAttribute(string label) : Attribute
{
    public string Label { get; } = label;
    public string? Help { get; set; }
    public double Minimum { get; set; } = 0;
    public double Maximum { get; set; } = 100;
}

// The non-generic boundary is internal; public providers keep their model type.
internal interface IScriptedSettingsProvider : ISettingsProvider
{
    SettingsValues Defaults { get; }
    void Build(StackPanel panel, SettingsDraft draft);
    void Apply(SettingsValues values);
}
