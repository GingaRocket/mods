using System.Collections.Generic;

namespace GtaCollectiblesMap.Core.Model;

/// <summary>
/// GTA IV's blip colour palette. <c>CHANGE_BLIP_COLOUR</c> takes an index in 0..19,
/// not an RGB value, so colour selection is a choice from this fixed set.
/// </summary>
public enum BlipColour
{
    White = 0,
    Red = 1,
    Green = 2,
    Blue = 3,
    Black = 4,
    Magenta = 5,
    Orange = 6,
    Violet = 7,
    BrightGreen = 8,
    BrightRed = 9,
    DarkPink = 10,
    DarkOrange = 11,
    Teal = 12,
    Cyan = 13,
    LightYellow = 14,
    DarkGreen = 15,
    Purple = 16,
    LightPurple = 17,
    LightOrange = 18,
    Yellow = 19,
}

public static class BlipColours
{
    /// <summary>
    /// Colours the game already uses for its own markers. Reusing one makes our blips
    /// read as something they are not, so the settings UI must exclude these.
    /// </summary>
    /// <remarks>
    /// <see cref="BlipColour.Blue"/> is special: it is not merely a visual clash. Setting a
    /// blip to colour 3 makes the game force its hover text to "Friend", so it is excluded
    /// on behavioural grounds rather than aesthetic ones.
    /// </remarks>
    public static readonly IReadOnlyDictionary<BlipColour, string> Reserved =
        new Dictionary<BlipColour, string>
        {
            [BlipColour.Yellow] = "Mission routes and waypoints; runway alignment markers",
            [BlipColour.LightYellow] = "Mission routes and waypoints; runway alignment markers",
            [BlipColour.Green] = "Player-set waypoints and GPS routes, safehouses, pickups",
            [BlipColour.BrightGreen] = "Player-set waypoints and GPS routes, safehouses, pickups",
            [BlipColour.Blue] = "Forces the blip's hover text to \"Friend\"",
            [BlipColour.Red] = "Enemies and wanted-level markers",
            [BlipColour.BrightRed] = "Enemies and wanted-level markers",
            [BlipColour.Magenta] = "Gang marker colour",
            [BlipColour.DarkPink] = "Gang marker colour",
            [BlipColour.Black] = "Invisible against the map",
        };

    public static bool IsReserved(BlipColour colour) => Reserved.ContainsKey(colour);

    /// <summary>Colours safe to offer in the settings dropdown.</summary>
    public static IEnumerable<BlipColour> Selectable()
    {
        foreach (BlipColour colour in System.Enum.GetValues(typeof(BlipColour)))
        {
            if (!IsReserved(colour))
            {
                yield return colour;
            }
        }
    }
}
