using System;
using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace AdjustUnknownChances;

/// <summary>
/// Applies the user's custom odds to a run. Runs after the vanilla modifier hooks, so custom values
/// take precedence over e.g. Deadly Events' elite odds.
/// </summary>
public static class RunOddsPatches
{
    private static readonly Func<RunManager, RunState?> GetState =
        AccessTools.MethodDelegate<Func<RunManager, RunState?>>(AccessTools.PropertyGetter(typeof(RunManager), "State"));

    /// <param name="setCurrentOdds">
    /// True for a brand new run. For a loaded run, the current odds come from the save file and only the
    /// base odds (used for resets and per-room increases) are set.
    /// </param>
    private static void Apply(RunManager runManager, bool setCurrentOdds)
    {
        OddsConfig config = OddsSettings.Current;
        if (!config.Enabled) return;

        RunState? state = GetState(runManager);
        if (state == null) return;

        UnknownMapPointOdds odds = state.Odds.UnknownMapPoint;
        Set(odds, RoomType.Monster, config.Monster, setCurrentOdds);
        Set(odds, RoomType.Elite, config.Elite, setCurrentOdds);
        Set(odds, RoomType.Treasure, config.Treasure, setCurrentOdds);
        Set(odds, RoomType.Shop, config.Shop, setCurrentOdds);
        GD.Print($"{ModEntry.LogPrefix} Applied custom odds: Monster {config.Monster}%, Elite {config.Elite}%, " +
                 $"Treasure {config.Treasure}%, Shop {config.Shop}%, Event {config.Event}%");
    }

    private static void Set(UnknownMapPointOdds odds, RoomType type, float percent, bool setCurrent)
    {
        // Negative odds mean "never rolled and never increases", matching how vanilla disables elites.
        float value = percent <= 0f ? -1f : percent / 100f;
        odds.SetBaseOdds(type, value);
        if (!setCurrent) return;
        switch (type)
        {
            case RoomType.Monster: odds.MonsterOdds = value; break;
            case RoomType.Elite: odds.EliteOdds = value; break;
            case RoomType.Treasure: odds.TreasureOdds = value; break;
            case RoomType.Shop: odds.ShopOdds = value; break;
        }
    }

    [HarmonyPatch(typeof(RunManager), "InitializeNewRun")]
    private static class InitializeNewRunPatch
    {
        private static void Postfix(RunManager __instance)
        {
            try { Apply(__instance, setCurrentOdds: true); }
            catch (Exception ex) { GD.PrintErr($"{ModEntry.LogPrefix} Failed to apply odds to new run: {ex}"); }
        }
    }

    [HarmonyPatch(typeof(RunManager), "InitializeSavedRun")]
    private static class InitializeSavedRunPatch
    {
        private static void Postfix(RunManager __instance)
        {
            try { Apply(__instance, setCurrentOdds: false); }
            catch (Exception ex) { GD.PrintErr($"{ModEntry.LogPrefix} Failed to apply odds to loaded run: {ex}"); }
        }
    }
}
