using System;
using GtaCollectiblesMap.Core.Abstractions;
using GTA;

namespace GtaCollectiblesMap.Game;

/// <summary>Detects the window in which collectible sprites must be invisible to taxi scripts.</summary>
public sealed class TaxiRideDetector(ILog log)
{
    private bool _warned;

    /// <summary>
    /// True while entering any vehicle or while seated in a taxi as a passenger.
    /// </summary>
    /// <remarks>
    /// Vehicle entry deliberately starts the window early. The stock taxi script snapshots
    /// destination sprites as the passenger settles into the cab; waiting until the character
    /// is fully seated risks changing the sprites one script frame after that snapshot.
    /// Entering a non-taxi therefore uses the alternate icons for only the entry animation.
    /// </remarks>
    public bool ShouldUseTaxiSafeIcons()
    {
        try
        {
            Ped? player = GTA.Game.LocalPlayer?.Character;
            if (player is null || !player.Exists())
            {
                return false;
            }

            if (GTA.Native.Function.Call<bool>("IS_CHAR_GETTING_IN_TO_A_CAR", player))
            {
                return true;
            }

            if (!GTA.Native.Function.Call<bool>("IS_CHAR_IN_TAXI", player))
            {
                return false;
            }

            Vehicle? taxi = player.CurrentVehicle;
            if (taxi is null || !taxi.Exists())
            {
                // The native already proved this is a taxi. Prefer excluding the blips while
                // the SDK's vehicle wrapper catches up rather than exposing them for one frame.
                return true;
            }

            Ped? driver = taxi.GetPedOnSeat(VehicleSeat.Driver);
            return driver is null || !driver.Exists() || !driver.Equals(player);
        }
        catch (Exception ex)
        {
            if (!_warned)
            {
                _warned = true;
                log.Warn($"could not detect taxi entry: {ex.Message} (reported once)");
            }

            return false;
        }
    }
}
