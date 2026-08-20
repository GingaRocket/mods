namespace GtaCollectiblesMap.Core.Model;

/// <summary>The full desired state of one on-map marker.</summary>
public readonly record struct MarkerState(
    Vec3 Position,
    string Label,
    string Name,
    MarkerStyle Style);
