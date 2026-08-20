using GtaCollectiblesMap.Core.Abstractions;
using GtaCollectiblesMap.Core.Config;

namespace GtaCollectiblesMap.Features;

/// <summary>
/// One tracked collectible type. The host holds a list of these and never branches on
/// what they are, so adding a new type is a new source plus one registration line.
/// </summary>
public interface ICollectibleTracker
{
    string Name { get; }

    TrackerStatus Status { get; }

    void Refresh(IGameContext context, ModSettings settings);

    /// <summary>Removes this tracker's markers without disabling it.</summary>
    void Clear();
}
