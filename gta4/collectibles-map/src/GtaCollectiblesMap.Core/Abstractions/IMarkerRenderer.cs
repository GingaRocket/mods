using System.Collections.Generic;
using GtaCollectiblesMap.Core.Model;

namespace GtaCollectiblesMap.Core.Abstractions;

/// <summary>
/// Draws markers on the map. Implemented by the Game layer over the radar blip pool;
/// faked wholesale in tests.
/// </summary>
public interface IMarkerRenderer
{
    void Add(CollectibleKey key, MarkerState state);

    void Update(CollectibleKey key, MarkerState state);

    void Remove(CollectibleKey key);

    /// <summary>
    /// Drops every marker this renderer created. Must be called on shutdown and on script
    /// reload — orphaned blips survive the script and litter the map permanently.
    /// </summary>
    void RemoveAll();

    /// <summary>
    /// Deletes leftover destination-2 blips at collected stunt-jump positions that this
    /// renderer is not tracking (empty hover name). Complete Edition can keep those after
    /// a failed delete.
    /// </summary>
    void SweepNamelessOrphans(
        CollectibleCategory category,
        IReadOnlyList<Vec3> collectedPositions);

    int ActiveCount { get; }
}
