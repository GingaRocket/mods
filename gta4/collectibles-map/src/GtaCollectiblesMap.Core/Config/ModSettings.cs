using GtaCollectiblesMap.Core.Model;

namespace GtaCollectiblesMap.Core.Config;

/// <summary>
/// Every user-facing setting. Mutable because the in-game panel edits it live; the INI
/// is loaded into this on startup and rewritten whenever a value changes.
/// </summary>
public sealed class ModSettings
{
    // --- Categories -------------------------------------------------------

    public bool ShowBirds { get; set; } = true;

    public bool ShowStuntJumps { get; set; } = true;

    /// <summary>
    /// Also show items the player has already collected, faded. Off by default because the
    /// common case is "show me what is left".
    /// </summary>
    public bool ShowAll { get; set; }

    /// <summary>
    /// Whether already-collected items are also promoted onto the mini-radar when nearby.
    /// Inert while <see cref="ShowAll"/> is off, since there are no collected markers to promote.
    /// </summary>
    public bool ShowAllOnMinimap { get; set; }

    // --- Display ----------------------------------------------------------

    /// <summary>Metres within which a marker is also shown on the mini-radar.</summary>
    public float RadarRadius { get; set; } = 250f;

    public int RefreshIntervalMs { get; set; } = 1500;

    public BlipColour BirdColour { get; set; } = BlipColour.Teal;

    public BlipColour StuntJumpColour { get; set; } = BlipColour.DarkOrange;

    /// <summary>Base marker size. Kept small so 200 dots stay separable in dense Algonquin.</summary>
    public float MarkerScale { get; set; } = 0.7f;

    /// <summary>
    /// Opacity of collected markers, 0..1.
    /// </summary>
    /// <remarks>
    /// Alpha alone is not enough. A translucent blip blends with whatever is under it, and the
    /// pause map runs from dark water to pale land, so the same value recedes over one and
    /// nearly vanishes over the other. <see cref="CollectedScale"/> carries the cue that does
    /// not depend on the background.
    /// </remarks>
    public float CollectedAlpha { get; set; } = 0.35f;

    /// <summary>Size of collected markers relative to <see cref="MarkerScale"/>.</summary>
    public float CollectedScale { get; set; } = 0.6f;

    // --- Diagnostics and escape hatches ----------------------------------

    public bool DebugLogging { get; set; }

    /// <summary>Virtual-key code that opens the settings panel. INI only.</summary>
    public int MenuKey { get; set; } = 0x76; // VK_F7

    public ModSettings Clone() => (ModSettings)MemberwiseClone();
}
