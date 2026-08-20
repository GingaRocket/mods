using System;
using System.Collections.Generic;
using GtaCollectiblesMap.Core.Abstractions;
using GtaCollectiblesMap.Core.Config;
using GtaCollectiblesMap.Core.Model;

namespace GtaCollectiblesMap.Features;

/// <summary>
/// Owns the live trackers: rebuilds them when the episode changes, and refreshes each in
/// isolation.
/// </summary>
/// <remarks>
/// Extracted from the host so this behaviour is testable. Both responsibilities here are easy
/// to get subtly wrong and impossible to notice in game — a missed rebuild would silently show
/// GTA IV pigeon positions while playing an episode, and a missing catch would let one broken
/// tracker take the others down with it.
/// </remarks>
public sealed class TrackerCoordinator(ICollectibleTrackerFactory factory, ILog log)
{
    private IReadOnlyList<ICollectibleTracker> _trackers = [];

    private Episode? _builtFor;

    public IReadOnlyList<ICollectibleTracker> Trackers => _trackers;

    public Episode? BuiltForEpisode => _builtFor;

    public void Refresh(IGameContext context, ModSettings settings)
    {
        EnsureBuiltFor(context.Episode);

        foreach (ICollectibleTracker tracker in _trackers)
        {
            // Isolated on purpose: a failure in one tracker must never stop the others drawing.
            try
            {
                tracker.Refresh(context, settings);
            }
            catch (Exception ex)
            {
                log.Error($"{tracker.Name} refresh threw: {ex.Message}");
            }
        }
    }

    /// <summary>Drops every tracker's markers and forgets them, so the next refresh rebuilds.</summary>
    public void Reset()
    {
        ClearTrackers();
        _trackers = [];
        _builtFor = null;
    }

    private void EnsureBuiltFor(Episode episode)
    {
        if (_builtFor == episode)
        {
            return;
        }

        if (_builtFor is not null)
        {
            // Marker identity is the index into an episode-specific coordinate table, so the
            // old markers are meaningless under the new episode. Tear down rather than reconcile.
            log.Info($"episode changed to {episode}; rebuilding collectible tables.");
            ClearTrackers();
        }

        _trackers = factory.Create(episode);
        _builtFor = episode;
    }

    private void ClearTrackers()
    {
        foreach (ICollectibleTracker tracker in _trackers)
        {
            try
            {
                tracker.Clear();
            }
            catch (Exception ex)
            {
                log.Error($"clearing {tracker.Name} failed: {ex.Message}");
            }
        }
    }
}
