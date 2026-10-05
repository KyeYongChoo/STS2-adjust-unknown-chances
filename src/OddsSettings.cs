using System;
using System.IO;
using System.Text.Json;
using Godot;
using MegaCrit.Sts2.Core.Odds;

namespace AdjustUnknownChances;

/// <summary>
/// User-configurable base odds (in percent) for unknown map points.
/// Event odds are whatever is left over after Monster/Elite/Treasure/Shop.
/// </summary>
public sealed class OddsConfig
{
    public bool Enabled { get; set; }
    public float Monster { get; set; } = UnknownMapPointOdds.baseMonsterOdds * 100f;
    public float Elite { get; set; } = 0f;
    public float Treasure { get; set; } = UnknownMapPointOdds.baseTreasureOdds * 100f;
    public float Shop { get; set; } = UnknownMapPointOdds.baseShopOdds * 100f;

    public float NonEventTotal => Monster + Elite + Treasure + Shop;
    public float Event => Math.Max(0f, 100f - NonEventTotal);
}

public static class OddsSettings
{
    // Kept out of the mods folder on purpose: the game treats every .json under mods/ as a mod manifest.
    private const string FileName = "AdjustUnknownChances.settings.json";

    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static OddsConfig Current { get; private set; } = new();

    private static string FilePath => Path.Combine(OS.GetUserDataDir(), FileName);

    public static void Load()
    {
        try
        {
            if (File.Exists(FilePath))
                Current = JsonSerializer.Deserialize<OddsConfig>(File.ReadAllText(FilePath), JsonOptions) ?? new OddsConfig();
            Sanitize(Current);
        }
        catch (Exception ex)
        {
            GD.PrintErr($"{ModEntry.LogPrefix} Failed to read settings, using defaults: {ex.Message}");
            Current = new OddsConfig();
        }
    }

    public static void Save()
    {
        try
        {
            File.WriteAllText(FilePath, JsonSerializer.Serialize(Current, JsonOptions));
        }
        catch (Exception ex)
        {
            GD.PrintErr($"{ModEntry.LogPrefix} Failed to save settings: {ex.Message}");
        }
    }

    public static void ResetToVanilla()
    {
        bool enabled = Current.Enabled;
        Current = new OddsConfig { Enabled = enabled };
    }

    /// <summary>Clamp each value to [0, 100] and make sure the non-event total never exceeds 100%.</summary>
    private static void Sanitize(OddsConfig c)
    {
        c.Monster = Math.Clamp(c.Monster, 0f, 100f);
        c.Elite = Math.Clamp(c.Elite, 0f, 100f - c.Monster);
        c.Treasure = Math.Clamp(c.Treasure, 0f, 100f - c.Monster - c.Elite);
        c.Shop = Math.Clamp(c.Shop, 0f, 100f - c.Monster - c.Elite - c.Treasure);
    }
}
