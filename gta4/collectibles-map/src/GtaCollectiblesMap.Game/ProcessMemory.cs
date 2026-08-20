using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using GtaCollectiblesMap.Core.Model;

namespace GtaCollectiblesMap.Game;

/// <summary>Guarded reads of the current 32-bit game process.</summary>
internal static class ProcessMemory
{
    private static readonly object DiscoveryLock = new();

    /// <summary>
    /// The current-process pseudo-handle.
    /// </summary>
    /// <remarks>
    /// A constant that never needs closing, unlike <c>Process.GetCurrentProcess().Handle</c>,
    /// which opens a real kernel handle through <c>OpenProcess</c> on every call and only
    /// releases it when the finalizer eventually runs. A single structural scan reads memory
    /// thousands of times, so that difference is the difference between no handles and
    /// thousands of them accumulating inside the game process.
    /// </remarks>
    private static readonly IntPtr CurrentProcess = GetCurrentProcess();

    private const uint MemCommit = 0x1000;
    private const uint PageNoAccess = 0x01;
    private const uint PageGuard = 0x100;

    public static IntPtr Add(IntPtr address, long offset) =>
        new(address.ToInt64() + offset);

    public static Vec3 ReadVec3(byte[] bytes, int offset) =>
        new(
            BitConverter.ToSingle(bytes, offset),
            BitConverter.ToSingle(bytes, offset + 4),
            BitConverter.ToSingle(bytes, offset + 8));

    public static bool IsWritable(uint protection)
    {
        uint basic = protection & 0xFF;
        return basic is 0x04 or 0x08 or 0x40 or 0x80;
    }

    public static bool TryRead(IntPtr address, byte[] destination)
    {
        if (address == IntPtr.Zero || destination.Length == 0)
        {
            return false;
        }

        bool success = ReadProcessMemory(
            CurrentProcess,
            address,
            destination,
            destination.Length,
            out IntPtr bytesRead);

        return success && bytesRead.ToInt64() == destination.Length;
    }

    public static IEnumerable<MemoryRegion> ReadableRegions()
    {
        IntPtr address = new(0x10000);
        long upperBound = Environment.Is64BitProcess ? long.MaxValue : 0x7FFEFFFFL;
        int infoSize = Marshal.SizeOf(typeof(MemoryBasicInformation));

        while (address.ToInt64() < upperBound
            && VirtualQuery(address, out MemoryBasicInformation info, (UIntPtr)(uint)infoSize) != UIntPtr.Zero)
        {
            long regionSize = unchecked((long)info.RegionSize.ToUInt64());
            if (regionSize <= 0)
            {
                yield break;
            }

            if (info.State == MemCommit
                && (info.Protect & (PageNoAccess | PageGuard)) == 0)
            {
                yield return new MemoryRegion(info.BaseAddress, regionSize, info.Protect);
            }

            long next = info.BaseAddress.ToInt64() + regionSize;
            if (next <= address.ToInt64() || next > upperBound)
            {
                yield break;
            }

            address = new IntPtr(next);
        }
    }

    /// <summary>
    /// Runs at most one expensive structural search at once and yields CPU priority to the game.
    /// The caller is responsible for putting this on a background task.
    /// </summary>
    public static T RunDiscovery<T>(Func<T> discover)
    {
        lock (DiscoveryLock)
        {
            Thread thread = Thread.CurrentThread;
            ThreadPriority originalPriority = thread.Priority;
            try
            {
                thread.Priority = ThreadPriority.BelowNormal;
                return discover();
            }
            finally
            {
                thread.Priority = originalPriority;
            }
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryBasicInformation
    {
        public IntPtr BaseAddress;
        public IntPtr AllocationBase;
        public uint AllocationProtect;
        public UIntPtr RegionSize;
        public uint State;
        public uint Protect;
        public uint Type;
    }

    internal readonly record struct MemoryRegion(IntPtr BaseAddress, long Size, uint Protection);

    [DllImport("kernel32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool ReadProcessMemory(
        IntPtr process,
        IntPtr baseAddress,
        [Out] byte[] buffer,
        int size,
        out IntPtr bytesRead);

    [DllImport("kernel32.dll")]
    private static extern IntPtr GetCurrentProcess();

    [DllImport("kernel32.dll")]
    private static extern UIntPtr VirtualQuery(
        IntPtr address,
        out MemoryBasicInformation buffer,
        UIntPtr length);
}
