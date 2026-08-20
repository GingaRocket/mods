using System;
using System.IO;
using GtaCollectiblesMap.Core;
using GtaCollectiblesMap.Core.Abstractions;
using GtaCollectiblesMap.Core.Config;
using GtaCollectiblesMap.Features;
using GtaCollectiblesMap.Game;
using GtaCollectiblesMap.Ui;
using GTA;

namespace GtaCollectiblesMap;

/// <summary>ScriptHookDotNet entry point and composition root.</summary>
public sealed class CollectiblesScript : Script
{
    private readonly ModHost _host;
    private readonly RadarMarkerRenderer _renderer;
    private readonly TaxiRideDetector _taxiRideDetector;
    private readonly IGameContext _context;
    private readonly ILog _log;
    private bool _shutDown;
    private bool _swept;
    private bool _reportedOwnMarkers;

    public CollectiblesScript()
    {
        ISettingsStore store = new IniSettingsStore(ResolveIniPath());
        ModSettings settings = store.Load();

        IClock clock = new SystemClock();
        ILog log = new GameLog("CollectiblesMap", () => settings.DebugLogging);
        IGameContext context = new GameContext(log);

        // Concrete on purpose: the sweep is a Complete Edition housekeeping concern, not part
        // of the rendering contract Core defines. It cannot run here - the script is
        // constructed seconds before the world exists, so there would be nothing to find yet.
        RadarMarkerRenderer renderer = new(log);
        _renderer = renderer;
        _taxiRideDetector = new TaxiRideDetector(log);
        _context = context;
        _log = log;

        ICollectibleTrackerFactory trackerFactory = new GameTrackerFactory(renderer, log);

        _host = new ModHost(
            settings,
            context,
            renderer,
            new TrackerCoordinator(trackerFactory, log),
            new RefreshThrottle(clock),
            new DebouncedSettingsWriter(store, clock),
            new ScriptHookSettingsUi(),
            log);

        Tick += OnTick;
        KeyDown += OnKeyDown;
        AppDomain.CurrentDomain.DomainUnload += OnDomainExit;
        AppDomain.CurrentDomain.ProcessExit += OnDomainExit;

        _host.Initialise();
    }

    private void OnTick(object sender, EventArgs e)
    {
        SweepOnce();
        _renderer.SetTaxiSafeIcons(_taxiRideDetector.ShouldUseTaxiSafeIcons());
        _host.Tick();
        ReportOwnMarkersOnce();
        _host.RenderUi();
    }

    /// <summary>
    /// Reports what the game says about our own markers, once they exist.
    /// </summary>
    /// <remarks>
    /// Runs after <see cref="ModHost.Tick"/>, unlike the census, and that is the entire point:
    /// the census can only ever describe blips this mod did not create. Without this there is
    /// no ground truth for what a blip of ours looks like from the outside, and identifying
    /// anything else on the map is guesswork.
    /// </remarks>
    private void ReportOwnMarkersOnce()
    {
        if (_reportedOwnMarkers || _renderer.ActiveCount == 0)
        {
            return;
        }

        _reportedOwnMarkers = true;
        _renderer.LogOwnMarkerFacts();
    }

    /// <summary>
    /// Records the startup blip census on the first tick the world is actually up.
    /// </summary>
    /// <remarks>
    /// Deliberately not done in the constructor. The script is loaded several seconds before
    /// the world is ready — the log shows gaps of one and a half to five and a half seconds
    /// between this script loading and the first successful game read — so a sweep there runs
    /// against an empty world and reports nothing regardless of what is really on the map.
    /// This runs before <see cref="ModHost.Tick"/>, so no marker of our own exists yet.
    /// </remarks>
    private void SweepOnce()
    {
        if (_swept || !_context.IsInGame)
        {
            return;
        }

        _swept = true;

        // The census only reads. A sprite match cannot distinguish this mod's remnants from
        // game-owned objective markers. Nothing may delete a blip this mod did not create and
        // is not still tracking.
        _renderer.LogBlipCensus();
    }

    private void OnKeyDown(object sender, GTA.KeyEventArgs e)
    {
        if ((int)e.Key == _host.MenuKey)
        {
            _host.ToggleUi();
        }
    }

    private void OnDomainExit(object sender, EventArgs e)
    {
        if (_shutDown)
        {
            return;
        }

        _shutDown = true;
        _host.Shutdown();
    }

    /// <summary>Keeps the INI in the scripts folder despite SHDN loading assemblies from bytes.</summary>
    private static string ResolveIniPath()
    {
        try
        {
            return Path.Combine(GTA.Game.InstallFolder, "scripts", "GtaCollectiblesMap.ini");
        }
        catch
        {
            // Fall through to the game's working directory.
        }

        return "GtaCollectiblesMap.ini";
    }
}
