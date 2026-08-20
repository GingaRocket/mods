namespace GtaCollectiblesMap.Core.Model;

/// <summary>
/// Stable identity for one collectible, used to diff successive scans.
/// </summary>
/// <remarks>
/// <para>
/// Stability matters more than it looks. Birds are matched back to their entry in the
/// known-coordinate table, so a bird keeps the same index across scans even though the
/// underlying pickup array is re-walked every time. Stunt jumps use their pool index.
/// </para>
/// <para>
/// Without a stable key the diff would report every marker as removed-and-re-added on
/// each refresh, which makes blips flicker and churns the radar pool.
/// </para>
/// </remarks>
public readonly record struct CollectibleKey(CollectibleCategory Category, int Index)
{
    public override string ToString() => $"{Category}:{Index}";
}
