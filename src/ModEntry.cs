using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;

namespace AdjustUnknownChances;

[ModInitializer("Initialize")]
public static class ModEntry
{
    public const string ModId = "AdjustUnknownChances";
    public const string LogPrefix = "[AdjustUnknownChances]";

    public static void Initialize()
    {
        try
        {
            OddsSettings.Load();
            new Harmony("com.kyeyongchoo." + ModId).PatchAll();
            BaseLibBridge.RegisterWhenModsLoaded();
            GD.Print($"{LogPrefix} Loaded");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"{LogPrefix} Failed to initialize: {ex}");
        }
    }
}
