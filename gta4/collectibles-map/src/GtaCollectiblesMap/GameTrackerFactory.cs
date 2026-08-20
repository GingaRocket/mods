using System.Collections.Generic;
using GtaCollectiblesMap.Core.Abstractions;
using GtaCollectiblesMap.Core.Model;
using GtaCollectiblesMap.Features;
using GtaCollectiblesMap.Game;

namespace GtaCollectiblesMap;

/// <summary>
/// Builds the trackers over game-backed sources.
/// </summary>
/// <remarks>
/// Lives in the composition root because it is the only layer allowed to know both the
/// orchestration contracts and the game adapters that satisfy them.
/// </remarks>
public sealed class GameTrackerFactory(IMarkerRenderer renderer, ILog log) : ICollectibleTrackerFactory
{
    public IReadOnlyList<ICollectibleTracker> Create(Episode episode)
    {
        KnownCollectibleTable birdTable = CollectibleTables.ForBirds(episode);
        List<Vec3> birdPositions = [];
        foreach (KnownCollectible bird in birdTable.Entries)
        {
            birdPositions.Add(bird.Position);
        }

        List<ICollectibleTracker> trackers =
        [
            new CollectibleTracker(
                episode == Episode.Iv ? "Flying Rats" : "Seagulls",
                episode == Episode.Iv ? "Flying Rat" : "Seagull",
                new PickupCollectibleSource(birdPositions, log),
                renderer,
                birdTable,
                settings => settings.ShowBirds,
                log),
        ];

        // Unique stunt jumps are a GTA IV collectible; the episodes replace them with their own
        // seagulls and ship neither the fifty launch boxes nor the statistic the locator
        // validates against. Building the tracker there could only ever fail - leaving a
        // permanent red line in the Status panel and re-scanning the whole address space every
        // thirty seconds for a pool that does not exist.
        if (episode == Episode.Iv)
        {
            trackers.Add(new CollectibleTracker(
                "Stunt Jumps",
                "Stunt Jump",
                new StuntJumpCollectibleSource(log),
                renderer,
                // No table: the game keeps every jump in memory with its own passed flag.
                table: null,
                settings => settings.ShowStuntJumps,
                log));
        }

        return trackers;
    }
}
