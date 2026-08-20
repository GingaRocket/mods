using System;
using System.Collections.Generic;
using GtaCollectiblesMap.Core.Config;
using GtaCollectiblesMap.Core.Model;
using Xunit;

namespace GtaCollectiblesMap.Features.Tests;

internal sealed class FakeTracker(string name) : ICollectibleTracker
{
    public string Name { get; } = name;

    public TrackerStatus Status { get; private set; } = TrackerStatus.Waiting;

    public int RefreshCount { get; private set; }

    public int ClearCount { get; private set; }

    public Exception? ThrowOnRefresh { get; set; }

    public void Refresh(Core.Abstractions.IGameContext context, ModSettings settings)
    {
        RefreshCount++;

        if (ThrowOnRefresh is not null)
        {
            throw ThrowOnRefresh;
        }

        Status = new TrackerStatus(TrackerState.Ok, null, 1, 2);
    }

    public void Clear() => ClearCount++;
}

internal sealed class FakeTrackerFactory : ICollectibleTrackerFactory
{
    public List<Episode> CreatedFor { get; } = [];

    public List<FakeTracker> Created { get; } = [];

    public Func<Episode, FakeTracker[]>? Builder { get; set; }

    public IReadOnlyList<ICollectibleTracker> Create(Episode episode)
    {
        CreatedFor.Add(episode);
        FakeTracker[] trackers = Builder?.Invoke(episode) ?? [new FakeTracker($"tracker-{episode}")];
        Created.AddRange(trackers);
        return trackers;
    }
}

public class TrackerCoordinatorTests
{
    private static (TrackerCoordinator Coordinator, FakeTrackerFactory Factory, FakeLog Log) Build()
    {
        FakeTrackerFactory factory = new();
        FakeLog log = new();
        return (new TrackerCoordinator(factory, log), factory, log);
    }

    [Fact]
    public void BuildsTrackersOnTheFirstRefresh()
    {
        (TrackerCoordinator coordinator, FakeTrackerFactory factory, _) = Build();

        coordinator.Refresh(new FakeGameContext { Episode = Episode.Iv }, new ModSettings());

        Assert.Equal([Episode.Iv], factory.CreatedFor);
        Assert.Equal(1, factory.Created[0].RefreshCount);
        Assert.Equal(Episode.Iv, coordinator.BuiltForEpisode);
    }

    [Fact]
    public void DoesNotRebuildWhileTheEpisodeIsUnchanged()
    {
        (TrackerCoordinator coordinator, FakeTrackerFactory factory, _) = Build();
        FakeGameContext context = new() { Episode = Episode.Iv };

        coordinator.Refresh(context, new ModSettings());
        coordinator.Refresh(context, new ModSettings());
        coordinator.Refresh(context, new ModSettings());

        Assert.Single(factory.CreatedFor);
        Assert.Equal(3, factory.Created[0].RefreshCount);
    }

    /// <summary>
    /// Marker identity is an index into an episode-specific coordinate table, so carrying
    /// trackers across an episode switch would show GTA IV pigeon positions during an episode.
    /// </summary>
    [Fact]
    public void RebuildsAndClearsTheOldTrackersWhenTheEpisodeChanges()
    {
        (TrackerCoordinator coordinator, FakeTrackerFactory factory, _) = Build();
        FakeGameContext context = new() { Episode = Episode.Iv };

        coordinator.Refresh(context, new ModSettings());
        FakeTracker original = factory.Created[0];

        context.Episode = Episode.Tbogt;
        coordinator.Refresh(context, new ModSettings());

        Assert.Equal([Episode.Iv, Episode.Tbogt], factory.CreatedFor);
        Assert.Equal(1, original.ClearCount);
        Assert.Equal(Episode.Tbogt, coordinator.BuiltForEpisode);
        Assert.Equal(1, original.RefreshCount);
    }

    /// <summary>
    /// The requirement that stunt jumps failing must never stop birds drawing, made structural.
    /// </summary>
    [Fact]
    public void OneTrackerThrowingDoesNotStopTheOthers()
    {
        (TrackerCoordinator coordinator, FakeTrackerFactory factory, FakeLog log) = Build();
        FakeTracker broken = new("Stunt Jumps") { ThrowOnRefresh = new InvalidOperationException("boom") };
        FakeTracker healthy = new("Flying Rats");
        factory.Builder = _ => [broken, healthy];

        coordinator.Refresh(new FakeGameContext(), new ModSettings());

        Assert.Equal(1, broken.RefreshCount);
        Assert.Equal(1, healthy.RefreshCount);
        Assert.Equal(TrackerState.Ok, healthy.Status.State);
        Assert.Single(log.Errors);
        Assert.Contains("Stunt Jumps", log.Errors[0]);
    }

    [Fact]
    public void ResetClearsTrackersAndForcesARebuild()
    {
        (TrackerCoordinator coordinator, FakeTrackerFactory factory, _) = Build();
        FakeGameContext context = new() { Episode = Episode.Iv };

        coordinator.Refresh(context, new ModSettings());
        FakeTracker original = factory.Created[0];

        coordinator.Reset();

        Assert.Equal(1, original.ClearCount);
        Assert.Null(coordinator.BuiltForEpisode);
        Assert.Empty(coordinator.Trackers);

        coordinator.Refresh(context, new ModSettings());
        Assert.Equal(2, factory.CreatedFor.Count);
    }
}
