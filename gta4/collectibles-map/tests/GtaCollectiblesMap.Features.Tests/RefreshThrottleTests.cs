using GtaCollectiblesMap.Core.Abstractions;
using Xunit;

namespace GtaCollectiblesMap.Features.Tests;

internal sealed class FakeClock : IClock
{
    public int TickCount { get; set; }

    public void Advance(int ms) => TickCount = unchecked(TickCount + ms);
}

public class RefreshThrottleTests
{
    [Fact]
    public void AllowsTheFirstRefreshImmediately()
    {
        RefreshThrottle throttle = new(new FakeClock());

        Assert.True(throttle.ShouldRefresh(1500));
    }

    [Fact]
    public void BlocksUntilTheIntervalHasElapsed()
    {
        FakeClock clock = new();
        RefreshThrottle throttle = new(clock);

        Assert.True(throttle.ShouldRefresh(1500));

        clock.Advance(1499);
        Assert.False(throttle.ShouldRefresh(1500));

        clock.Advance(1);
        Assert.True(throttle.ShouldRefresh(1500));
    }

    /// <summary>
    /// Refreshing is heavy — it walks the pickup array and reconciles markers — so an absurdly
    /// small configured interval must not drag it back to frame rate.
    /// </summary>
    [Fact]
    public void EnforcesAFloorOnTheConfiguredInterval()
    {
        FakeClock clock = new();
        RefreshThrottle throttle = new(clock, floorMs: 250);

        Assert.True(throttle.ShouldRefresh(0));

        clock.Advance(100);
        Assert.False(throttle.ShouldRefresh(0));

        clock.Advance(150);
        Assert.True(throttle.ShouldRefresh(0));
    }

    /// <summary>
    /// TickCount wraps to negative roughly every 25 days of uptime. Comparing with a plain
    /// subtraction would make the throttle stop allowing refreshes across the wrap.
    /// </summary>
    [Fact]
    public void SurvivesTickCountWraparound()
    {
        FakeClock clock = new() { TickCount = int.MaxValue - 100 };
        RefreshThrottle throttle = new(clock);

        Assert.True(throttle.ShouldRefresh(1500));

        clock.Advance(1600); // wraps into negative
        Assert.True(clock.TickCount < 0);

        Assert.True(throttle.ShouldRefresh(1500));
    }
}
