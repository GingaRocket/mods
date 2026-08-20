using GtaCollectiblesMap.Core.Config;
using Xunit;

namespace GtaCollectiblesMap.Core.Tests;

public class IniDocumentTests
{
    [Fact]
    public void ReadsTypedValues()
    {
        IniDocument ini = IniDocument.Parse("ShowAll = true\nRadarRadius = 312.5\nRefreshIntervalMs=900\n");

        Assert.True(ini.GetBool("ShowAll", false));
        Assert.Equal(312.5f, ini.GetFloat("RadarRadius", 0f));
        Assert.Equal(900, ini.GetInt("RefreshIntervalMs", 0));
    }

    [Fact]
    public void FallsBackWhenKeyMissingOrUnparseable()
    {
        IniDocument ini = IniDocument.Parse("RadarRadius = not-a-number\n");

        Assert.Equal(250f, ini.GetFloat("RadarRadius", 250f));
        Assert.Equal(7, ini.GetInt("Absent", 7));
    }

    /// <summary>
    /// The settings panel rewrites the file whenever a value changes, so a hand-annotated
    /// INI must survive that round trip intact.
    /// </summary>
    [Fact]
    public void PreservesCommentsAndLayoutWhenRewritingAValue()
    {
        const string original = "; radar tuning\nRadarRadius = 250\n\n; categories\nShowAll = false\n";
        IniDocument ini = IniDocument.Parse(original);

        ini.Set("ShowAll", true);
        string rendered = ini.Render();

        Assert.Contains("; radar tuning", rendered);
        Assert.Contains("; categories", rendered);
        Assert.Contains("RadarRadius = 250", rendered);
        Assert.Contains("ShowAll = true", rendered);
        Assert.DoesNotContain("ShowAll = false", rendered);
    }

    [Fact]
    public void AppendsKeysThatWereNotAlreadyPresent()
    {
        IniDocument ini = IniDocument.Parse("RadarRadius = 250\n");

        ini.Set("ShowAll", true);

        Assert.Contains("ShowAll = true", ini.Render());
    }
}
