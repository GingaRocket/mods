using System.Collections.Generic;
using GtaCollectiblesMap.Core.Model;

namespace GtaCollectiblesMap.Features;

/// <summary>
/// Builds the set of trackers appropriate to an episode.
/// </summary>
/// <remarks>
/// A port, not a concrete class, because the trackers need game-backed sources and this
/// assembly must not reference the game SDK. The composition root supplies the implementation.
/// </remarks>
public interface ICollectibleTrackerFactory
{
    IReadOnlyList<ICollectibleTracker> Create(Episode episode);
}
