using System;
using System.Collections.Generic;
using BaseLib.Config;
using BaseLib.Config.UI;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Localization;

namespace AdjustUnknownChances.BaseLibCompat;

/// <summary>
/// Adds this mod to BaseLib's home screen "Mod Configuration" menu. Only loaded by
/// <see cref="BaseLibBridge"/> when BaseLib is installed.
/// </summary>
public static class BaseLibIntegration
{
    // BaseLib looks up "<ROOT NAMESPACE>.mod_title" in settings_ui and otherwise shows the raw namespace.
    private const string TitleKey = "ADJUSTUNKNOWNCHANCES.mod_title";
    private const string Title = "Adjust Unknown Chances";

    public static void Register()
    {
        AddTitle();
        // Loc tables are rebuilt on language change, so re-add the title whenever the menu builds its mod list.
        var initializeModList = AccessTools.Method(typeof(NModConfigSubmenu), "InitializeModList");
        if (initializeModList != null)
        {
            new Harmony("com.kyeyongchoo." + ModEntry.ModId + ".baselib")
                .Patch(initializeModList, prefix: new HarmonyMethod(typeof(BaseLibIntegration), nameof(AddTitle)));
        }

        ModConfigRegistry.Register(ModEntry.ModId, new UnknownChancesModConfig());
    }

    private static void AddTitle()
    {
        try
        {
            LocManager.Instance?.GetTable("settings_ui").MergeWith(new Dictionary<string, string> { [TitleKey] = Title });
        }
        catch (Exception ex)
        {
            GD.PrintErr($"{ModEntry.LogPrefix} Could not set Mod Configuration title: {ex.Message}");
        }
    }
}

/// <summary>
/// Settings live in <see cref="OddsSettings"/> (shared with the vanilla mod list panel), so this config has no
/// BaseLib-managed properties; it only supplies the custom UI.
/// </summary>
public sealed class UnknownChancesModConfig : ModConfig
{
    public UnknownChancesModConfig() : base(ModEntry.ModId)
    {
    }

    public override bool VisibleInModList() => true;

    public override void SetupConfigUI(Control optionContainer)
    {
        var panel = new ModMenuPanel(fontSize: 28);
        panel.Root.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        optionContainer.AddChild(panel.Root);
    }
}
