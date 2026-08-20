namespace GtaCollectiblesMap.Core.Model;

/// <summary>Where a marker should be visible.</summary>
/// <remarks>
/// Deliberately expressed in intent, not in GTA IV's numeric display flags. Translating
/// to the game's values is the Game layer's job.
/// </remarks>
public enum MarkerDisplay
{
    Hidden,
    MapOnly,
    MapAndRadar,
}
