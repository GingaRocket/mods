using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GtaCollectiblesMap.Core.Abstractions;
using GtaCollectiblesMap.Core.Model;
using GTA;

namespace GtaCollectiblesMap.Game;

/// <summary>Reads the 50 stunt jumps and their passed flags from Complete Edition.</summary>
/// <remarks>
/// ScriptHookDotNet exposes the completed counter but not the pool. The locator first tries
/// the legacy contiguous layout, then finds the CPool header that owns a canonical jump.
/// Complete Edition can place live entries in sparse slots, so pool flags are authoritative.
/// Every result is cross-checked against canonical launch-box data and the game statistic.
/// </remarks>
public sealed class StuntJumpCollectibleSource(ILog log) : ICollectibleSource
{
    private const int JumpCount = 50;
    private const int LegacyEntrySize = 0x60;
    private const int MinimumEntrySize = 0x58;
    private const int MaximumEntrySize = 0x800;
    private const int StartBoxMinOffset = 0x00;
    private const int StartBoxMaxOffset = 0x10;
    private const int PassedOffset = 0x54;
    private const int ScanChunkSize = 1024 * 1024;
    private const int ScanOverlap = LegacyEntrySize * 3;
    private const int PoolHeaderSize = 0x1C;
    private const int MinimumPoolCapacity = JumpCount;
    private const int MaximumPoolCapacity = 512;
    private const float SignatureEpsilon = 0.02f;
    private const uint FailedDiscoveryRetryMs = 30_000;

    /// <summary>
    /// How many consecutive failed re-reads it takes to give up on a validated layout.
    /// </summary>
    /// <remarks>
    /// Rediscovery walks the whole address space twice over, so one failure is far too little
    /// to throw a known-good layout away. A momentary mismatch is much likelier than the pool
    /// actually moving.
    /// </remarks>
    private const int CachedLayoutRetryLimit = 3;

    /// <summary>
    /// How many consecutive refreshes the passed flags may disagree with the game's counter
    /// before the read is treated as a real failure.
    /// </summary>
    private const int CounterMismatchTolerance = 3;

    private static readonly JumpSignature[] FirstJumps =
    [
        new(new Vec3(1273.05f, 891.17f, 30.2f), new Vec3(1275.48f, 897.8f, 39.63f)),
        new(new Vec3(-222.27f, 132.04f, 14.72f), new Vec3(-219.09f, 135.45f, 22.54f)),
        new(new Vec3(699.3619f, 1366.308f, 25.2288f), new Vec3(712.58f, 1362.417f, 36.9953f)),
    ];

    private StorageLayout _layout;
    private int _cachedLayoutFailures;
    private int _counterMismatches;
    private Task<DiscoveryResult>? _discovery;
    private uint _lastFailedDiscoveryTick;
    private bool _hasFailedDiscovery;
    private string _lastDiscoveryError = "stunt jump discovery has not completed";

    public CollectibleCategory Category => CollectibleCategory.StuntJump;

    public ReadResult Read()
    {
        try
        {
            if (_layout.ObjectsAddress != IntPtr.Zero)
            {
                if (TryReadStorage(_layout, out List<SourceItem> jumps))
                {
                    _cachedLayoutFailures = 0;
                    return ValidateCounters(jumps);
                }

                if (++_cachedLayoutFailures < CachedLayoutRetryLimit)
                {
                    return ReadResult.Fail(
                        $"the known stunt jump storage did not read back ({_cachedLayoutFailures} "
                        + $"of {CachedLayoutRetryLimit} attempts)");
                }

                _layout = default;
                _cachedLayoutFailures = 0;
            }

            if (_discovery is not null)
            {
                if (!_discovery.IsCompleted)
                {
                    return ReadResult.Fail("stunt jump discovery is running in the background");
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
                    RecordFailedDiscovery($"stunt jump discovery threw: {ex.Message}");
                    return ReadResult.Fail(_lastDiscoveryError);
                }

                if (result.Layout.ObjectsAddress != IntPtr.Zero)
                {
                    _layout = result.Layout;
                    _cachedLayoutFailures = 0;
                    _hasFailedDiscovery = false;
                    log.Info($"validated stunt jump {result.Layout.Describe()}");
                    return ValidateCounters(result.Items);
                }

                RecordFailedDiscovery(
                    "could not validate the Complete Edition stunt jump array; " + result.Diagnostics);
            }

            uint now = unchecked((uint)Environment.TickCount);
            if (_hasFailedDiscovery
                && unchecked(now - _lastFailedDiscoveryTick) < FailedDiscoveryRetryMs)
            {
                return ReadResult.Fail(_lastDiscoveryError);
            }

            _discovery = Task.Run(() => ProcessMemory.RunDiscovery(() =>
            {
                ScanDiagnostics diagnostics = new();
                bool success = TryLocateStorage(
                    out StorageLayout layout,
                    out List<SourceItem> items,
                    diagnostics);
                return new DiscoveryResult(
                    success ? layout : default,
                    items,
                    diagnostics.ToString());
            }));

            return ReadResult.Fail("stunt jump discovery started in the background");
        }
        catch (Exception ex)
        {
            return ReadResult.Fail($"stunt jump read failed: {ex.Message}");
        }
    }

