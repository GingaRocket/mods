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

    int ActiveCount { get; }
}
