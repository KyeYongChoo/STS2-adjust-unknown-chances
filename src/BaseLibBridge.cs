using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Godot;
using MegaCrit.Sts2.Core.Modding;

namespace AdjustUnknownChances;

/// <summary>
/// Optional integration with BaseLib's "Mod Configuration" menu.
/// The BaseLib-specific code lives in a separate assembly, because the game calls GetTypes() on this one and
/// would fail to load the whole mod if any type here referenced BaseLib while BaseLib isn't installed.
/// </summary>
public static class BaseLibBridge
{
    private const string BaseLibModId = "BaseLib";
    private const string CompatAssemblyName = "AdjustUnknownChances.BaseLib";
    private const string IntegrationType = "AdjustUnknownChances.BaseLibCompat.BaseLibIntegration";

    /// <summary>
    /// Mod load order only follows declared dependencies (and the player's manual ordering), so BaseLib may load
    /// after this mod. Check once every mod has finished loading; BaseLib only reads its registry when its menu opens.
    /// </summary>
    public static void RegisterWhenModsLoaded() => Callable.From(TryRegister).CallDeferred();

    private static void TryRegister()
    {
        try
        {
            if (!ModManager.GetLoadedMods().Any(m => m.manifest?.id == BaseLibModId)) return;

            Assembly self = typeof(BaseLibBridge).Assembly;
            string path = Path.Combine(Path.GetDirectoryName(self.Location)!, CompatAssemblyName + ".dll");
            if (!File.Exists(path))
            {
                GD.PrintErr($"{ModEntry.LogPrefix} BaseLib is loaded but {CompatAssemblyName}.dll is missing; skipping Mod Configuration entry");
                return;
            }

            AssemblyLoadContext context = AssemblyLoadContext.GetLoadContext(self) ?? AssemblyLoadContext.Default;
            Assembly compat = context.LoadFromAssemblyPath(path);
            compat.GetType(IntegrationType, throwOnError: true)!
                .GetMethod("Register", BindingFlags.Public | BindingFlags.Static)!
                .Invoke(null, null);
            GD.Print($"{ModEntry.LogPrefix} Registered with BaseLib Mod Configuration");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"{ModEntry.LogPrefix} Failed to register with BaseLib: {ex}");
        }
    }
}
