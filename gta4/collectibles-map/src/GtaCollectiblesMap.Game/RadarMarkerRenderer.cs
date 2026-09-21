using System;
using System.Collections.Generic;
using GtaCollectiblesMap.Core.Abstractions;
using GtaCollectiblesMap.Core.Model;
using GTA;

namespace GtaCollectiblesMap.Game;

/// <summary>Draws markers as radar blips.</summary>
/// <remarks>
/// Every blip handed out here is tracked. Blips outlive the script that made them, so anything
/// not explicitly removed stays on the player's map until the game restarts — which is why
/// <see cref="RemoveAll"/> must run on shutdown and on script reload.
/// </remarks>
public sealed class RadarMarkerRenderer(ILog log) : IMarkerRenderer
{
    /// <summary>
    /// How far a blip may sit from where we last put it and still be recognised as ours.
    /// Generous on purpose: this is proving the slot was not handed to something else, not
    /// checking float precision.
    /// </summary>
    private const float OwnershipEpsilonSquared = 4f;

    /// <summary>
    /// Objective sprites occupy about 1.8 times the width of destination sprites at the same
    /// game scale. Measured from matching 3840x2160 pause-map captures: roughly 44-47 pixels
    /// versus 23-25 pixels. This keeps their temporary taxi footprint visually equivalent.
    /// </summary>
    private const float TaxiSafeScaleMultiplier = 0.55f;

    /// <summary>
    /// Every blip type, because Complete Edition has historically misreported restored blips.
    /// Anything that inventories the map has to look everywhere and report the sprite too.
    /// </summary>
    private static readonly BlipType[] BlipTypes =
    [
        BlipType.Coordinate, BlipType.Contact, BlipType.Object, BlipType.Pickup,
        BlipType.Pickup2, BlipType.Vehicle, BlipType.Ped, BlipType.Unknown,
    ];

    private readonly Dictionary<CollectibleKey, Marker> _markers = [];

    private bool _taxiSafeIcons;

    private bool _warnedAboutCreation;

    private bool _warnedAboutStaleHandle;

    public int ActiveCount => _markers.Count;

    /// <summary>
    /// Logs every blip currently on the map, whatever its type. Reads only — never deletes.
    /// </summary>
    /// <remarks>
    /// A census, not a sweep. There is deliberately no counterpart that removes what it finds:
    /// blips can only be identified from outside by sprite, and GTA IV draws its own friend and
    /// activity blips with the same destination sprites this mod uses. A sprite match cannot
    /// establish ownership. Nothing here may delete a blip this renderer did not create and
    /// is not still tracking.
    /// </remarks>
    public void LogBlipCensus()
    {
        int logged = 0;
        foreach (BlipType type in BlipTypes)
        {
            Blip[]? blips;
            try
            {
                blips = Blip.GetAllBlipsOfType(type);
            }
            catch (Exception ex)
            {
                log.Info($"census: enumerating {type} failed: {ex.Message}");
                continue;
            }

            if (blips is null || blips.Length == 0)
            {
                continue;
            }

            log.Info($"census: {blips.Length} blip(s) of type {type}");
            foreach (Blip blip in blips)
            {
                if (logged++ >= 120)
                {
                    log.Info("census: truncated.");
                    return;
                }

                // Each property separately: a blip the SDK misreads on Complete Edition may
                // throw on one and answer another, and giving up on the first failure would
                // hide exactly the blips worth finding.
                log.Info(
                    $"census: {type} icon={Probe(() => blip.Icon.ToString())} "
                    + $"colour={Probe(() => blip.Color.ToString())} "
                    + $"display={Probe(() => ((int)blip.Display).ToString())} "
                    + $"position={Probe(() => blip.Position.ToString())} "
                    + $"ours={IsMarkerSprite(Probe(() => blip.Icon.ToString()))}");
            }
        }

        if (logged == 0)
        {
            log.Info("census: the game reports no blips at all.");
        }
    }

