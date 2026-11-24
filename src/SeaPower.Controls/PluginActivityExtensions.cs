using System.Collections.Concurrent;
using System.Reflection;
using AnchorChain;
using SeaPower.SharedUi;

namespace SeaPower.Controls;

public static class PluginActivityExtensions
{
    private static readonly ConcurrentDictionary<Type, ModIdentity> Owners = new();

    public static IDisposable BeginActivity(this IAnchorChainMod plugin, ModActivityKind operation, params string[] details) =>
        ModActivity.Begin(Owners.GetOrAdd(plugin.GetType(), type => new(type.GetCustomAttribute<ACPlugin>()?.GUID
            ?? throw new ArgumentException("The plugin must declare [ACPlugin].", nameof(plugin)))), operation, details);

    public static IDisposable BeginActivity<TOperation>(this IAnchorChainMod plugin, TOperation operation, params string[] details)
        where TOperation : struct, Enum => plugin.BeginActivity(ModActivityKind.GameAction,
            $"{typeof(TOperation).Name}.{operation}", string.Join(" ", details));
}
