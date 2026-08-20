namespace GtaCollectiblesMap.Core.Model;

/// <summary>One collectible and whether the player has already got it.</summary>
public sealed record Collectible(
    CollectibleKey Key,
    Vec3 Position,
    string Label,
    bool IsCollected);
