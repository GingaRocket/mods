namespace GtaCollectiblesMap.Core.Model;

/// <summary>How one marker should be drawn.</summary>
/// <param name="Colour">Palette index; see <see cref="BlipColours.Reserved"/>.</param>
/// <param name="Scale">Relative size. Collected items shrink so they recede.</param>
/// <param name="Alpha">0..255. Collected items fade.</param>
/// <param name="Display">Map only, map and radar, or hidden.</param>
public readonly record struct MarkerStyle(
    BlipColour Colour,
    float Scale,
    byte Alpha,
    MarkerDisplay Display);
