using System;
using MegaCrit.Sts2.Core.Localization;
using MegaCrit.Sts2.Core.Rooms;

namespace AdjustUnknownChances;

/// <summary>
/// Room type names in the player's language, reusing the game's own map legend strings.
/// Looked up on every call so a language change applies immediately.
/// </summary>
public static class RoomNames
{
    public static string Unknown => Get("LEGEND_UNKNOWN.title", "Unknown");

    public static string For(RoomType type) => type switch
    {
        RoomType.Event => Get("EVENT.title", "Event"),
        RoomType.Monster => Get("LEGEND_ENEMY.title", "Monster"),
        RoomType.Elite => Get("LEGEND_ELITE.title", "Elite"),
        RoomType.Treasure => Get("LEGEND_TREASURE.title", "Treasure"),
        RoomType.Shop => Get("LEGEND_MERCHANT.title", "Shop"),
        _ => type.ToString(),
    };

    private static string Get(string key, string fallback)
    {
        try
        {
            return LocString.GetIfExists("map", key)?.GetFormattedText() ?? fallback;
        }
        catch (Exception)
        {
            // Loc tables aren't available (e.g. very early in startup).
            return fallback;
        }
    }
}
