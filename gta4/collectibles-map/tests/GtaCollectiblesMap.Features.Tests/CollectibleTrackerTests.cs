using System.Collections.Generic;
using GtaCollectiblesMap.Core.Abstractions;
using GtaCollectiblesMap.Core.Config;
using GtaCollectiblesMap.Core.Model;
using Xunit;

namespace GtaCollectiblesMap.Features.Tests;

public class CollectibleTrackerTests
{
    private static KnownCollectibleTable Table(params (float X, string Label)[] entries)
    {
        List<KnownCollectible> list = [];
        foreach ((float x, string label) in entries)
        {
            list.Add(new KnownCollectible(new Vec3(x, 0f, 0f), label));
        }

        return new KnownCollectibleTable(list);
    }

    private static readonly KnownCollectibleTable ThreeBirds =
        Table((0f, "Bird 1"), (100f, "Bird 2"), (200f, "Bird 3"));

    private static CollectibleTracker Build(
        FakeSource source,
        FakeRenderer renderer,
        KnownCollectibleTable? table,
        FakeLog? log = null) =>
        new("Birds", "Bird", source, renderer, table, _ => true, log ?? new FakeLog());

    private static SourceItem Present(float x, int index) =>
        new(new Vec3(x, 0f, 0f), IsCollected: false, Label: null, SourceIndex: index);

    /// <summary>
    /// The central inference for pool-backed collectibles: what the pool still holds is what
    /// the player has not collected, and anything missing from it has been collected.
    /// </summary>
    [Fact]
    public void TreatsAbsenceFromTheSourceAsCollected()
    {
        FakeSource source = new(CollectibleCategory.Bird);
        source.Items.Add(Present(0f, 0));
        source.Items.Add(Present(200f, 1));
        FakeRenderer renderer = new();

        Build(source, renderer, ThreeBirds).Refresh(new FakeGameContext(), new ModSettings());

        // Bird 2 (x=100) is gone from the pool, so it is collected and — with ShowAll off —
        // is not drawn at all.
        Assert.Equal(2, renderer.ActiveCount);
        Assert.DoesNotContain(new CollectibleKey(CollectibleCategory.Bird, 1), renderer.Markers.Keys);
    }

    [Fact]
    public void ReportsRemainingAndTotalCounts()
    {
        FakeSource source = new(CollectibleCategory.Bird);
        source.Items.Add(Present(0f, 0));
        FakeRenderer renderer = new();

        CollectibleTracker tracker = Build(source, renderer, ThreeBirds);
        tracker.Refresh(new FakeGameContext(), new ModSettings());

        Assert.Equal(TrackerState.Ok, tracker.Status.State);
        Assert.Equal(1, tracker.Status.Remaining);
        Assert.Equal(3, tracker.Status.Total);
        Assert.Equal(2, tracker.Status.Collected);
    }

    [Fact]
    public void ShowAllDrawsCollectedItemsFadedAndSmaller()
    {
        FakeSource source = new(CollectibleCategory.Bird);
        source.Items.Add(Present(0f, 0));
        FakeRenderer renderer = new();
        ModSettings settings = new() { ShowAll = true };

        Build(source, renderer, ThreeBirds).Refresh(new FakeGameContext(), settings);

        Assert.Equal(3, renderer.ActiveCount);

        MarkerState collected = renderer.Markers[new CollectibleKey(CollectibleCategory.Bird, 1)];
        MarkerState remaining = renderer.Markers[new CollectibleKey(CollectibleCategory.Bird, 0)];

        Assert.True(collected.Style.Alpha < remaining.Style.Alpha);
        Assert.True(collected.Style.Scale < remaining.Style.Scale);

        // Faded, but still the category's own colour, so a done bird still reads as a bird.
        Assert.Equal(remaining.Style.Colour, collected.Style.Colour);
    }

    /// <summary>
    /// Collected state is derived from what the pool still holds, so it can go backwards:
    /// reloading an earlier save puts a bird back. The marker keeps its identity and has to be
    /// updated all the way back to full opacity, not left faded.
    /// </summary>
    [Fact]
    public void AnItemThatReturnsToThePoolGoesBackToFullOpacity()
    {
        FakeSource source = new(CollectibleCategory.Bird);
        source.Items.Add(Present(0f, 0));
        FakeRenderer renderer = new();
        CollectibleTracker tracker = Build(source, renderer, ThreeBirds);
        FakeGameContext context = new();
        ModSettings settings = new() { ShowAll = true };
        CollectibleKey key = new(CollectibleCategory.Bird, 1);

        tracker.Refresh(context, settings);
        Assert.True(renderer.Markers[key].Style.Alpha < byte.MaxValue);

        renderer.Operations.Clear();
        source.Items.Add(Present(100f, 1));
        tracker.Refresh(context, settings);

        Assert.Equal(byte.MaxValue, renderer.Markers[key].Style.Alpha);
        Assert.Contains($"update:{key}", renderer.Operations);
    }

