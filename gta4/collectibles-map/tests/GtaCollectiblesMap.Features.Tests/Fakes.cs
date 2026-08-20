using System.Collections.Generic;
using GtaCollectiblesMap.Core.Abstractions;
using GtaCollectiblesMap.Core.Model;

namespace GtaCollectiblesMap.Features.Tests;

internal sealed class FakeSource(CollectibleCategory category) : ICollectibleSource
{
    public CollectibleCategory Category { get; } = category;

    public List<SourceItem> Items { get; } = [];

    public string? Error { get; set; }

    public int ReadCount { get; private set; }

    public ReadResult Read()
    {
        ReadCount++;
        return Error is null ? ReadResult.Ok(Items) : ReadResult.Fail(Error);
    }
}

internal sealed class FakeRenderer : IMarkerRenderer
{
    public Dictionary<CollectibleKey, MarkerState> Markers { get; } = [];

    public List<string> Operations { get; } = [];

    public int ActiveCount => Markers.Count;

    public void Add(CollectibleKey key, MarkerState state)
    {
        Markers[key] = state;
        Operations.Add($"add:{key}");
    }

    public void Update(CollectibleKey key, MarkerState state)
    {
        Markers[key] = state;
        Operations.Add($"update:{key}");
    }

    public void Remove(CollectibleKey key)
    {
        Markers.Remove(key);
        Operations.Add($"remove:{key}");
    }

    public void RemoveAll()
    {
        Markers.Clear();
        Operations.Add("removeAll");
    }
}

internal sealed class FakeGameContext : IGameContext
{
    public bool IsInGame { get; set; } = true;

    public Vec3 PlayerPosition { get; set; } = Vec3.Zero;

    public Episode Episode { get; set; } = Episode.Iv;
}

internal sealed class FakeLog : ILog
{
    public List<string> Infos { get; } = [];

    public List<string> Warnings { get; } = [];

    public List<string> Errors { get; } = [];

    public void Info(string message) => Infos.Add(message);

    public void Warn(string message) => Warnings.Add(message);

    public void Error(string message) => Errors.Add(message);

    public void Debug(string message) { }
}
