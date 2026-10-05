using System;
using System.Collections.Generic;
using System.Linq;
using MegaCrit.Sts2.Core.Map;
using MegaCrit.Sts2.Core.Hooks;
using MegaCrit.Sts2.Core.Odds;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Runs.History;

namespace AdjustUnknownChances;

public sealed class UnknownOddsPreview
{
    public required IReadOnlyDictionary<RoomType, float> Chances { get; init; }
    /// <summary>The hovered point is reachable right now, so the shop blacklist from the current room is exact.</summary>
    public bool IsNextStep { get; init; }
    /// <summary>The result is forced by the first-run tutorial rules rather than the normal odds.</summary>
    public bool IsTutorialOverride { get; init; }
}

/// <summary>
/// Predicts what <see cref="UnknownMapPointOdds.Roll"/> would produce if the given point were entered next,
/// without consuming any RNG or changing state.
/// </summary>
public static class OddsCalculator
{
    /// <summary>The order Roll() walks the odds in (dictionary insertion order in UnknownMapPointOdds).</summary>
    public static readonly RoomType[] NonEventOrder = [RoomType.Monster, RoomType.Elite, RoomType.Treasure, RoomType.Shop];

    public static UnknownOddsPreview Compute(IRunState runState, MapPoint target)
    {
        bool isNextStep = runState.CurrentMapPoint?.Children.Contains(target) ?? target.coord.row == 0;

        // First-ever run: the first two unknown rooms are events, the third is a monster.
        if (runState.UnlockState.NumberOfRuns == 0)
        {
            int unknownsVisited = runState.MapPointHistory.SelectMany(l => l).Count(p => p.MapPointType == MapPointType.Unknown);
            if (unknownsVisited <= 2)
            {
                return new UnknownOddsPreview
                {
                    Chances = new Dictionary<RoomType, float> { [unknownsVisited < 2 ? RoomType.Event : RoomType.Monster] = 1f },
                    IsNextStep = isNextStep,
                    IsTutorialOverride = true,
                };
            }
        }

        // When the room is actually entered, the "previous" entry is the room we're in now.
        // For points further away the previous room is unknown, so only the next-points rule can be applied.
        MapPointHistoryEntry? previousEntry = isNextStep ? runState.CurrentMapPointHistoryEntry : null;
        HashSet<RoomType> blacklist = RunManager.BuildRoomTypeBlacklist(previousEntry, target.Children);

        IReadOnlySet<RoomType> roomTypes = NonEventOrder.Append(RoomType.Event).Except(blacklist).ToHashSet();
        roomTypes = Hook.ModifyUnknownMapPointRoomTypes(runState, roomTypes);

        UnknownMapPointOdds odds = runState.Odds.UnknownMapPoint;
        var chances = new Dictionary<RoomType, float>();
        float cumulative = 0f;
        foreach (RoomType type in NonEventOrder)
        {
            float value = GetOdds(odds, type);
            if (!roomTypes.Contains(type) || value < 0f) continue;
            float low = Math.Min(cumulative, 1f);
            cumulative += value;
            chances[type] = Math.Min(cumulative, 1f) - low;
        }

        // Anything the roll doesn't land on falls through to Event, or to the lowest allowed type if Event is excluded.
        float remainder = 1f - Math.Min(cumulative, 1f);
        if (remainder > 0f && roomTypes.Count > 0)
        {
            RoomType fallback = roomTypes.Contains(RoomType.Event) ? RoomType.Event : roomTypes.Order().First();
            chances[fallback] = chances.GetValueOrDefault(fallback) + remainder;
        }

        return new UnknownOddsPreview { Chances = chances, IsNextStep = isNextStep };
    }

    private static float GetOdds(UnknownMapPointOdds odds, RoomType type) => type switch
    {
        RoomType.Monster => odds.MonsterOdds,
        RoomType.Elite => odds.EliteOdds,
        RoomType.Treasure => odds.TreasureOdds,
        RoomType.Shop => odds.ShopOdds,
        _ => 0f,
    };
}
