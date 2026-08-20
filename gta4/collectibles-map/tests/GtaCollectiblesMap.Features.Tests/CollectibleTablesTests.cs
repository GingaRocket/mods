using System.Collections.Generic;
using GtaCollectiblesMap.Core.Model;
using Xunit;

namespace GtaCollectiblesMap.Features.Tests;

/// <summary>
/// Guards the coordinate tables ported from pigeon-locator. These are hand-transcribed game
/// data, so a silently dropped or duplicated line is a real risk and would be invisible in
/// game — it would just look like a collectible that never appears.
/// </summary>
public class CollectibleTablesTests
{
    [Fact]
    public void GtaIvHasExactlyTwoHundredPigeons()
    {
        Assert.Equal(
            CollectibleTables.ExpectedPigeonCount,
            CollectibleTables.ForBirds(Episode.Iv).Count);
    }

    [Theory]
    [InlineData(Episode.Tlad)]
    [InlineData(Episode.Tbogt)]
    public void EachEpisodeHasExactlyFiftySeagulls(Episode episode)
    {
        Assert.Equal(
            CollectibleTables.ExpectedSeagullCount,
            CollectibleTables.ForBirds(episode).Count);
    }

    [Theory]
    [InlineData(Episode.Iv)]
    [InlineData(Episode.Tlad)]
    [InlineData(Episode.Tbogt)]
    public void LabelsAreUnique(Episode episode)
    {
        KnownCollectibleTable table = CollectibleTables.ForBirds(episode);
        HashSet<string> seen = [];

        foreach (KnownCollectible entry in table.Entries)
        {
            Assert.True(seen.Add(entry.Label), $"duplicate label: {entry.Label}");
        }
    }

    /// <summary>
    /// Two entries closer together than the match epsilon would be indistinguishable at
    /// runtime: one pickup could satisfy both, permanently marking a real collectible as done.
    /// </summary>
    [Theory]
    [InlineData(Episode.Iv)]
    [InlineData(Episode.Tlad)]
    [InlineData(Episode.Tbogt)]
    public void NoTwoEntriesAreCloserThanTheMatchEpsilon(Episode episode)
    {
        IReadOnlyList<KnownCollectible> entries = CollectibleTables.ForBirds(episode).Entries;
        float limit = KnownCollectibleTable.MatchEpsilon * KnownCollectibleTable.MatchEpsilon;

        for (int i = 0; i < entries.Count; i++)
        {
            for (int j = i + 1; j < entries.Count; j++)
            {
                float distance = entries[i].Position.DistanceSquaredTo(entries[j].Position);
                Assert.True(
                    distance > limit,
                    $"entries {i} and {j} are {System.Math.Sqrt(distance):0.###} apart");
            }
        }
    }

    [Fact]
    public void PigeonLabelsCarryTheirNeighbourhood()
    {
        KnownCollectibleTable table = CollectibleTables.ForBirds(Episode.Iv);

        Assert.StartsWith("Pigeon 1 (", table.Entries[0].Label);
    }

    /// <summary>Seagull source data has no names, so they fall back to a bare ordinal.</summary>
    [Fact]
    public void SeagullLabelsAreBareOrdinals()
    {
        KnownCollectibleTable table = CollectibleTables.ForBirds(Episode.Tlad);

        Assert.Equal("Seagull 1", table.Entries[0].Label);
        Assert.Equal("Seagull 50", table.Entries[49].Label);
    }
}