    /// <summary>
    /// Reports what the game says about the markers this renderer created and still owns.
    /// </summary>
    /// <remarks>
    /// The census can only see blips from outside, so it can never say whether one is ours.
    /// This can: every blip here was created by this renderer and has been verified as still
    /// ours. What the game reports for them is therefore ground truth for reading any other
    /// blip — in particular whether a coordinate blip of ours comes back as
    /// <see cref="BlipType.Coordinate"/> or as something else entirely.
    /// </remarks>
    public void LogOwnMarkerFacts()
    {
        Dictionary<string, int> byType = [];
        Dictionary<string, int> byIcon = [];
        int foreign = 0;
        int gone = 0;

        foreach (KeyValuePair<CollectibleKey, Marker> pair in _markers)
        {
            switch (Classify(pair.Value))
            {
                case Ownership.Foreign: foreign++; break;
                case Ownership.Gone: gone++; break;
                default: break;
            }

            string type = Probe(() => pair.Value.Blip.Type.ToString());
            string icon = Probe(() => pair.Value.Blip.Icon.ToString());
            byType[type] = byType.TryGetValue(type, out int t) ? t + 1 : 1;
            byIcon[icon] = byIcon.TryGetValue(icon, out int i) ? i + 1 : 1;
        }

        log.Info($"own markers: {_markers.Count} tracked, {foreign} foreign, {gone} gone");
        log.Info($"own markers by reported type: {Describe(byType)}");
        log.Info($"own markers by reported icon: {Describe(byIcon)}");
    }

    private static string Describe(Dictionary<string, int> counts)
    {
        List<string> parts = [];
        foreach (KeyValuePair<string, int> pair in counts)
        {
            parts.Add($"{pair.Key}={pair.Value}");
        }

        return parts.Count == 0 ? "none" : string.Join(", ", parts);
    }

    private static string Probe(Func<string> read)
    {
        try
        {
            return read();
        }
        catch (Exception ex)
        {
            return $"<{ex.GetType().Name}>";
        }
    }

    public void Add(CollectibleKey key, MarkerState state)
    {
        // Defensive: a leaked handle here would be unreachable for cleanup.
        if (_markers.ContainsKey(key))
        {
            Update(key, state);
            return;
        }

        Blip? blip = null;
        try
        {
            blip = Blip.AddBlip(state.Position.ToGameVector());

            if (blip is null)
            {
                WarnAboutCreation($"the game refused a blip for {key}");
                return;
            }

            if (!blip.Exists())
            {
                // Never walk away from it. ScriptHookDotNet predates Complete Edition, so a
                // blip it reports as non-existent may well be a real one on the map — and at
                // this point it still carries sprite 0 and no name. That combination is the
                // worst possible orphan: invisible (sprite 0 draws no artwork on CE) yet still
                // hoverable, showing an empty box that nothing can ever update or remove.
                WarnAboutCreation($"discarding a blip for {key} that reported as not existing");
                try
                {
                    blip.Delete();
                }
                catch
                {
                    // Nothing further to try; it was already doubtful.
                }

                return;
            }

            ApplyIcon(blip, key.Category, _taxiSafeIcons);
            CompleteEditionBlipInterop.SetName(blip, state.Name);
            // The mod applies its configurable radius through Display, so disable the game's
            // separate fixed short-range filter.
            blip.ShowOnlyWhenNear = false;
            ApplyStyle(blip, state.Style, _taxiSafeIcons);
            // A blip is created fully opaque, so that is the alpha already in effect.
            ApplyAlpha(blip, applied: byte.MaxValue, wanted: state.Style.Alpha);
            _markers[key] = new Marker(blip, state);
        }
        catch (Exception ex)
        {
            try
            {
                blip?.Delete();
            }
            catch
            {
                // Preserve the original creation error in the log.
            }

            log.Error($"creating blip for {key} failed: {ex.Message}");
        }
    }

    public void Update(CollectibleKey key, MarkerState state)
    {
        if (!_markers.TryGetValue(key, out Marker? marker))
        {
            Add(key, state);
            return;
        }

        if (Classify(marker) != Ownership.Ours)
        {
            // Never write through a handle we cannot prove is ours. Forget it and build a
            // fresh marker instead of stamping our icon onto whatever now holds that slot.
            _markers.Remove(key);
            WarnAboutStaleHandle($"the blip handle for {key} no longer refers to our marker; recreating it");
            Add(key, state);
            return;
        }

        try
        {
            // The diff guarantees something changed but not what, and every one of these is a
            // native call on the Complete Edition compatibility path, so each part is compared
            // against what was last actually pushed to the game.
            bool mutated = false;

            if (marker.State.Position != state.Position)
            {
                // Marker identity is a table index or a pool slot, not a coordinate. A pool
                // that re-packs after a save is reloaded can hand the same slot to a different
                // stunt jump, and its blip has to follow rather than sit at the old launch box.
                marker.Blip.Position = state.Position.ToGameVector();
                log.Debug($"{key} moved from {marker.State.Position} to {state.Position}");
                mutated = true;
            }

            if (marker.State.Style != state.Style)
            {
                ApplyStyle(marker.Blip, state.Style, _taxiSafeIcons);
                ApplyAlpha(marker.Blip, marker.State.Style.Alpha, state.Style.Alpha);
                mutated = true;
            }

            // Restored after any mutation, not merely when the name itself changed. On Complete
            // Edition a blip that has been altered in place can come back with an empty hover
            // label, so the name is re-established last and unconditionally. The string is
            // already interned as a process-lifetime pointer, so this is one native call with
            // no allocation behind it.
            if (mutated || !string.Equals(marker.State.Name, state.Name, StringComparison.Ordinal))
            {
                CompleteEditionBlipInterop.SetName(marker.Blip, state.Name);
            }

            // Recorded last: if a native above threw, the next refresh has to still see the
            // parts that did not make it as outstanding.
            marker.State = state;
        }
        catch (Exception ex)
        {
            log.Error($"updating blip for {key} failed: {ex.Message}");
        }
    }