    [Fact]
    public void CollectingOneItemRemovesExactlyOneMarker()
    {
        FakeSource source = new(CollectibleCategory.Bird);
        source.Items.Add(Present(0f, 0));
        source.Items.Add(Present(100f, 1));
        source.Items.Add(Present(200f, 2));
        FakeRenderer renderer = new();
        CollectibleTracker tracker = Build(source, renderer, ThreeBirds);
        FakeGameContext context = new();
        ModSettings settings = new();

        tracker.Refresh(context, settings);
        renderer.Operations.Clear();

        source.Items.RemoveAt(1);
        tracker.Refresh(context, settings);

        Assert.Equal(2, renderer.ActiveCount);
        Assert.Equal(["remove:Bird:1"], renderer.Operations);
    }

    /// <summary>
    /// A stale or mistyped table entry must never hide something the player still has to find.
    /// </summary>
    [Fact]
    public void StillShowsItemsTheTableDoesNotKnowAbout()
    {
        FakeSource source = new(CollectibleCategory.Bird);
        source.Items.Add(Present(9999f, 7));
        FakeRenderer renderer = new();
        FakeLog log = new();

        Build(source, renderer, ThreeBirds, log).Refresh(new FakeGameContext(), new ModSettings());

        Assert.Contains(renderer.Markers, m => m.Value.Position.X == 9999f);
        Assert.Single(log.Warnings);
    }

    [Fact]
    public void SourceFailureClearsMarkersAndReportsWhy()
    {
        FakeSource source = new(CollectibleCategory.Bird);
        source.Items.Add(Present(0f, 0));
        FakeRenderer renderer = new();
        CollectibleTracker tracker = Build(source, renderer, ThreeBirds);
        FakeGameContext context = new();
        ModSettings settings = new();

        tracker.Refresh(context, settings);
        source.Error = "pool address did not resolve";
        tracker.Refresh(context, settings);

        Assert.Equal(TrackerState.Failed, tracker.Status.State);
        Assert.Equal("pool address did not resolve", tracker.Status.Message);
        Assert.Equal(0, renderer.ActiveCount);
    }

    [Fact]
    public void DisabledFeatureClearsItsMarkersAndStopsDrawing()
    {
        FakeSource source = new(CollectibleCategory.Bird);
        source.Items.Add(Present(0f, 0));
        FakeRenderer renderer = new();
        ModSettings settings = new();
        bool enabled = true;
        CollectibleTracker tracker =
            new("Birds", "Bird", source, renderer, ThreeBirds, _ => enabled, new FakeLog());

        tracker.Refresh(new FakeGameContext(), settings);
        enabled = false;
        tracker.Refresh(new FakeGameContext(), settings);

        Assert.Equal(TrackerState.Disabled, tracker.Status.State);
        Assert.Equal(0, renderer.ActiveCount);
    }

    /// <summary>
    /// Stunt jumps take the table-free path: the game keeps every jump in memory with its own
    /// passed flag, so the source reports collected state directly.
    /// </summary>
    [Fact]
    public void WithoutATableTheSourceReportsCollectedStateItself()
    {
        FakeSource source = new(CollectibleCategory.StuntJump);
        source.Items.Add(new SourceItem(new Vec3(0f, 0f, 0f), IsCollected: true, null, 0));
        source.Items.Add(new SourceItem(new Vec3(50f, 0f, 0f), IsCollected: false, null, 1));
        FakeRenderer renderer = new();

        CollectibleTracker tracker =
            new("Stunt Jumps", "Stunt Jump", source, renderer, table: null, _ => true, new FakeLog());
        tracker.Refresh(new FakeGameContext(), new ModSettings());

        Assert.Equal(1, tracker.Status.Remaining);
        Assert.Equal(2, tracker.Status.Total);
        Assert.Equal(1, renderer.ActiveCount);
    }

    [Fact]
    public void GeneratesAFallbackLabelWhenTheSourceSuppliesNone()
    {
        FakeSource source = new(CollectibleCategory.StuntJump);
        source.Items.Add(new SourceItem(new Vec3(0f, 0f, 0f), IsCollected: false, null, 11));
        FakeRenderer renderer = new();

        new CollectibleTracker("Stunt Jumps", "Stunt Jump", source, renderer, null, _ => true, new FakeLog())
            .Refresh(new FakeGameContext(), new ModSettings());

        MarkerState marker = renderer.Markers[new CollectibleKey(CollectibleCategory.StuntJump, 11)];
        Assert.Equal("Stunt Jump 12", marker.Label);
        Assert.Equal("Stunt Jump", marker.Name);
    }
}
