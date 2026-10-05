using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.HoverTips;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Nodes.HoverTips;
using MegaCrit.Sts2.Core.Nodes.Screens.Map;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace AdjustUnknownChances;

/// <summary>Shows a tooltip with room-type odds when hovering an unvisited unknown (?) map point.</summary>
[HarmonyPatch(typeof(NNormalMapPoint), "OnFocus")]
public static class MapHoverPatch
{
    private static readonly AccessTools.FieldRef<NMapPoint, IRunState> RunStateRef =
        AccessTools.FieldRefAccess<NMapPoint, IRunState>("_runState");

    private static readonly Func<NMapPoint, bool> IsInputAllowed =
        AccessTools.MethodDelegate<Func<NMapPoint, bool>>(AccessTools.Method(typeof(NMapPoint), "IsInputAllowed"));

    private static readonly RoomType[] DisplayOrder =
        [RoomType.Event, RoomType.Monster, RoomType.Elite, RoomType.Treasure, RoomType.Shop];

    private static void Postfix(NNormalMapPoint __instance)
    {
        try
        {
            MapPoint? point = __instance.Point;
            if (point == null || point.PointType != MapPointType.Unknown) return;
            if (__instance.State == MapPointState.Traveled || !IsInputAllowed(__instance)) return;

            IRunState? runState = RunStateRef(__instance);
            if (runState == null) return;

            UnknownOddsPreview preview = OddsCalculator.Compute(runState, point);
            NHoverTipSet.Remove(__instance);
            NHoverTipSet.CreateAndShow(__instance, BuildTip(preview), HoverTip.GetHoverTipAlignment(__instance));
        }
        catch (Exception ex)
        {
            GD.PrintErr($"{ModEntry.LogPrefix} Failed to show unknown odds: {ex}");
        }
    }

    private static IHoverTip BuildTip(UnknownOddsPreview preview)
    {
        var sb = new StringBuilder();
        foreach (RoomType type in DisplayOrder)
        {
            string name = RoomNames.For(type);
            float chance = preview.Chances.GetValueOrDefault(type);
            sb.Append(chance > 0f ? $"[gold]{name}[/gold]: {FormatPercent(chance)}" : $"{name}: 0%");
            sb.Append('\n');
        }

        // Anything the hooks allowed that isn't one of the five usual types (shouldn't happen in vanilla).
        foreach (var (type, chance) in preview.Chances.Where(kv => !DisplayOrder.Contains(kv.Key)))
            sb.Append($"[gold]{RoomNames.For(type)}[/gold]: {FormatPercent(chance)}\n");

        if (preview.IsTutorialOverride)
            sb.Append("\nFirst-run tutorial: this room type is fixed.");
        else if (!preview.IsNextStep)
            sb.Append("\nAssumes this is your next ? room.");
        if (OddsSettings.Current.Enabled)
            sb.Append("\nCustom odds are enabled.");

        return CreateHoverTip(RoomNames.Unknown, sb.ToString().TrimEnd());
    }

    private static string FormatPercent(float chance)
    {
        float percent = chance * 100f;
        return Math.Abs(percent - MathF.Round(percent)) < 0.05f ? $"{MathF.Round(percent):0}%" : $"{percent:0.#}%";
    }

    /// <summary>HoverTip only has LocString constructors and private setters, so fill a default one via reflection.</summary>
    private static IHoverTip CreateHoverTip(string title, string description)
    {
        object boxed = default(HoverTip);
        Type type = typeof(HoverTip);
        type.GetProperty(nameof(HoverTip.Id))!.SetValue(boxed, "AdjustUnknownChances_odds");
        type.GetProperty(nameof(HoverTip.Title))!.GetSetMethod(true)!.Invoke(boxed, [title]);
        type.GetProperty(nameof(HoverTip.Description))!.GetSetMethod(true)!.Invoke(boxed, [description]);
        return (IHoverTip)boxed;
    }
}