    public void Remove(CollectibleKey key)
    {
        if (!_markers.TryGetValue(key, out Marker? marker))
        {
            return;
        }

        // Drop it from tracking first. If the native throws we must not keep trying to remove
        // a handle forever, and a stale entry would block a later Add for the same key.
        _markers.Remove(key);

        Ownership ownership = Classify(marker);
        if (ownership == Ownership.Foreign)
        {
            // A live blip somewhere other than where we put ours: the slot has been reused.
            // Deleting it would destroy a ped or vehicle blip that was never ours to remove.
            WarnAboutStaleHandle($"not deleting the blip for {key}: the handle now belongs to something else");
            return;
        }

        try
        {
            // Hidden before it is deleted. Tracking has just been dropped, so if the removal
            // does not actually take effect on Complete Edition, nothing in the mod can ever
            // style, name or remove that blip again — it becomes a permanent leftover the
            // player can still hover. Hiding first makes the worst case an inert invisible
            // blip instead of one that sits on the map with a stale colour and no label.
            CompleteEditionBlipInterop.SetDisplay(marker.Blip, MarkerDisplay.Hidden);
        }
        catch (Exception ex)
        {
            // Best effort only, but worth recording: if hiding failed and the delete below
            // also fails to take, this is the blip that turns into a visible leftover.
            log.Debug($"could not hide {key} before removing it: {ex.Message}");
        }

        try
        {
            marker.Blip.Delete();
            CompleteEditionBlipInterop.RemoveBlip(marker.Blip);
        }
        catch (Exception ex)
        {
            log.Error($"removing blip for {key} failed: {ex.Message}");
        }
    }

    public void SweepNamelessOrphans(
        CollectibleCategory category,
        IReadOnlyList<Vec3> collectedPositions)
    {
        if (category != CollectibleCategory.StuntJump || collectedPositions.Count == 0)
        {
            return;
        }

        const float radiusSquared = 10000f;

        foreach (BlipType type in BlipTypes)
        {
            Blip[]? blips;
            try
            {
                blips = Blip.GetAllBlipsOfType(type);
            }
            catch
            {
                continue;
            }

            if (blips is null)
            {
                continue;
            }

            foreach (Blip blip in blips)
            {
                try
                {
                    if (!blip.Exists())
                    {
                        continue;
                    }

                    if (Probe(() => blip.Icon.ToString()) != nameof(BlipIcon.Misc_Destination2))
                    {
                        continue;
                    }

                    Vec3 pos = blip.Position.ToVec3();
                    if (IsTrackedAt(pos) || !IsNearCollected(pos, collectedPositions, radiusSquared))
                    {
                        continue;
                    }

                    CompleteEditionBlipInterop.SetDisplay(blip, MarkerDisplay.Hidden);
                    CompleteEditionBlipInterop.RemoveBlip(blip);
                    try
                    {
                        blip.Delete();
                    }
                    catch
                    {
                        // Native REMOVE_BLIP already ran.
                    }
                }
                catch
                {
                    // Best effort; a live taxi blip must not abort the rest of the sweep.
                }
            }
        }
    }

