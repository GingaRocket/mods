using System.Collections.Generic;
using GtaCollectiblesMap.Core.Model;
using GtaCollectiblesMap.Features.Data;

namespace GtaCollectiblesMap.Features;

/// <summary>
/// Builds the known-coordinate table for the bird collectible of a given episode.
/// </summary>
/// <remarks>
/// Borough order is fixed and must stay that way. Table position is the collectible's
/// identity, so reordering these lists would renumber every bird and invalidate the marker
/// diff across a reload.
/// </remarks>
public static class CollectibleTables
{
    public const int ExpectedPigeonCount = 200;

    public const int ExpectedSeagullCount = 50;

    public static KnownCollectibleTable ForBirds(Episode episode) => episode switch
    {
        Episode.Tlad => Build(SeagullsTlad.Entries, "Seagull"),
        Episode.Tbogt => Build(SeagullsTbogt.Entries, "Seagull"),
        _ => BuildPigeons(),
    };

    private static KnownCollectibleTable BuildPigeons()
    {
        List<KnownCollectible> all = new(ExpectedPigeonCount);
        all.AddRange(PigeonsAlderney.Alderney);
        all.AddRange(PigeonsAlgonquin.Algonquin);
        all.AddRange(PigeonsBohan.Bohan);
        all.AddRange(PigeonsDukesBroker.DukesBroker);

        return Build(all, "Pigeon");
    }

    /// <summary>
    /// Numbers the entries and folds the neighbourhood into the label.
    /// </summary>
    /// <remarks>
    /// Only the neighbourhood survives from the source data. The original entries carry a
    /// multi-line prose hint as well, which is far too long for a blip's hover text.
    /// Seagull entries have no name at all, so they get a bare ordinal.
    /// </remarks>
    private static KnownCollectibleTable Build(IReadOnlyList<KnownCollectible> source, string noun)
    {
        List<KnownCollectible> labelled = new(source.Count);

        for (int i = 0; i < source.Count; i++)
        {
            KnownCollectible entry = source[i];
            string ordinal = $"{noun} {i + 1}";
            string label = string.IsNullOrWhiteSpace(entry.Label)
                ? ordinal
                : $"{ordinal} ({entry.Label})";

            labelled.Add(new KnownCollectible(entry.Position, label));
        }

        return new KnownCollectibleTable(labelled);
    }
}
