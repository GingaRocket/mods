using System;
using System.Collections.Generic;
using GtaCollectiblesMap.Core.Abstractions;
using GtaCollectiblesMap.Core.Config;
using GtaCollectiblesMap.Core.Model;

namespace GtaCollectiblesMap.Features;

/// <summary>
/// Drives one collectible type: read the game, work out what is left, and reconcile the
/// markers on the map.
/// </summary>
/// <remarks>
/// <para>
/// One class serves both collectible types. The awkward difference between them — birds need
/// a known-coordinate table because collected ones are deleted from the pickup pool, stunt
/// jumps do not because the game keeps all fifty with a passed flag — is absorbed by the
/// optional <paramref name="table"/>. Nothing downstream branches on collectible type.
/// </para>
/// </remarks>
public sealed class CollectibleTracker(
    string name,
    string markerName,
    ICollectibleSource source,
    IMarkerRenderer renderer,
    KnownCollectibleTable? table,
    Func<ModSettings, bool> isEnabled,
    ILog log) : ICollectibleTracker
{
    /// <summary>
    /// Key offset for items the coordinate table does not recognise, keeping them from
    /// colliding with real table indices.
    /// </summary>
    private const int UnmatchedKeyBase = 100_000;

    private Dictionary<CollectibleKey, MarkerState> _current = [];

    private bool _warnedAboutUnmatched;

    public string Name { get; } = name;

    public TrackerStatus Status { get; private set; } = TrackerStatus.Waiting;

    public void Refresh(IGameContext context, ModSettings settings)
    {
        if (!isEnabled(settings))
        {
            Clear();
            Status = TrackerStatus.Disabled;
            return;
        }

        ReadResult read = source.Read();
        if (!read.Success)
        {
            Clear();
            Status = TrackerStatus.Failed(read.Error ?? "source read failed");
            return;
        }

        IReadOnlyList<Collectible> collectibles = Resolve(read.Items);
        Dictionary<CollectibleKey, MarkerState> desired = BuildMarkers(collectibles, context, settings);
        Apply(desired);

        int remaining = 0;
        int collected = 0;
        foreach (Collectible collectible in collectibles)
        {
            if (!collectible.IsCollected)
            {
                remaining++;
            }
            else
            {
                collected++;
            }
        }

        Status = new TrackerStatus(TrackerState.Ok, null, remaining, collectibles.Count);
    }

    public void Clear()
    {
        if (_current.Count == 0)
        {
            return;
        }

        foreach (CollectibleKey key in _current.Keys)
        {
            renderer.Remove(key);
        }

        _current = [];
    }

    /// <summary>
    /// Turns raw source items into collectibles with stable identities and a collected flag.
    /// </summary>
    private IReadOnlyList<Collectible> Resolve(IReadOnlyList<SourceItem> items)
    {
        CollectibleCategory category = source.Category;

        if (table is null)
        {
            // The source tracks the whole set itself and reports collected state directly.
            List<Collectible> direct = new(items.Count);
            foreach (SourceItem item in items)
            {
                direct.Add(new Collectible(
                    new CollectibleKey(category, item.SourceIndex),
                    item.Position,
                    item.Label ?? $"{markerName} {item.SourceIndex + 1}",
                    item.IsCollected));
            }

            return direct;
        }

        // Pool-backed: everything the source returned is still present, so still uncollected.
        HashSet<int> present = [];
        List<SourceItem> unmatched = [];

        foreach (SourceItem item in items)
        {
            if (table.TryMatch(item.Position, out int index))
            {
                present.Add(index);
            }
            else
            {
                unmatched.Add(item);
            }
        }

        List<Collectible> resolved = new(table.Count + unmatched.Count);

        for (int i = 0; i < table.Count; i++)
        {
            KnownCollectible entry = table.Entries[i];
            resolved.Add(new Collectible(
                new CollectibleKey(category, i),
                entry.Position,
                entry.Label,
                !present.Contains(i)));
        }

        // An item the table does not know about is still a real, uncollected collectible.
        // Show it rather than hiding it: a stale or mistyped table entry must never cause the
        // mod to silently omit something the player still has to find.
        foreach (SourceItem item in unmatched)
        {
            resolved.Add(new Collectible(
                new CollectibleKey(category, UnmatchedKeyBase + item.SourceIndex),
                item.Position,
                item.Label ?? "Unlisted",
                IsCollected: false));
        }

        if (unmatched.Count > 0 && !_warnedAboutUnmatched)
        {
            _warnedAboutUnmatched = true;
            log.Warn(
                $"{Name}: {unmatched.Count} item(s) did not match the known-coordinate table "
                + "and are shown as unlisted. The table may be out of date for this episode.");
        }

        return resolved;
    }

    private Dictionary<CollectibleKey, MarkerState> BuildMarkers(
        IReadOnlyList<Collectible> collectibles,
        IGameContext context,
        ModSettings settings)
    {
        Dictionary<CollectibleKey, MarkerState> desired = [];
        float radiusSquared = settings.RadarRadius * settings.RadarRadius;
        Vec3 player = context.PlayerPosition;
        foreach (Collectible collectible in collectibles)
        {
            if (collectible.IsCollected && !settings.ShowAll)
            {
                continue;
            }

            bool isNear = player.FlatDistanceSquaredTo(collectible.Position) <= radiusSquared;

            MarkerStyle style = MarkerStyleResolver.Resolve(
                source.Category,
                collectible.IsCollected,
                isNear,
                settings);

            desired[collectible.Key] = new MarkerState(
                collectible.Position,
                collectible.Label,
                markerName,
                style);
        }

        return desired;
    }

    private void Apply(Dictionary<CollectibleKey, MarkerState> desired)
    {
        MarkerDiff diff = MarkerDiffer.Compute(_current, desired);

        foreach (KeyValuePair<CollectibleKey, MarkerState> pair in diff.Added)
        {
            renderer.Add(pair.Key, pair.Value);
        }

        foreach (KeyValuePair<CollectibleKey, MarkerState> pair in diff.Updated)
        {
            renderer.Update(pair.Key, pair.Value);
        }

        foreach (CollectibleKey key in diff.Removed)
        {
            renderer.Remove(key);
        }

        _current = desired;
    }
}