    private bool IsTrackedAt(Vec3 position)
    {
        foreach (Marker marker in _markers.Values)
        {
            if (position.DistanceSquaredTo(marker.State.Position) <= OwnershipEpsilonSquared)
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsNearCollected(
        Vec3 position,
        IReadOnlyList<Vec3> collectedPositions,
        float radiusSquared)
    {
        foreach (Vec3 collected in collectedPositions)
        {
            if (position.DistanceSquaredTo(collected) <= radiusSquared)
            {
                return true;
            }
        }

        return false;
    }

    public void RemoveAll()
    {
        foreach (CollectibleKey key in new List<CollectibleKey>(_markers.Keys))
        {
            Remove(key);
        }

        _markers.Clear();
    }

    /// <summary>
    /// Switches between the normal rounded sprites and variants ignored by the taxi scripts.
    /// </summary>
    /// <remarks>
    /// This mutates existing blips instead of replacing them, preserving ownership handles and
    /// avoiding another create/delete cycle. Names are restored after the sprite write because
    /// Complete Edition can lose a custom hover label after an in-place blip mutation.
    /// </remarks>
    public void SetTaxiSafeIcons(bool enabled)
    {
        if (_taxiSafeIcons == enabled)
        {
            return;
        }

        _taxiSafeIcons = enabled;

        foreach (CollectibleKey key in new List<CollectibleKey>(_markers.Keys))
        {
            if (!_markers.TryGetValue(key, out Marker? marker))
            {
                continue;
            }

            if (Classify(marker) != Ownership.Ours)
            {
                // Follow the same recovery rule as Update: never write through a recycled
                // handle. Forget it and create a fresh marker in the requested icon mode.
                _markers.Remove(key);
                WarnAboutStaleHandle(
                    $"the blip handle for {key} changed while switching taxi icons; recreating it");
                Add(key, marker.State);
                continue;
            }

            try
            {
                ApplyIcon(marker.Blip, key.Category, enabled);
                ApplyScale(marker.Blip, marker.State.Style.Scale, enabled);
                CompleteEditionBlipInterop.SetName(marker.Blip, marker.State.Name);
            }
            catch (Exception ex)
            {
                log.Error($"switching the taxi icon for {key} failed: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// Reports a blip that could not be created, loudly the first time and quietly after.
    /// </summary>
    /// <remarks>
    /// Promoted from Debug because this is not a cosmetic miss: the marker is silently absent
    /// from the map, and the tracker still records it as drawn, so nothing else in the mod can
    /// notice. Only the first is a warning — creation is retried whenever the marker's state
    /// next changes, so a persistent fault would otherwise flood the log.
    /// </remarks>
    private void WarnAboutCreation(string message)
    {
        if (_warnedAboutCreation)
        {
            log.Debug(message);
            return;
        }

        _warnedAboutCreation = true;
        log.Warn($"{message} (further occurrences are logged only with DebugLogging on)");
    }

    private static void ApplyStyle(Blip blip, MarkerStyle style, bool taxiSafe)
    {
        blip.Color = (BlipColor)(int)style.Colour;
        ApplyScale(blip, style.Scale, taxiSafe);
        CompleteEditionBlipInterop.SetDisplay(blip, style.Display);
    }

    private static void ApplyScale(Blip blip, float configuredScale, bool taxiSafe)
    {
        blip.Scale = taxiSafe
            ? configuredScale * TaxiSafeScaleMultiplier
            : configuredScale;
    }

    /// <summary>Pushes alpha only when it actually has to change.</summary>
    /// <remarks>
    /// Do not use Blip.Transparency: its float signature loses the native's integer alpha
    /// semantics on the Complete Edition compatibility path. Comparing against the alpha last
    /// applied keeps the original safety property — a marker that is never faded never receives
    /// an alpha call at all, so a fading failure can never hide what is left to collect — while
    /// still restoring full opacity in the other direction. That direction is reachable:
    /// collected state is derived from what the pickup pool still holds, so reloading an earlier
    /// save turns a faded collected marker back into an uncollected one.
    /// </remarks>
    private static void ApplyAlpha(Blip blip, byte applied, byte wanted)
    {
        if (applied == wanted)
        {
            return;
        }

        CompleteEditionBlipInterop.SetAlpha(blip, wanted);
    }

    /// <summary>Whether a tracked handle still refers to the marker this renderer created.</summary>
    /// <remarks>
    /// <para>
    /// A <see cref="Blip"/> is a handle into the game's blip pool, and the game owns that pool.
    /// Loading a save tears it down, and the slots are then handed out again to whatever needs
    /// one next — a ped, a vehicle, a mission marker. A handle held across that point still
    /// looks valid and still accepts writes, so without this check the renderer stamps its
    /// icon, colour and display onto somebody else's blip, and deletes blips it never owned.
    /// That damage outlives the session, because the game saves those objects.
    /// </para>
    /// <para>
    /// Ownership is proven by two things the recycled slot cannot both satisfy: our blips are
    /// always coordinate blips, and their position is one this renderer put there.
    /// </para>
    /// </remarks>
    /// <summary>How much claim this renderer still has on a tracked handle.</summary>
    private enum Ownership
    {
        /// <summary>The handle refers to the marker this renderer created.</summary>
        Ours,

        /// <summary>The handle refers to nothing. Writing is pointless; deleting is harmless.</summary>
        Gone,

        /// <summary>A live blip that is not ours. Must not be written to or deleted.</summary>
        Foreign,
    }

    /// <summary>Classifies a tracked handle before anything is done to it.</summary>
    /// <remarks>
    /// <para>
    /// A <see cref="Blip"/> is a handle into the game's blip pool, and the game owns that pool.
    /// Tearing down a session frees the slots, and they are handed out again to whatever needs
    /// one next. A handle held across that point still accepts writes, so without this the
    /// renderer would stamp its icon and colour onto somebody else's blip, or delete one it
    /// never owned — damage that outlives the session, because the game saves those objects.
    /// </para>
    /// <para>
    /// The three-way answer matters. Treating an unreadable handle as foreign is what a
    /// two-way check does, and it means shutdown quietly stops deleting anything — which
    /// manufactures exactly the leftover markers this was meant to prevent. Deleting a handle
    /// that refers to nothing cannot hurt: only a live blip in the wrong place is untouchable.
    /// </para>
    /// <para>
    /// Note what is deliberately NOT checked: <c>BlipType</c>. Complete Edition reports our own
    /// coordinate blips as <see cref="BlipType.Ped"/>, with a colour and display never written
    /// here — the same wrong-offset reads that make Transparency and BlipDisplay unusable.
    /// Position is the one field that survives the trip, so position is what ownership rests on.
    /// </para>
    /// </remarks>
    private static Ownership Classify(Marker marker)
    {
        try
        {
            if (!marker.Blip.Exists())
            {
                return Ownership.Gone;
            }

            Vec3 actual = marker.Blip.Position.ToVec3();
            return actual.DistanceSquaredTo(marker.State.Position) <= OwnershipEpsilonSquared
                ? Ownership.Ours
                : Ownership.Foreign;
        }
        catch
        {
            return Ownership.Gone;
        }
    }

    private void WarnAboutStaleHandle(string message)
    {
        if (_warnedAboutStaleHandle)
        {
            log.Debug(message);
            return;
        }

        _warnedAboutStaleHandle = true;
        log.Warn($"{message} (further occurrences are logged only with DebugLogging on)");
    }

    /// <summary>
    /// Whether a sprite name is one this renderer currently hands out or used historically.
    /// Reporting only.
    /// </summary>
    /// <remarks>
    /// Note what this cannot tell you: GTA IV may use these sprites for its own objectives, so
    /// a match here means "looks like ours", never "is ours".
    /// Ownership is only ever established by <see cref="Classify"/>, against a tracked handle.
    /// </remarks>
    private static bool IsMarkerSprite(string icon) =>
        icon is nameof(BlipIcon.Misc_Destination1)
            or nameof(BlipIcon.Misc_Destination2)
            or nameof(BlipIcon.Misc_Objective4)
            or nameof(BlipIcon.Misc_Objective5);

    private static void ApplyIcon(Blip blip, CollectibleCategory category, bool taxiSafe)
    {
        // ADD_BLIP_FOR_COORD leaves sprite 0 selected. On Complete Edition that sprite has
        // no visible pause-map artwork, so the blip exists (and can even be hovered) without
        // drawing an indicator. Destination variants 1 and 2 give the normal rounded artwork,
        // but GTA IV, TLAD and TBoGT enumerate those sprite IDs as taxi destinations. Objective
        // variants 4 and 5 are used only during taxi entry/rides because those scripts omit them.
        blip.Icon = (category, taxiSafe) switch
        {
            (CollectibleCategory.Bird, false) => BlipIcon.Misc_Destination1,
            (CollectibleCategory.StuntJump, false) => BlipIcon.Misc_Destination2,
            (CollectibleCategory.Bird, true) => BlipIcon.Misc_Objective4,
            (CollectibleCategory.StuntJump, true) => BlipIcon.Misc_Objective5,
            _ => taxiSafe ? BlipIcon.Misc_Objective4 : BlipIcon.Misc_Destination1,
        };
    }

    /// <summary>A live blip and the state last pushed to it.</summary>
    private sealed class Marker(Blip blip, MarkerState state)
    {
        public Blip Blip { get; } = blip;

        public MarkerState State { get; set; } = state;
    }
}
