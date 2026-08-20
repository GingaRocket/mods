using System.Collections.Generic;
using GtaCollectiblesMap.Core.Model;
using Xunit;

namespace GtaCollectiblesMap.Core.Tests;

public class MarkerDifferTests
{
    private static MarkerState State(float x, byte alpha = 255) =>
        new(
            new Vec3(x, 0f, 0f),
            "label",
            "name",
            new MarkerStyle(BlipColour.Teal, 1f, alpha, MarkerDisplay.MapOnly));

    private static CollectibleKey Key(int i) => new(CollectibleCategory.Bird, i);

    [Fact]
    public void AddsEverythingWhenNothingExistsYet()
    {
        Dictionary<CollectibleKey, MarkerState> desired = new() { [Key(1)] = State(1f), [Key(2)] = State(2f) };

        MarkerDiff diff = MarkerDiffer.Compute(new Dictionary<CollectibleKey, MarkerState>(), desired);

        Assert.Equal(2, diff.Added.Count);
        Assert.Empty(diff.Updated);
        Assert.Empty(diff.Removed);
    }

    [Fact]
    public void ProducesNoOperationsWhenNothingChanged()
    {
        Dictionary<CollectibleKey, MarkerState> set = new() { [Key(1)] = State(1f) };

        MarkerDiff diff = MarkerDiffer.Compute(set, new Dictionary<CollectibleKey, MarkerState>(set));

        Assert.True(diff.IsEmpty);
    }

    /// <summary>
    /// The behaviour that makes "shoot one pigeon, exactly one blip disappears" verifiable.
    /// </summary>
    [Fact]
    public void RemovingOneCollectibleTouchesOnlyThatMarker()
    {
        Dictionary<CollectibleKey, MarkerState> before = new()
        {
            [Key(1)] = State(1f),
            [Key(2)] = State(2f),
            [Key(3)] = State(3f),
        };
        Dictionary<CollectibleKey, MarkerState> after = new(before);
        after.Remove(Key(2));

        MarkerDiff diff = MarkerDiffer.Compute(before, after);

        Assert.Empty(diff.Added);
        Assert.Empty(diff.Updated);
        Assert.Equal(Key(2), Assert.Single(diff.Removed));
    }

    [Fact]
    public void ReportsStyleChangeAsUpdateNotAsRemoveAndAdd()
    {
        Dictionary<CollectibleKey, MarkerState> before = new() { [Key(1)] = State(1f) };
        Dictionary<CollectibleKey, MarkerState> after = new() { [Key(1)] = State(1f, alpha: 90) };

        MarkerDiff diff = MarkerDiffer.Compute(before, after);

        Assert.Empty(diff.Added);
        Assert.Empty(diff.Removed);
        Assert.Equal(Key(1), Assert.Single(diff.Updated).Key);
    }
}
