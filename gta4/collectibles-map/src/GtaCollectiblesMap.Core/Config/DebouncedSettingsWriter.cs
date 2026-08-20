using GtaCollectiblesMap.Core.Abstractions;

namespace GtaCollectiblesMap.Core.Config;

/// <summary>
/// Collects rapid setting changes and writes them out once things settle.
/// </summary>
/// <remarks>
/// The settings panel redraws every frame, and a slider reports a change on each of those
/// frames while it is being dragged. Persisting on every notification meant rewriting the whole
/// INI dozens of times a second for one drag. This holds the change until the user stops
/// moving, then writes once.
/// </remarks>
public sealed class DebouncedSettingsWriter(ISettingsStore store, IClock clock, int quietPeriodMs = 750)
{
    private bool _pending;

    private int _lastChangeAt;

    /// <summary>Records that something changed, restarting the quiet period.</summary>
    public void MarkChanged()
    {
        _pending = true;
        _lastChangeAt = clock.TickCount;
    }

    /// <summary>
    /// Writes if the quiet period has elapsed since the last change. Safe to call every tick.
    /// </summary>
    /// <returns>True if a write happened.</returns>
    public bool FlushIfDue(ModSettings settings)
    {
        if (!_pending)
        {
            return false;
        }

        // Unchecked so the comparison still holds when TickCount wraps.
        if (unchecked(clock.TickCount - _lastChangeAt) < quietPeriodMs)
        {
            return false;
        }

        return Flush(settings);
    }

    /// <summary>
    /// Writes any pending change immediately, ignoring the quiet period. Call on shutdown so a
    /// change made in the last moments before exit is not lost.
    /// </summary>
    /// <returns>True if a write happened.</returns>
    public bool Flush(ModSettings settings)
    {
        if (!_pending)
        {
            return false;
        }

        _pending = false;
        store.Save(settings);
        return true;
    }

    public bool HasPendingChanges => _pending;
}
