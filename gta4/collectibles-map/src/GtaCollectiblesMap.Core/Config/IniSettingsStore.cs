using GtaCollectiblesMap.Core.Abstractions;
using GtaCollectiblesMap.Core.Model;

namespace GtaCollectiblesMap.Core.Config;

/// <summary>Maps <see cref="ModSettings"/> to and from an INI file.</summary>
/// <remarks>
/// Keeps its own INI reader rather than taking a dependency: every runtime assembly has to be
/// deployed beside the script in the game folder, and the one the SDK offers rebuilds the file
/// by re-inserting comments at their original absolute line numbers — which drifts as soon as
/// the set of keys changes. <see cref="IniDocument"/> instead rewrites values in place, so
/// comments never move relative to what they document.
/// </remarks>
public sealed class IniSettingsStore(string path) : ISettingsStore
{
    public string Path => path;

    public ModSettings Load()
    {
        IniDocument ini = IniDocument.Load(path);
        ModSettings defaults = new();

        return new ModSettings
        {
            ShowBirds = ini.GetBool(nameof(ModSettings.ShowBirds), defaults.ShowBirds),
            ShowStuntJumps = ini.GetBool(nameof(ModSettings.ShowStuntJumps), defaults.ShowStuntJumps),
            ShowAll = ini.GetBool(nameof(ModSettings.ShowAll), defaults.ShowAll),
            ShowAllOnMinimap = ini.GetBool(nameof(ModSettings.ShowAllOnMinimap), defaults.ShowAllOnMinimap),
            RadarRadius = ini.GetFloat(nameof(ModSettings.RadarRadius), defaults.RadarRadius),
            RefreshIntervalMs = ini.GetInt(nameof(ModSettings.RefreshIntervalMs), defaults.RefreshIntervalMs),
            BirdColour = ini.GetEnum(nameof(ModSettings.BirdColour), defaults.BirdColour),
            StuntJumpColour = ini.GetEnum(nameof(ModSettings.StuntJumpColour), defaults.StuntJumpColour),
            MarkerScale = ini.GetFloat(nameof(ModSettings.MarkerScale), defaults.MarkerScale),
            CollectedAlpha = ini.GetFloat(nameof(ModSettings.CollectedAlpha), defaults.CollectedAlpha),
            CollectedScale = ini.GetFloat(nameof(ModSettings.CollectedScale), defaults.CollectedScale),
            DebugLogging = ini.GetBool(nameof(ModSettings.DebugLogging), defaults.DebugLogging),
            MenuKey = ini.GetInt(nameof(ModSettings.MenuKey), defaults.MenuKey),
        };
    }

    /// <summary>
    /// Writes settings back, preserving the existing file's comments and ordering so a
    /// hand-annotated INI is not flattened when the panel changes one value.
    /// </summary>
    public void Save(ModSettings settings)
    {
        IniDocument ini = IniDocument.Load(path);

        ini.Set(nameof(ModSettings.ShowBirds), settings.ShowBirds);
        ini.Set(nameof(ModSettings.ShowStuntJumps), settings.ShowStuntJumps);
        ini.Set(nameof(ModSettings.ShowAll), settings.ShowAll);
        ini.Set(nameof(ModSettings.ShowAllOnMinimap), settings.ShowAllOnMinimap);
        ini.Set(nameof(ModSettings.RadarRadius), settings.RadarRadius);
        ini.Set(nameof(ModSettings.RefreshIntervalMs), settings.RefreshIntervalMs);
        ini.SetEnum(nameof(ModSettings.BirdColour), settings.BirdColour);
        ini.SetEnum(nameof(ModSettings.StuntJumpColour), settings.StuntJumpColour);
        ini.Set(nameof(ModSettings.MarkerScale), settings.MarkerScale);
        ini.Set(nameof(ModSettings.CollectedAlpha), settings.CollectedAlpha);
        ini.Set(nameof(ModSettings.CollectedScale), settings.CollectedScale);
        ini.Set(nameof(ModSettings.DebugLogging), settings.DebugLogging);

        // MenuKey is intentionally not written back: it is INI-only, read once at startup.

        ini.Save(path);
    }
}
