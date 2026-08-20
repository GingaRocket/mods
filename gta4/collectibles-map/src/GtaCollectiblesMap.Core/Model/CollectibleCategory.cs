namespace GtaCollectiblesMap.Core.Model;

/// <summary>
/// A kind of collectible the mod can track.
/// </summary>
/// <remarks>
/// Birds cover pigeons (GTA IV) and seagulls (TLAD/TBOGT) — only one is ever active
/// at a time, decided by the running episode, so they share a category rather than
/// splitting into three.
/// </remarks>
public enum CollectibleCategory
{
    Bird,
    StuntJump,
}
