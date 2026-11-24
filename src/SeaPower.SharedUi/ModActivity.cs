using System.Reflection;

namespace SeaPower.SharedUi;

public readonly struct ModIdentity
{
    public string Value { get; }
    public ModIdentity(string value) => Value = !string.IsNullOrWhiteSpace(value)
        ? value : throw new ArgumentException("Supply the owning mod's ID.", nameof(value));
}

public enum ModActivityKind { Other, UiInput, SettingsEdit, SettingsRebuild, GameAction }

/// <summary>Optional Keel breadcrumbs for callbacks that enter native UI or gameplay code.</summary>
public static class ModActivity
{
    private static MethodInfo? track, legacyTrack;
    private static Type? ownerType, kindType, contextType;
    private static bool inspected;

    public static IDisposable Begin(ModIdentity owner, ModActivityKind kind, params string[] details)
    {
        if (string.IsNullOrWhiteSpace(owner.Value)) return Empty.Instance;
        try
        {
            if (!inspected)
            {
                var assembly = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(item => item.GetName().Name == "SeaPowerKeel");
                var api = assembly?.GetType("SeaPowerKeel.Keel");
                ownerType = assembly?.GetType("SeaPowerKeel.PluginId");
                kindType = assembly?.GetType("SeaPowerKeel.ActivityKind");
                contextType = assembly?.GetType("SeaPowerKeel.ActivityContext");
                track = contextType == null ? null : api?.GetMethod("TrackActivity", [contextType]);
                legacyTrack = api?.GetMethod("TrackActivity", [typeof(string), typeof(string), typeof(string)]);
                inspected = assembly != null;
            }
            if (track != null && ownerType != null && kindType != null && contextType != null)
            {
                // Convert optional foreign types only at the reflection boundary.
                var context = Activator.CreateInstance(contextType, Activator.CreateInstance(ownerType, owner.Value), Enum.Parse(kindType, kind.ToString()), details);
                return track.Invoke(null, [context]) as IDisposable ?? Empty.Instance;
            }
            return legacyTrack?.Invoke(null, [owner.Value, kind.ToString(), string.Join(" ", details)]) as IDisposable ?? Empty.Instance;
        }
        catch (Exception) { return Empty.Instance; }
    }

    private class Empty : IDisposable
    {
        internal static readonly Empty Instance = new();
        public void Dispose() { }
    }
}
