using GtaCollectiblesMap.Core.Abstractions;

namespace GtaCollectiblesMap.Features;

/// <summary>
/// Rate-limits the collectible rescan.
/// </summary>
/// <remarks>
/// Scanning runs off the game's per-frame tick, but walking the pickup array and reconciling
/// markers is far too heavy to do at frame rate. Extracted from the host so the wraparound
/// arithmetic can actually be tested — <c>Environment.TickCount</c> wraps to negative roughly
/// every 25 days of uptime, and a naive comparison would stall refreshes when it does.
/// </remarks>
public sealed class RefreshThrottle(IClock clock, int floorMs = 250)
{
    private int _lastRefreshAt;

    private bool _hasRefreshed;

    /// <summary>
    /// Whether enough time has passed to refresh again. Records the refresh when it returns true.
    /// </summary>
    public bool ShouldRefresh(int requestedIntervalMs)
    {
        int interval = requestedIntervalMs < floorMs ? floorMs : requestedIntervalMs;
        int now = clock.TickCount;

        if (_hasRefreshed && unchecked(now - _lastRefreshAt) < interval)
        {
            return false;
        }

        _hasRefreshed = true;
        _lastRefreshAt = now;
        return true;
    }
}
