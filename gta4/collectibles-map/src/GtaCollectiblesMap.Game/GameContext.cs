using System;
using GtaCollectiblesMap.Core.Abstractions;
using GtaCollectiblesMap.Core.Model;
using GTA;

namespace GtaCollectiblesMap.Game;

/// <summary>Reads live player and episode state out of the game.</summary>
public sealed class GameContext(ILog log) : IGameContext
{
    private Vec3 _lastKnownPosition = Vec3.Zero;

    // These are polled every tick, so a persistent fault would otherwise produce either a flood
    // of log lines or - as before - total silence. Report each kind once, then stay quiet.
    private bool _warnedAboutPlayerState;

    private bool _warnedAboutPosition;

    private bool _warnedAboutEpisode;

    public bool IsInGame
    {
        get
        {
            try
            {
                Player? player = GTA.Game.LocalPlayer;
                Ped? ped = player?.Character;
                return player is not null && player.isActive && ped is not null && ped.Exists();
            }
            catch (Exception ex)
            {
                // Expected during load and shutdown, when the player pool is not built yet.
                WarnOnce(ref _warnedAboutPlayerState, $"could not read player state: {ex.Message}");
                return false;
            }
        }
    }

    public Vec3 PlayerPosition
    {
        get
        {
            try
            {
                Ped? ped = GTA.Game.LocalPlayer?.Character;
                if (ped is null || !ped.Exists())
                {
                    return _lastKnownPosition;
                }

                _lastKnownPosition = ped.Position.ToVec3();
                return _lastKnownPosition;
            }
            catch (Exception ex)
            {
                // Hold the last good value rather than snapping to the origin, which would
                // briefly promote every marker near Broker onto the radar.
                WarnOnce(ref _warnedAboutPosition, $"could not read player position: {ex.Message}");
                return _lastKnownPosition;
            }
        }
    }

    public Episode Episode
    {
        get
        {
            try
            {
                return (int)GTA.Game.CurrentEpisode switch
                {
                    1 => Core.Model.Episode.Tlad,
                    2 => Core.Model.Episode.Tbogt,
                    _ => Core.Model.Episode.Iv,
                };
            }
            catch (Exception ex)
            {
                WarnOnce(ref _warnedAboutEpisode, $"could not read the current episode: {ex.Message}");
                return Core.Model.Episode.Iv;
            }
        }
    }

    private void WarnOnce(ref bool alreadyWarned, string message)
    {
        if (alreadyWarned)
        {
            return;
        }

        alreadyWarned = true;
        log.Warn($"{message} (reported once; further occurrences are silent)");
    }
}
