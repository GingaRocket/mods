using GtaCollectiblesMap.Core.Config;
using GtaCollectiblesMap.Core.Model;
using Xunit;

namespace GtaCollectiblesMap.Features.Tests;

public class MarkerStyleResolverTests
{
    private static MarkerStyle Resolve(
        bool isCollected,
        bool isNear,
        bool showAllOnMinimap = false,
        CollectibleCategory category = CollectibleCategory.Bird) =>
        MarkerStyleResolver.Resolve(
            category,
            isCollected,
            isNear,
            new ModSettings { ShowAllOnMinimap = showAllOnMinimap });

    [Fact]
    public void DistantMarkersStayOnThePauseMapOnly()
    {
        Assert.Equal(MarkerDisplay.MapOnly, Resolve(isCollected: false, isNear: false).Display);
    }

    [Fact]
    public void NearbyUncollectedMarkersReachTheRadar()
    {
        Assert.Equal(MarkerDisplay.MapAndRadar, Resolve(isCollected: false, isNear: true).Display);
    }

    /// <summary>
    /// Faded dots on the mini-radar are mostly clutter, so collected items stay off it unless
    /// the user opts in.
    /// </summary>
    [Fact]
    public void NearbyCollectedMarkersStayOffTheRadarByDefault()
    {
        Assert.Equal(MarkerDisplay.MapOnly, Resolve(isCollected: true, isNear: true).Display);
    }

    [Fact]
    public void ShowAllOnMinimapLetsCollectedMarkersReachTheRadar()
    {
        Assert.Equal(
            MarkerDisplay.MapAndRadar,
            Resolve(isCollected: true, isNear: true, showAllOnMinimap: true).Display);
    }

    [Fact]
    public void EachCategoryGetsItsOwnColour()
    {
        MarkerStyle bird = Resolve(false, false, category: CollectibleCategory.Bird);
        MarkerStyle jump = Resolve(false, false, category: CollectibleCategory.StuntJump);

        Assert.NotEqual(bird.Colour, jump.Colour);
    }

    /// <summary>
    /// Every default must avoid the colours GTA IV already uses. Blue in particular is not
    /// merely a clash: the game forces a blip set to it to read "Friend".
    /// </summary>
    [Fact]
    public void DefaultColoursDoNotCollideWithTheGameSOwnMarkers()
    {
        ModSettings defaults = new();

        Assert.False(BlipColours.IsReserved(defaults.BirdColour));
        Assert.False(BlipColours.IsReserved(defaults.StuntJumpColour));
    }

    [Fact]
    public void ReservedColoursAreNotOfferedInTheSettingsDropdown()
    {
        foreach (BlipColour colour in BlipColours.Selectable())
        {
            Assert.False(BlipColours.IsReserved(colour));
        }

        Assert.DoesNotContain(BlipColour.Blue, BlipColours.Selectable());
    }

    [Fact]
    public void CollectedMarkersFadeButKeepTheirHue()
    {
        MarkerStyle remaining = Resolve(isCollected: false, isNear: false);
        MarkerStyle collected = Resolve(isCollected: true, isNear: false);

        Assert.Equal(remaining.Colour, collected.Colour);
        Assert.True(collected.Alpha < remaining.Alpha);
        Assert.True(collected.Scale < remaining.Scale);
    }
}
