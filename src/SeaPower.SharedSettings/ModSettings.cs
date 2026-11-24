using UnityEngine;

namespace SeaPower.SharedSettings;

public class ModSettings
{
    private const string ModeSection = "Settings";
    private const string DefaultKey = "UseModDefaults";
    private readonly string path;
    private SettingsValues custom;
    public string Id { get; }
    public SettingsValues Defaults { get; }
    public bool UseDefaults { get; private set; }
    public event Action? Changed;
    public SettingsValues Current => UseDefaults ? Defaults : custom;

    public ModSettings(string fileName, SettingsValues defaults, Func<SettingsValues>? legacy = null)
    {
        if (string.IsNullOrWhiteSpace(fileName) || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
            || fileName.Contains('/') || fileName.Contains('\\') || fileName is "." or "..")
            throw new ArgumentException("Use a unique mod ID without directory separators.", nameof(fileName));
        Id = fileName;
        path = Path.Combine(Application.persistentDataPath, "CrimeanTom", "Settings", fileName + ".ini");
        Defaults = defaults.Copy();
        custom = defaults.Copy();
        UseDefaults = true;
        try
        {
            if (File.Exists(path))
            {
                using var reader = File.OpenText(path);
                var saved = SettingsValues.Read(reader);
                custom.Overlay(saved, replaceSections: true);
                UseDefaults = saved.Get(ModeSection, DefaultKey, 1) != 0;
            }
            else if (legacy != null) custom.Overlay(legacy());
        }
        catch (IOException error) { Debug.LogWarning($"[{fileName}] Could not read settings: {error.Message}"); }
        catch (UnauthorizedAccessException error) { Debug.LogWarning($"[{fileName}] Could not read settings: {error.Message}"); }
    }

    public SettingsValues Edit() => custom.Copy();

    public void Apply(bool useDefaults, SettingsValues values)
    {
        var saved = values.Copy();
        saved.Set(ModeSection, DefaultKey, useDefaults ? 1 : 0);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + ".tmp";
        File.WriteAllLines(temporary, saved.Lines());
        if (File.Exists(path)) File.Replace(temporary, path, null);
        else File.Move(temporary, path);
        custom = saved;
        UseDefaults = useDefaults;
        Changed?.Invoke();
    }
}
