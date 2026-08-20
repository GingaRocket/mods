using System.Collections.Generic;
using GtaCollectiblesMap.Core.Abstractions;
using GtaCollectiblesMap.Core.Config;
using Xunit;

namespace GtaCollectiblesMap.Core.Tests;

internal sealed class FakeClock : IClock
{
    public int TickCount { get; set; }

    public void Advance(int ms) => TickCount = unchecked(TickCount + ms);
}

internal sealed class RecordingSettingsStore : ISettingsStore
{
    public List<ModSettings> Writes { get; } = [];

    public ModSettings Load() => new();

    public void Save(ModSettings settings) => Writes.Add(settings);
}

/// <summary>
/// The panel redraws every frame and a slider reports a change on each frame it is dragged, so
/// writing on every notification meant rewriting the whole INI dozens of times per second.
/// </summary>
public class DebouncedSettingsWriterTests
{
    private const int Quiet = 750;

    private static (DebouncedSettingsWriter Writer, FakeClock Clock, RecordingSettingsStore Store) Build()
    {
        FakeClock clock = new();
        RecordingSettingsStore store = new();
        return (new DebouncedSettingsWriter(store, clock, Quiet), clock, store);
    }

    [Fact]
    public void WritesNothingWhenNothingChanged()
    {
        (DebouncedSettingsWriter writer, FakeClock clock, RecordingSettingsStore store) = Build();

        clock.Advance(Quiet * 10);
        Assert.False(writer.FlushIfDue(new ModSettings()));

        Assert.Empty(store.Writes);
    }

    [Fact]
    public void HoldsTheWriteUntilTheQuietPeriodElapses()
    {
        (DebouncedSettingsWriter writer, FakeClock clock, RecordingSettingsStore store) = Build();

        writer.MarkChanged();
        clock.Advance(Quiet - 1);

        Assert.False(writer.FlushIfDue(new ModSettings()));
        Assert.Empty(store.Writes);

        clock.Advance(1);

        Assert.True(writer.FlushIfDue(new ModSettings()));
        Assert.Single(store.Writes);
    }

    /// <summary>The behaviour that fixes the drag: many changes collapse into one write.</summary>
    [Fact]
    public void CoalescesADragIntoASingleWrite()
    {
        (DebouncedSettingsWriter writer, FakeClock clock, RecordingSettingsStore store) = Build();

        // 60 frames of dragging, one change per frame.
        for (int frame = 0; frame < 60; frame++)
        {
            writer.MarkChanged();
            clock.Advance(16);
            writer.FlushIfDue(new ModSettings());
        }

        Assert.Empty(store.Writes);

        clock.Advance(Quiet);
        writer.FlushIfDue(new ModSettings());

        Assert.Single(store.Writes);
    }

    [Fact]
    public void WritesOnlyOncePerBatchOfChanges()
    {
        (DebouncedSettingsWriter writer, FakeClock clock, RecordingSettingsStore store) = Build();

        writer.MarkChanged();
        clock.Advance(Quiet);
        writer.FlushIfDue(new ModSettings());
        writer.FlushIfDue(new ModSettings());
        writer.FlushIfDue(new ModSettings());

        Assert.Single(store.Writes);
    }

    /// <summary>Shutdown must not drop an edit still sitting inside the quiet period.</summary>
    [Fact]
    public void FlushIgnoresTheQuietPeriod()
    {
        (DebouncedSettingsWriter writer, FakeClock clock, RecordingSettingsStore store) = Build();

        writer.MarkChanged();
        clock.Advance(1);

        Assert.True(writer.Flush(new ModSettings()));
        Assert.Single(store.Writes);
        Assert.False(writer.HasPendingChanges);
    }

    [Fact]
    public void FlushDoesNothingWhenThereIsNoPendingChange()
    {
        (DebouncedSettingsWriter writer, _, RecordingSettingsStore store) = Build();

        Assert.False(writer.Flush(new ModSettings()));
        Assert.Empty(store.Writes);
    }

    /// <summary>
    /// TickCount wraps to negative roughly every 25 days of uptime. A naive comparison would
    /// stall writes indefinitely across the wrap.
    /// </summary>
    [Fact]
    public void SurvivesTickCountWraparound()
    {
        FakeClock clock = new() { TickCount = int.MaxValue - 100 };
        RecordingSettingsStore store = new();
        DebouncedSettingsWriter writer = new(store, clock, Quiet);

        writer.MarkChanged();
        clock.Advance(Quiet + 50); // wraps past int.MaxValue into negative

        Assert.True(clock.TickCount < 0);
        Assert.True(writer.FlushIfDue(new ModSettings()));
        Assert.Single(store.Writes);
    }
}
