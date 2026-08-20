using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GtaCollectiblesMap.Core.Abstractions;
using GtaCollectiblesMap.Core.Model;
using GTA;

namespace GtaCollectiblesMap.Game;

/// <summary>Finds uncollected birds in Complete Edition's live pickup array.</summary>
/// <remarks>
/// ScriptHookDotNet has no pickup enumeration API. Instead of relying on an absolute address,
/// this locates the static array inside GTAIV.exe by its CPickup layout, the episode's known bird
/// coordinates, and the game's own collected counter. Every check must pass before markers are
/// returned.
/// </remarks>
public sealed class PickupCollectibleSource(
    IReadOnlyList<Vec3> knownPositions,
    ILog log) : ICollectibleSource
{
    private const int PickupCount = 1500;
    private const int EntrySize = 0x50;
    private const int PositionOffset = 0x18;
    private const int TypeOffset = 0x44;
    private const int ArrayByteSize = PickupCount * EntrySize;
    private const int ScanChunkSize = 4 * 1024 * 1024;
    private const int ScanOverlap = ArrayByteSize;
    private const byte PigeonPickupType = 3;
    private const float MatchEpsilonSquared = 0.25f;
    private const uint FailedDiscoveryRetryMs = 30_000;

    /// <summary>
    /// How many consecutive failed re-reads it takes to give up on a validated address.
    /// </summary>
    /// <remarks>
    /// Rediscovery walks the whole address space, so one failure is far too little to throw a
    /// known-good address away. The array does not move under a running game; a momentary
    /// mismatch is much likelier than a relocation.
    /// </remarks>
    private const int CachedAddressRetryLimit = 3;

    private IntPtr _arrayAddress;
    private int _cachedAddressFailures;
    private Task<DiscoveryResult>? _discovery;
    private uint _lastFailedDiscoveryTick;
    private bool _hasFailedDiscovery;
    private string _lastDiscoveryError = "pickup discovery has not completed";
    private readonly Dictionary<int, List<int>> _knownByXBucket = BuildKnownBuckets(knownPositions);

    public CollectibleCategory Category => CollectibleCategory.Bird;

    public ReadResult Read()
    {
        try
        {
            int collected = GTA.Game.GetIntegerStatistic(IntegerStatistic.PIGEONS_EXTERMINATED);
            int expectedRemaining = knownPositions.Count - collected;
            if (expectedRemaining < 0 || expectedRemaining > knownPositions.Count)
            {
                return ReadResult.Fail($"invalid bird counter ({collected} of {knownPositions.Count})");
            }

            if (expectedRemaining == 0)
            {
                return ReadResult.Ok(Array.Empty<SourceItem>());
            }

            if (_arrayAddress != IntPtr.Zero)
            {
                if (TryReadValidatedArray(_arrayAddress, expectedRemaining, out List<SourceItem>? cached))
                {
                    _cachedAddressFailures = 0;
                    return ReadResult.Ok(cached);
                }

                if (++_cachedAddressFailures < CachedAddressRetryLimit)
                {
                    return ReadResult.Fail(
                        $"the known pickup array did not validate ({_cachedAddressFailures} of "
                        + $"{CachedAddressRetryLimit} attempts)");
                }

                _arrayAddress = IntPtr.Zero;
                _cachedAddressFailures = 0;
            }

            if (_discovery is not null)
            {
                if (!_discovery.IsCompleted)
                {
                    return ReadResult.Fail("pickup discovery is running in the background");
                }

                Task<DiscoveryResult> completedDiscovery = _discovery;
                _discovery = null;

                DiscoveryResult result;
                try
                {
                    result = completedDiscovery.GetAwaiter().GetResult();
                }
                catch (Exception ex)
                {
                    // Caught here rather than left to the outer handler, which reports the
                    // failure but does not arm the backoff - so a throwing scan would start
                    // another whole-address-space search on the next refresh, and every one
                    // after that.
                    RecordFailedDiscovery($"pickup discovery threw: {ex.Message}");
                    return ReadResult.Fail(_lastDiscoveryError);
                }

                if (result.ExpectedRemaining == expectedRemaining && result.Address != IntPtr.Zero)
                {
                    _arrayAddress = result.Address;
                    _cachedAddressFailures = 0;
                    _hasFailedDiscovery = false;
                    log.Info($"validated pickup array at 0x{result.Address.ToInt64():X8}");
                    return ReadResult.Ok(result.Items);
                }

                RecordFailedDiscovery(result.ExpectedRemaining == expectedRemaining
                    ? $"could not validate the pickup array ({expectedRemaining} bird(s) expected); {result.Diagnostics}"
                    : "bird count changed while pickup discovery was running");
            }

            uint now = unchecked((uint)Environment.TickCount);
            if (_hasFailedDiscovery
                && unchecked(now - _lastFailedDiscoveryTick) < FailedDiscoveryRetryMs)
            {
                return ReadResult.Fail(_lastDiscoveryError);
            }

            int expectedForDiscovery = expectedRemaining;
            _discovery = Task.Run(() => ProcessMemory.RunDiscovery(() =>
            {
                ScanDiagnostics diagnostics = new();
                bool success = TryLocateArray(
                    expectedForDiscovery,
                    out IntPtr address,
                    out List<SourceItem> items,
                    diagnostics);
                return new DiscoveryResult(
                    expectedForDiscovery,
                    success ? address : IntPtr.Zero,
                    items,
                    diagnostics.ToString());
            }));

            return ReadResult.Fail("pickup discovery started in the background");
        }
        catch (Exception ex)
        {
            return ReadResult.Fail($"pickup scan failed: {ex.Message}");
        }
    }

    private void RecordFailedDiscovery(string message)
    {
        _hasFailedDiscovery = true;
        _lastFailedDiscoveryTick = unchecked((uint)Environment.TickCount);
        _lastDiscoveryError = message;
        log.Warn($"{message}; retrying in {FailedDiscoveryRetryMs / 1000} seconds");
    }

    private bool TryLocateArray(
        int expectedRemaining,
        out IntPtr address,
        out List<SourceItem> items,
        ScanDiagnostics diagnostics)
    {
        address = IntPtr.Zero;
        items = [];

        foreach (ProcessMemory.MemoryRegion region in ProcessMemory.ReadableRegions())
        {
            if (!ProcessMemory.IsWritable(region.Protection) || region.Size < ArrayByteSize)
            {
                continue;
            }

            long consumed = 0;
            while (consumed < region.Size)
            {
                int length = (int)Math.Min(ScanChunkSize, region.Size - consumed);
                byte[] image = new byte[length];
                IntPtr imageAddress = ProcessMemory.Add(region.BaseAddress, consumed);
                if (ProcessMemory.TryRead(imageAddress, image)
                    && TryLocateInImage(
                        imageAddress,
                        image,
                        expectedRemaining,
                        out address,
                        out items,
                        diagnostics))
                {
                    return true;
                }

                if (length == region.Size - consumed)
                {
                    break;
                }

                consumed += length - ScanOverlap;
            }
        }

        return false;
    }

    private bool TryLocateInImage(
        IntPtr imageAddress,
        byte[] image,
        int expectedRemaining,
        out IntPtr address,
        out List<SourceItem> items,
        ScanDiagnostics diagnostics)
    {
        int lastRecord = image.Length - EntrySize;
        for (int record = 0; record <= lastRecord; record += 4)
        {
            Vec3 position = ProcessMemory.ReadVec3(image, record + PositionOffset);
            if (!TryMatchKnown(position, null, out _))
            {
                continue;
            }

            diagnostics.RecordRawCoordinate(
                ProcessMemory.Add(imageAddress, record + PositionOffset),
                image[record + TypeOffset]);

            if (image[record + TypeOffset] != PigeonPickupType)
            {
                continue;
            }

            diagnostics.TypedCoordinateHits++;
            for (int slot = 0; slot < PickupCount; slot++)
            {
                int start = record - (slot * EntrySize);
                if (start < 0 || start + (PickupCount * EntrySize) > image.Length)
                {
                    continue;
                }

                bool valid = TryValidateBytes(image, start, expectedRemaining, out items);
                diagnostics.BestPartialWindow = Math.Max(diagnostics.BestPartialWindow, items.Count);
                if (valid)
                {
                    address = ProcessMemory.Add(imageAddress, start);
                    return true;
                }
            }
        }

        address = IntPtr.Zero;
        items = [];
        return false;
    }

    private bool TryReadValidatedArray(
        IntPtr address,
        int expectedRemaining,
        out List<SourceItem> items)
    {
        byte[] bytes = new byte[PickupCount * EntrySize];
        if (!ProcessMemory.TryRead(address, bytes))
        {
            items = [];
            return false;
        }

        return TryValidateBytes(bytes, 0, expectedRemaining, out items);
    }

    private bool TryValidateBytes(
        byte[] bytes,
        int start,
        int expectedRemaining,
        out List<SourceItem> items)
    {
        items = [];
        HashSet<int> matchedKnown = [];

        for (int slot = 0; slot < PickupCount; slot++)
        {
            int record = start + (slot * EntrySize);
            if (bytes[record + TypeOffset] != PigeonPickupType)
            {
                continue;
            }

            Vec3 position = ProcessMemory.ReadVec3(bytes, record + PositionOffset);
            if (!TryMatchKnown(position, matchedKnown, out _))
            {
                // Type 3 is used by a few mission-created pickups too. Only coordinates from
                // the canonical bird table belong to this source; unrelated type-3 entries do
                // not invalidate an otherwise complete pickup window.
                continue;
            }

            items.Add(new SourceItem(position, IsCollected: false, Label: null, SourceIndex: slot));
            if (items.Count > expectedRemaining)
            {
                return false;
            }
        }

        return items.Count == expectedRemaining;
    }

    private bool TryMatchKnown(Vec3 candidate, HashSet<int>? alreadyMatched, out int matchedIndex)
    {
        matchedIndex = -1;
        if (float.IsNaN(candidate.X) || float.IsInfinity(candidate.X)
            || candidate.X < int.MinValue / 2f || candidate.X > int.MaxValue / 2f)
        {
            return false;
        }

        int bucket = (int)Math.Floor(candidate.X * 2f);
        if (!_knownByXBucket.TryGetValue(bucket, out List<int>? possible))
        {
            return false;
        }

        foreach (int i in possible)
        {
            if (alreadyMatched?.Contains(i) == true)
            {
                continue;
            }

            Vec3 known = knownPositions[i];
            float dx = candidate.X - known.X;
            float dy = candidate.Y - known.Y;
            float dz = candidate.Z - known.Z;
            if ((dx * dx) + (dy * dy) + (dz * dz) <= MatchEpsilonSquared)
            {
                alreadyMatched?.Add(i);
                matchedIndex = i;
                return true;
            }
        }

        return false;
    }

    private static Dictionary<int, List<int>> BuildKnownBuckets(IReadOnlyList<Vec3> positions)
    {
        Dictionary<int, List<int>> buckets = [];
        for (int i = 0; i < positions.Count; i++)
        {
            int centre = (int)Math.Floor(positions[i].X * 2f);
            for (int bucket = centre - 1; bucket <= centre + 1; bucket++)
            {
                if (!buckets.TryGetValue(bucket, out List<int>? indices))
                {
                    indices = [];
                    buckets[bucket] = indices;
                }

                indices.Add(i);
            }
        }

        return buckets;
    }

    private readonly record struct DiscoveryResult(
        int ExpectedRemaining,
        IntPtr Address,
        List<SourceItem> Items,
        string Diagnostics);

    private sealed class ScanDiagnostics
    {
        private readonly List<string> _samples = [];

        public int RawCoordinateHits { get; private set; }

        public int TypedCoordinateHits { get; set; }

        public int BestPartialWindow { get; set; }

        public void RecordRawCoordinate(IntPtr address, byte assumedType)
        {
            RawCoordinateHits++;
            if (_samples.Count < 6)
            {
                // The address printed is the coordinate, so the type offset is quoted relative
                // to it rather than to the record base.
                _samples.Add(
                    $"0x{address.ToInt64():X8}/type@+{TypeOffset - PositionOffset:X}={assumedType}");
            }
        }

        public override string ToString() =>
            $"raw coordinate hits={RawCoordinateHits}, type-3 hits={TypedCoordinateHits}, "
            + $"best partial window={BestPartialWindow}, samples=[{string.Join(", ", _samples)}]";
    }
}
