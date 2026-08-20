using System.Collections.Generic;

namespace GtaCollectiblesMap.Core.Model;

/// <summary>The minimal set of renderer operations to move from one marker set to another.</summary>
public sealed record MarkerDiff
{
    public required IReadOnlyList<KeyValuePair<CollectibleKey, MarkerState>> Added { get; init; }
    public required IReadOnlyList<KeyValuePair<CollectibleKey, MarkerState>> Updated { get; init; }
    public required IReadOnlyList<CollectibleKey> Removed { get; init; }

    public bool IsEmpty => Added.Count == 0 && Updated.Count == 0 && Removed.Count == 0;

    public static readonly MarkerDiff Empty = new()
    {
        Added = [],
        Updated = [],
        Removed = [],
    };
}

/// <summary>
/// Computes the difference between the markers that currently exist and the ones that
/// should exist.
/// </summary>
/// <remarks>
/// This exists so refreshes touch only what actually changed. Tearing down and rebuilding
/// every marker each cycle would flicker the map and needlessly churn the game's blip pool,
/// and would make "killing one pigeon removes exactly one blip" impossible to verify.
/// </remarks>
public static class MarkerDiffer
{
    public static MarkerDiff Compute(
        IReadOnlyDictionary<CollectibleKey, MarkerState> actual,
        IReadOnlyDictionary<CollectibleKey, MarkerState> desired)
    {
        List<KeyValuePair<CollectibleKey, MarkerState>> added = [];
        List<KeyValuePair<CollectibleKey, MarkerState>> updated = [];
        List<CollectibleKey> removed = [];

        foreach (KeyValuePair<CollectibleKey, MarkerState> want in desired)
        {
            if (!actual.TryGetValue(want.Key, out MarkerState have))
            {
                added.Add(want);
            }
            else if (!have.Equals(want.Value))
            {
                updated.Add(want);
            }
        }

        foreach (CollectibleKey key in actual.Keys)
        {
            if (!desired.ContainsKey(key))
            {
                removed.Add(key);
            }
        }

        return new MarkerDiff
        {
            Added = added,
            Updated = updated,
            Removed = removed,
        };
    }
}