    private void RecordFailedDiscovery(string message)
    {
        _hasFailedDiscovery = true;
        _lastFailedDiscoveryTick = unchecked((uint)Environment.TickCount);
        _lastDiscoveryError = message;
        log.Warn($"{message}; retrying in {FailedDiscoveryRetryMs / 1000} seconds");
    }

    /// <summary>
    /// Cross-checks the decoded passed flags against the game's own completed counter.
    /// </summary>
    /// <remarks>
    /// A brief disagreement is expected rather than exceptional: the pool flag and the
    /// statistic are not written in the same frame, so landing a jump shows up in one before
    /// the other. Failing on that transient is expensive out of all proportion — a failed read
    /// clears the tracker, so every completed jump would tear down all fifty markers and
    /// rebuild them a moment later. The pool flags are the authoritative source here, so a
    /// short run of mismatches is reported as success and only a persistent one fails.
    /// </remarks>
    private ReadResult ValidateCounters(List<SourceItem> jumps)
    {
        int decodedCompleted = 0;
        foreach (SourceItem jump in jumps)
        {
            if (jump.IsCollected)
            {
                decodedCompleted++;
            }
        }

        int reportedCompleted = GTA.Game.GetIntegerStatistic(IntegerStatistic.STUNT_JUMPS_COMPLETED);
        if (decodedCompleted == reportedCompleted)
        {
            _counterMismatches = 0;
            return ReadResult.Ok(jumps);
        }

        if (++_counterMismatches < CounterMismatchTolerance)
        {
            return ReadResult.Ok(jumps);
        }

        return ReadResult.Fail(
            $"stunt jump validation mismatch: decoded {decodedCompleted}, game reports {reportedCompleted}");
    }

    private static bool TryLocateStorage(
        out StorageLayout layout,
        out List<SourceItem> jumps,
        ScanDiagnostics diagnostics)
    {
        byte[] firstX = BitConverter.GetBytes(FirstJumps[0].Min.X);
        bool triedPoolDiscovery = false;

        foreach (ProcessMemory.MemoryRegion region in ProcessMemory.ReadableRegions())
        {
            if (!ProcessMemory.IsWritable(region.Protection) || region.Size < JumpCount * MinimumEntrySize)
            {
                continue;
            }

            long consumed = 0;
            while (consumed < region.Size)
            {
                int length = (int)Math.Min(ScanChunkSize, region.Size - consumed);
                byte[] chunk = new byte[length];
                IntPtr chunkAddress = ProcessMemory.Add(region.BaseAddress, consumed);
                if (ProcessMemory.TryRead(chunkAddress, chunk))
                {
                    int offset = 0;
                    while (offset <= chunk.Length - firstX.Length)
                    {
                        offset = Array.IndexOf(chunk, firstX[0], offset);
                        if (offset < 0 || offset > chunk.Length - firstX.Length)
                        {
                            break;
                        }

                        if (chunk[offset + 1] != firstX[1]
                            || chunk[offset + 2] != firstX[2]
                            || chunk[offset + 3] != firstX[3])
                        {
                            offset++;
                            continue;
                        }

                        IntPtr jumpZero = ProcessMemory.Add(chunkAddress, offset);
                        diagnostics.RecordFirstX(jumpZero);
                        if (TryDiscoverFlatArray(jumpZero, out layout, out jumps))
                        {
                            return true;
                        }

                        // The repeated managed/static copies have the canonical minimum but
                        // not CStuntJump's padded box layout. Only the live record passes this.
                        if (!triedPoolDiscovery && MatchesJump(jumpZero, FirstJumps[0]))
                        {
                            triedPoolDiscovery = true;
                            diagnostics.RecordLiveJump(jumpZero);
                            if (TryLocateOwningPool(jumpZero, out layout, out jumps, diagnostics))
                            {
                                return true;
                            }
                        }

                        offset++;
                    }
                }

                if (length == region.Size - consumed)
                {
                    break;
                }

                consumed += length - ScanOverlap;
            }
        }

        layout = default;
        jumps = [];
        return false;
    }

