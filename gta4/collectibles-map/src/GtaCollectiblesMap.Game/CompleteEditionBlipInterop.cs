using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using GtaCollectiblesMap.Core.Model;
using GTA;

namespace GtaCollectiblesMap.Game;

/// <summary>Complete Edition-safe operations missing or misdeclared in ScriptHookDotNet.</summary>
/// <remarks>
/// Keep every raw native call and numeric compatibility correction here. The renderer should
/// describe intent; this class owns the quirks needed to express it on CE 1.2.0.59 through the
/// legacy ScriptHookDotNet 1.7.1.8 API.
/// </remarks>
internal static class CompleteEditionBlipInterop
{
    // ScriptHookDotNet's BlipDisplay labels do not match GTA IV's observed values.
    private const int MapAndRadarDisplay = 2;
    private const int MapOnlyDisplay = 3;

    private static readonly object NameLock = new();
    private static readonly Dictionary<string, IntPtr> PersistentNames = [];

    public static void SetAlpha(Blip blip, byte alpha)
    {
        // Blip.Transparency sends a float even though CHANGE_BLIP_ALPHA expects an integer.
        GTA.Native.Function.Call("CHANGE_BLIP_ALPHA", blip, (int)alpha);
    }

    public static void SetDisplay(Blip blip, MarkerDisplay display)
    {
        blip.Display = display switch
        {
            MarkerDisplay.MapOnly => (BlipDisplay)MapOnlyDisplay,
            MarkerDisplay.MapAndRadar => (BlipDisplay)MapAndRadarDisplay,
            _ => BlipDisplay.Hidden,
        };
    }

    public static void SetName(Blip blip, string name)
    {
        IntPtr namePointer;
        lock (NameLock)
        {
            if (!PersistentNames.TryGetValue(name, out namePointer))
            {
                namePointer = Marshal.StringToHGlobalAnsi(name);
                PersistentNames.Add(name, namePointer);
            }
        }

        // Do not call Blip.Name on Complete Edition. ScriptHookDotNet frees its temporary
        // unmanaged string immediately after CHANGE_BLIP_NAME_FROM_ASCII returns. On the CE
        // compatibility path, hovering later behaves as though that pointer was retained.
        // These strings intentionally live until the game process exits.
        GTA.Native.Function.Call("CHANGE_BLIP_NAME_FROM_ASCII", blip, namePointer.ToInt32());
    }
}
