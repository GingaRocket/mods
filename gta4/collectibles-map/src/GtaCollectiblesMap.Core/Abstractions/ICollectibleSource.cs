using System.Collections.Generic;
using GtaCollectiblesMap.Core.Model;

namespace GtaCollectiblesMap.Core.Abstractions;

/// <summary>One raw item as reported by a source, before identity is resolved.</summary>
/// <param name="Position">World position.</param>
/// <param name="IsCollected">
/// Whether the source can say this item is already collected. Pool-backed sources such as
/// pickups cannot: a collected pigeon is deleted outright, so everything they return is
/// uncollected and this stays false. Stunt jumps keep their whole set in memory with a
/// passed flag, so they report it directly.
/// </param>
/// <param name="Label">Optional human-readable name for the map hover text.</param>
/// <param name="SourceIndex">Index within the source's own storage.</param>
public readonly record struct SourceItem(
    Vec3 Position,
    bool IsCollected,
    string? Label,
    int SourceIndex);

/// <summary>Reads the live state of one collectible category out of the game.</summary>
public interface ICollectibleSource
{
    CollectibleCategory Category { get; }

    /// <summary>
    /// Reads what the game currently holds. Returns a failure rather than throwing so one
    /// broken source disables only itself.
    /// </summary>
    ReadResult Read();
}

/// <summary>Outcome of a source read.</summary>
public sealed record ReadResult
{
    public required bool Success { get; init; }

    public IReadOnlyList<SourceItem> Items { get; init; } = [];

    public string? Error { get; init; }

    public static ReadResult Ok(IReadOnlyList<SourceItem> items) =>
        new() { Success = true, Items = items };

    public static ReadResult Fail(string error) =>
        new() { Success = false, Error = error };
}