    private static bool TryDiscoverFlatArray(
        IntPtr jumpZero,
        out StorageLayout layout,
        out List<SourceItem> jumps)
    {
        for (int candidateSize = MinimumEntrySize; candidateSize <= MaximumEntrySize; candidateSize += 4)
        {
            if (MatchesJump(ProcessMemory.Add(jumpZero, candidateSize), FirstJumps[1])
                && MatchesJump(ProcessMemory.Add(jumpZero, candidateSize * 2L), FirstJumps[2])
                && TryReadArray(jumpZero, candidateSize, out jumps))
            {
                layout = StorageLayout.Flat(jumpZero, candidateSize);
                return true;
            }

            if (MatchesJump(ProcessMemory.Add(jumpZero, -candidateSize), FirstJumps[1])
                && MatchesJump(ProcessMemory.Add(jumpZero, -(candidateSize * 2L)), FirstJumps[2]))
            {
                IntPtr reverseBase = ProcessMemory.Add(jumpZero, -((JumpCount - 1L) * candidateSize));
                if (TryReadArray(reverseBase, candidateSize, out jumps))
                {
                    layout = StorageLayout.Flat(reverseBase, candidateSize);
                    return true;
                }
            }
        }

        layout = default;
        jumps = [];
        return false;
    }

    private static bool TryLocateOwningPool(
        IntPtr knownJump,
        out StorageLayout layout,
        out List<SourceItem> jumps,
        ScanDiagnostics diagnostics)
    {
        uint knownJumpAddress = Address32(knownJump);

        foreach (ProcessMemory.MemoryRegion region in ProcessMemory.ReadableRegions())
        {
            if (!ProcessMemory.IsWritable(region.Protection) || region.Size < PoolHeaderSize)
            {
                continue;
            }

            long consumed = 0;
            while (consumed < region.Size)
            {
                int length = (int)Math.Min(ScanChunkSize, region.Size - consumed);
                byte[] chunk = new byte[length];
                IntPtr chunkAddress = ProcessMemory.Add(region.BaseAddress, consumed);
                if (ProcessMemory.TryRead(chunkAddress, chunk))
                {
                    for (int offset = 0; offset <= chunk.Length - PoolHeaderSize; offset += 4)
                    {
                        if (!TryParsePoolHeader(
                            chunk,
                            offset,
                            knownJumpAddress,
                            out StorageLayout candidate))
                        {
                            continue;
                        }

                        candidate = candidate with { HeaderAddress = ProcessMemory.Add(chunkAddress, offset) };
                        diagnostics.RecordPoolCandidate(candidate);
                        if (TryReadPool(candidate, knownJumpAddress, out jumps))
                        {
                            layout = candidate;
                            return true;
                        }
                    }
                }

                if (length == region.Size - consumed)
                {
                    break;
                }

                consumed += length - PoolHeaderSize;
            }
        }

        layout = default;
        jumps = [];
        return false;
    }

