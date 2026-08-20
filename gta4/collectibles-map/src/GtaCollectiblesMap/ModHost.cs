using System;
using System.Collections.Generic;
using GtaCollectiblesMap.Core.Abstractions;
using GtaCollectiblesMap.Core.Config;
using GtaCollectiblesMap.Core.Model;
using GtaCollectiblesMap.Features;
using GtaCollectiblesMap.Ui;

namespace GtaCollectiblesMap;

/// <summary>
/// Drives the mod: pump the refresh loop, render the panel, clean up on exit.
/// </summary>
/// <remarks>
/// Everything it needs is injected, so it holds no <c>new</c> and no game types. Scheduling
/// lives in <see cref="RefreshThrottle"/>, tracker lifetime in <see cref="TrackerCoordinator"/>
/// and persistence timing in <see cref="DebouncedSettingsWriter"/> — this class only sequences
/// them.
/// </remarks>
public sealed class ModHost(
    ModSettings settings,
    IGameContext context,
    IMarkerRenderer renderer,
    TrackerCoordinator coordinator,
    RefreshThrottle throttle,
    DebouncedSettingsWriter settingsWriter,
    ISettingsUi ui,
    ILog log)
{
    /// <summary>The key that opens the settings panel.</summary>
    /// <remarks>
    /// Exposed on its own rather than handing out the settings object, so nothing outside can
    /// mutate configuration behind the panel's back.
    /// </remarks>
    public int MenuKey => settings.MenuKey;

    public void Initialise() =>
        log.Info($"loaded. Press {(System.Windows.Forms.Keys)settings.MenuKey} for settings.");

    public void Tick()
    {
        // Pumped every tick regardless of the game being ready, so an edit made in a menu still
        // reaches disk.
        settingsWriter.FlushIfDue(settings);

        if (!context.IsInGame)
        {
            return;
        }

        if (!throttle.ShouldRefresh(settings.RefreshIntervalMs))
        {
            return;
        }

        coordinator.Refresh(context, settings);
    }

    public void RenderUi() => ui.Render(BuildViewModel());

    public void ToggleUi() => ui.Toggle();

    public void Shutdown()
    {
        // Blips outlive the script that created them. Skipping this leaves the player's map
        // littered until they restart the game.
        try
        {
            coordinator.Reset();
            renderer.RemoveAll();
        }
        catch (Exception ex)
        {
            log.Error($"cleanup failed: {ex.Message}");
        }

        // A setting changed in the last moments before exit would otherwise still be sitting in
        // the debounce window.
        try
        {
            settingsWriter.Flush(settings);
        }
        catch (Exception ex)
        {
            log.Error($"could not save settings: {ex.Message}");
        }
    }

    private SettingsViewModel BuildViewModel()
    {
        List<DiagnosticLine> diagnostics = [];

        foreach (ICollectibleTracker tracker in coordinator.Trackers)
        {
            diagnostics.Add(TrackerDiagnostics.Describe(tracker));
        }

        diagnostics.Add(new DiagnosticLine(
            "Episode",
            coordinator.BuiltForEpisode?.ToString() ?? "not detected yet",
            false));

        diagnostics.Add(new DiagnosticLine(
            "Active markers",
            renderer.ActiveCount.ToString(),
            false));

        return new SettingsViewModel
        {
            Settings = settings,
            Diagnostics = diagnostics,
            NotifyChanged = settingsWriter.MarkChanged,
        };
    }
}
