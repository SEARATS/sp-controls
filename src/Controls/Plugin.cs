using System.Reflection;
using AnchorChain;
using HarmonyLib;
using Settings;
using UnityEngine;

namespace Controls;

[ACPlugin(Id, "Controls & Settings", "0.3.0")]
public class Plugin : IAnchorChainMod
{
    public const string Id = "crimean-tom.controls";

    public void TriggerEntryPoint()
    {
        var harmony = new Harmony(Id);
        var hooked = new HashSet<MethodInfo>();
        foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (var type in Types(assembly).Where(type => !type.IsAbstract
                && typeof(IAnchorChainMod).IsAssignableFrom(type) && typeof(ISettingsProvider).IsAssignableFrom(type)))
            {
                try
                {
                    var map = type.GetInterfaceMap(typeof(IAnchorChainMod));
                    var entry = map.TargetMethods.Single();
                    if (hooked.Add(entry)) harmony.Patch(entry, postfix: new HarmonyMethod(typeof(Plugin), nameof(RegisterSettings)));
                }
                catch (Exception error) { Debug.LogError($"[Controls & Settings] Could not hook {type.FullName}: {error}"); }
            }
        }
        Debug.Log("[Controls & Settings] Loaded native UI and settings library.");
    }

    private static IEnumerable<Type> Types(Assembly assembly)
    {
        try { return assembly.GetTypes(); }
        catch (ReflectionTypeLoadException error) { return error.Types.OfType<Type>(); }
        catch (NotSupportedException) { return []; }
    }

    private static void RegisterSettings(object __instance)
    {
        if (__instance is not ISettingsProvider provider) return;
        try { GameplaySettings.Register(provider); }
        catch (Exception error) { Debug.LogError($"[Controls & Settings] Could not register {provider.GetType().FullName}: {error}"); }
    }
}