    private static bool TryParsePoolHeader(
        byte[] bytes,
        int offset,
        uint knownJumpAddress,
        out StorageLayout layout)
    {
        uint objectsAddress = BitConverter.ToUInt32(bytes, offset);
        uint flagsAddress = BitConverter.ToUInt32(bytes, offset + 4);
        int capacity = BitConverter.ToInt32(bytes, offset + 8);
        int entrySize = BitConverter.ToInt32(bytes, offset + 0x0C);
        int top = BitConverter.ToInt32(bytes, offset + 0x10);
        int used = BitConverter.ToInt32(bytes, offset + 0x14);
        byte allocated = bytes[offset + 0x18];

        if (objectsAddress < 0x10000
            || flagsAddress < 0x10000
            || capacity < MinimumPoolCapacity
            || capacity > MaximumPoolCapacity
            || used != JumpCount
            || top < -1
            || top >= capacity
            || allocated > 1)
        {
            layout = default;
            return false;
        }

        if (entrySize == sizeof(uint))
        {
            layout = new StorageLayout(
                StorageKind.PointerPool,
                AddressFrom32(objectsAddress),
                AddressFrom32(flagsAddress),
                capacity,
                entrySize,
                IntPtr.Zero);
            return true;
        }

        ulong span = (ulong)(uint)capacity * (uint)entrySize;
        ulong delta = knownJumpAddress >= objectsAddress
            ? knownJumpAddress - objectsAddress
            : ulong.MaxValue;
        if (entrySize < MinimumEntrySize
            || entrySize > MaximumEntrySize
            || (entrySize & 3) != 0
            || delta >= span
            || delta % (uint)entrySize != 0)
        {
            layout = default;
            return false;
        }

        layout = new StorageLayout(
            StorageKind.InlinePool,
            AddressFrom32(objectsAddress),
            AddressFrom32(flagsAddress),
            capacity,
            entrySize,
            IntPtr.Zero);
        return true;
    }

    private static bool MatchesJump(IntPtr address, JumpSignature signature)
    {
        byte[] bytes = new byte[0x20];
        return ProcessMemory.TryRead(address, bytes)
            && Near(ProcessMemory.ReadVec3(bytes, StartBoxMinOffset), signature.Min)
            && Near(ProcessMemory.ReadVec3(bytes, StartBoxMaxOffset), signature.Max);
    }

    private static bool TryReadStorage(StorageLayout layout, out List<SourceItem> jumps) =>
        layout.Kind == StorageKind.FlatArray
            ? TryReadArray(layout.ObjectsAddress, layout.EntrySize, out jumps)
            : TryReadPool(layout, knownJumpAddress: null, out jumps);

    private static bool TryReadPool(
        StorageLayout layout,
        uint? knownJumpAddress,
        out List<SourceItem> jumps)
    {
        jumps = [];
        if (layout.Capacity < MinimumPoolCapacity
            || layout.Capacity > MaximumPoolCapacity
            || layout.FlagsAddress == IntPtr.Zero)
        {
            return false;
        }

        byte[] flags = new byte[layout.Capacity];
        if (!ProcessMemory.TryRead(layout.FlagsAddress, flags))
        {
            return false;
        }

        List<DecodedJump> decoded = new(JumpCount);
        bool containsKnownAddress = knownJumpAddress is null;

        if (layout.Kind == StorageKind.InlinePool)
        {
            if (layout.EntrySize < MinimumEntrySize || layout.EntrySize > MaximumEntrySize)
            {
                return false;
            }

            byte[] objects = new byte[layout.Capacity * layout.EntrySize];
            if (!ProcessMemory.TryRead(layout.ObjectsAddress, objects))
            {
                return false;
            }

            for (int slot = 0; slot < layout.Capacity; slot++)
            {
                if (!IsPoolSlotValid(flags[slot]))
                {
                    continue;
                }

                int entryOffset = slot * layout.EntrySize;
                if (!TryDecodeJump(objects, entryOffset, slot, out DecodedJump jump))
                {
                    return false;
                }

                decoded.Add(jump);
                if (knownJumpAddress is uint expected
                    && Address32(ProcessMemory.Add(layout.ObjectsAddress, entryOffset)) == expected)
                {
                    containsKnownAddress = true;
                }
            }
        }
        else if (layout.Kind == StorageKind.PointerPool && layout.EntrySize == sizeof(uint))
        {
            byte[] pointers = new byte[layout.Capacity * sizeof(uint)];
            if (!ProcessMemory.TryRead(layout.ObjectsAddress, pointers))
            {
                return false;
            }

            for (int slot = 0; slot < layout.Capacity; slot++)
            {
                if (!IsPoolSlotValid(flags[slot]))
                {
                    continue;
                }

                uint pointer = BitConverter.ToUInt32(pointers, slot * sizeof(uint));
                if (pointer < 0x10000)
                {
                    return false;
                }

                byte[] record = new byte[PassedOffset + 1];
                if (!ProcessMemory.TryRead(AddressFrom32(pointer), record)
                    || !TryDecodeJump(record, 0, slot, out DecodedJump jump))
                {
                    return false;
                }

                decoded.Add(jump);
                if (knownJumpAddress == pointer)
                {
                    containsKnownAddress = true;
                }
            }
        }
        else
        {
            return false;
        }

        if (!containsKnownAddress
            || decoded.Count != JumpCount
            || !ContainsCanonicalJump(decoded))
        {
            return false;
        }

        jumps = new List<SourceItem>(JumpCount);
        foreach (DecodedJump jump in decoded)
        {
            jumps.Add(jump.Item);
        }

        return true;
    }

