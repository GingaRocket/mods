using System.Collections.Generic;
using GtaCollectiblesMap.Core.Model;

namespace GtaCollectiblesMap.Features;

/// <summary>One entry in a known-coordinate table.</summary>
public readonly record struct KnownCollectible(Vec3 Position, string Label);

/// <summary>
/// The complete set of positions for a collectible type, used to work out what has already
/// been collected.
/// </summary>
/// <remarks>
/// Needed only for pool-backed sources. A collected pigeon is deleted from the game's pickup
/// array outright, so its position is unrecoverable at runtime — the pool can only ever say
/// what remains. Showing collected birds therefore requires knowing the full set up front and
/// subtracting what is still present.
/// </remarks>
public sealed class KnownCollectibleTable(IReadOnlyList<KnownCollectible> entries)
{
    /// <summary>
    /// Positions are compared with slack because table values are recorded to two decimals
    /// while the game reports full float precision.
    /// </summary>
    public const float MatchEpsilon = 0.5f;

    private const float MatchEpsilonSquared = MatchEpsilon * MatchEpsilon;

    public IReadOnlyList<KnownCollectible> Entries { get; } = entries;

    public int Count => Entries.Count;

    /// <summary>
    /// Finds the table entry nearest <paramref name="position"/> within the epsilon.
    /// </summary>
    /// <remarks>
    /// Brute force on purpose: the table is at most 200 entries and matching runs once per
    /// refresh, roughly every 1.5 seconds. A spatial index would add a failure mode for no
    /// measurable gain. Returns the *nearest* match rather than the first so that two entries
    /// closer together than the epsilon still resolve deterministically.
    /// </remarks>
    public bool TryMatch(Vec3 position, out int index)
    {
        index = -1;
        float best = MatchEpsilonSquared;

        for (int i = 0; i < Entries.Count; i++)
        {
            float distance = Entries[i].Position.DistanceSquaredTo(position);
            if (distance <= best)
            {
                best = distance;
                index = i;
            }
        }

        return index >= 0;
    }
}