    private static bool TryDecodeJump(
        byte[] bytes,
        int offset,
        int sourceIndex,
        out DecodedJump jump)
    {
        byte passedByte = bytes[offset + PassedOffset];
        Vec3 min = ProcessMemory.ReadVec3(bytes, offset + StartBoxMinOffset);
        Vec3 max = ProcessMemory.ReadVec3(bytes, offset + StartBoxMaxOffset);
        if (passedByte > 1 || !IsFinite(min) || !IsFinite(max))
        {
            jump = default;
            return false;
        }

        Vec3 centre = new(
            (min.X + max.X) * 0.5f,
            (min.Y + max.Y) * 0.5f,
            (min.Z + max.Z) * 0.5f);
        jump = new DecodedJump(
            min,
            max,
            new SourceItem(
                centre,
                IsCollected: passedByte != 0,
                Label: null,
                SourceIndex: sourceIndex));
        return true;
    }

    private static bool ContainsCanonicalJump(List<DecodedJump> decoded)
    {
        JumpSignature signature = FirstJumps[0];
        foreach (DecodedJump jump in decoded)
        {
            if (Near(jump.Min, signature.Min) && Near(jump.Max, signature.Max))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPoolSlotValid(byte flag) => (flag & 0x80) == 0;

    private static bool TryReadArray(IntPtr arrayAddress, int entrySize, out List<SourceItem> jumps)
    {
        jumps = [];
        if (entrySize < MinimumEntrySize || entrySize > MaximumEntrySize)
        {
            return false;
        }

        byte[] bytes = new byte[JumpCount * entrySize];
        if (!ProcessMemory.TryRead(arrayAddress, bytes))
        {
            return false;
        }

        bool forwardOrder = MatchesFirstJumps(bytes, entrySize, reverseOrder: false);
        bool reverseOrder = MatchesFirstJumps(bytes, entrySize, reverseOrder: true);
        if (!forwardOrder && !reverseOrder)
        {
            return false;
        }

        for (int slot = 0; slot < JumpCount; slot++)
        {
            int entry = slot * entrySize;
            byte passedByte = bytes[entry + PassedOffset];
            if (passedByte > 1)
            {
                return false;
            }

            Vec3 min = ProcessMemory.ReadVec3(bytes, entry + StartBoxMinOffset);
            Vec3 max = ProcessMemory.ReadVec3(bytes, entry + StartBoxMaxOffset);
            if (!IsFinite(min) || !IsFinite(max))
            {
                return false;
            }

            Vec3 centre = new(
                (min.X + max.X) * 0.5f,
                (min.Y + max.Y) * 0.5f,
                (min.Z + max.Z) * 0.5f);

            jumps.Add(new SourceItem(
                centre,
                IsCollected: passedByte != 0,
                Label: null,
                SourceIndex: slot));
        }

        return true;
    }

    private static bool MatchesFirstJumps(byte[] bytes, int entrySize, bool reverseOrder)
    {
        for (int i = 0; i < FirstJumps.Length; i++)
        {
            int physicalSlot = reverseOrder ? JumpCount - 1 - i : i;
            int entry = physicalSlot * entrySize;
            if (!Near(ProcessMemory.ReadVec3(bytes, entry + StartBoxMinOffset), FirstJumps[i].Min)
                || !Near(ProcessMemory.ReadVec3(bytes, entry + StartBoxMaxOffset), FirstJumps[i].Max))
            {
                return false;
            }
        }

        return true;
    }

    private static bool Near(Vec3 left, Vec3 right) =>
        Math.Abs(left.X - right.X) <= SignatureEpsilon
        && Math.Abs(left.Y - right.Y) <= SignatureEpsilon
        && Math.Abs(left.Z - right.Z) <= SignatureEpsilon;

    private static bool IsFinite(Vec3 value) =>
        !float.IsNaN(value.X) && !float.IsInfinity(value.X)
        && !float.IsNaN(value.Y) && !float.IsInfinity(value.Y)
        && !float.IsNaN(value.Z) && !float.IsInfinity(value.Z)
        && Math.Abs(value.X) < 10000f
        && Math.Abs(value.Y) < 10000f
        && Math.Abs(value.Z) < 10000f;

    private static uint Address32(IntPtr address) => unchecked((uint)address.ToInt64());

    private static IntPtr AddressFrom32(uint address) => new(unchecked((int)address));

    private readonly record struct JumpSignature(Vec3 Min, Vec3 Max);

    private readonly record struct DecodedJump(Vec3 Min, Vec3 Max, SourceItem Item);

    private enum StorageKind
    {
        FlatArray,
        InlinePool,
        PointerPool,
    }

    private readonly record struct StorageLayout(
        StorageKind Kind,
        IntPtr ObjectsAddress,
        IntPtr FlagsAddress,
        int Capacity,
        int EntrySize,
        IntPtr HeaderAddress)
    {
        public static StorageLayout Flat(IntPtr address, int entrySize) =>
            new(StorageKind.FlatArray, address, IntPtr.Zero, JumpCount, entrySize, IntPtr.Zero);

        public string Describe() => Kind switch
        {
            StorageKind.FlatArray =>
                $"array at 0x{ObjectsAddress.ToInt64():X8} with entry size 0x{EntrySize:X}",
            StorageKind.InlinePool =>
                $"inline pool at 0x{HeaderAddress.ToInt64():X8} "
                + $"(objects 0x{ObjectsAddress.ToInt64():X8}, capacity {Capacity}, "
                + $"entry size 0x{EntrySize:X})",
            StorageKind.PointerPool =>
                $"pointer pool at 0x{HeaderAddress.ToInt64():X8} "
                + $"(objects 0x{ObjectsAddress.ToInt64():X8}, capacity {Capacity})",
            _ => "storage",
        };
    }

    private readonly record struct DiscoveryResult(
        StorageLayout Layout,
        List<SourceItem> Items,
        string Diagnostics);

    private sealed class ScanDiagnostics
    {
        private readonly List<string> _samples = [];
        private readonly List<string> _poolSamples = [];

        public int FirstXHits { get; private set; }

        public int FirstMinHits { get; private set; }

        public int LiveJumpHits { get; private set; }

        public int PoolCandidates { get; private set; }

        public void RecordFirstX(IntPtr address)
        {
            FirstXHits++;
            byte[] bytes = new byte[0x60];
            if (!ProcessMemory.TryRead(address, bytes))
            {
                return;
            }

            if (Near(ProcessMemory.ReadVec3(bytes, 0), FirstJumps[0].Min))
            {
                FirstMinHits++;
                if (_samples.Count < 6)
                {
                    _samples.Add(
                        $"0x{address.ToInt64():X8}: +0C={BitConverter.ToSingle(bytes, 0x0C):0.###}, "
                        + $"+10={BitConverter.ToSingle(bytes, 0x10):0.###}, "
                        + $"+14={BitConverter.ToSingle(bytes, 0x14):0.###}, "
                        + $"+18={BitConverter.ToSingle(bytes, 0x18):0.###}, "
                        + $"byte+54={bytes[0x54]}");
                }
            }
        }

        public void RecordLiveJump(IntPtr address)
        {
            LiveJumpHits++;
            if (_poolSamples.Count < 6)
            {
                _poolSamples.Add($"live jump 0x{address.ToInt64():X8}");
            }
        }

        public void RecordPoolCandidate(StorageLayout candidate)
        {
            PoolCandidates++;
            if (_poolSamples.Count < 6)
            {
                _poolSamples.Add(candidate.Describe());
            }
        }

        public override string ToString() =>
            $"first-X hits={FirstXHits}, full first-min hits={FirstMinHits}, "
            + $"live jump hits={LiveJumpHits}, pool candidates={PoolCandidates}, "
            + $"samples=[{string.Join(" | ", _samples)}], "
            + $"pool samples=[{string.Join(" | ", _poolSamples)}]";
    }
}
