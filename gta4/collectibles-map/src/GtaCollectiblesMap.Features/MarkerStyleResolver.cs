using GtaCollectiblesMap.Core.Config;
using GtaCollectiblesMap.Core.Model;

namespace GtaCollectiblesMap.Features;

/// <summary>
/// Decides how a marker looks. Every colour, fade and size rule lives here, so the visual
/// language is defined in exactly one place rather than scattered through rendering.
/// </summary>
public static class MarkerStyleResolver
{
    public static MarkerStyle Resolve(
        CollectibleCategory category,
        bool isCollected,
        bool isNearPlayer,
        ModSettings settings)
    {
        BlipColour colour = category switch
        {
            CollectibleCategory.Bird => settings.BirdColour,
            CollectibleCategory.StuntJump => settings.StuntJumpColour,
            _ => BlipColour.White,
        };

        // Collected items keep their category's hue rather than turning grey, so a done
        // pigeon still reads as a pigeon. They recede by fading *and* shrinking: alpha alone
        // is background-dependent and can nearly vanish over dark water.
        float scale = isCollected
            ? settings.MarkerScale * settings.CollectedScale
            : settings.MarkerScale;

        byte alpha = isCollected
            ? ToAlphaByte(settings.CollectedAlpha)
            : (byte)255;

        return new MarkerStyle(colour, scale, alpha, ResolveDisplay(isCollected, isNearPlayer, settings));
    }

    private static MarkerDisplay ResolveDisplay(bool isCollected, bool isNearPlayer, ModSettings settings)
    {
        if (!isNearPlayer)
        {
            return MarkerDisplay.MapOnly;
        }

        // Nearby uncollected items always reach the radar. Collected ones only do so when the
        // user opts in, because faded dots on the mini-radar are mostly clutter.
        if (isCollected && !settings.ShowAllOnMinimap)
        {
            return MarkerDisplay.MapOnly;
        }

        return MarkerDisplay.MapAndRadar;
    }

    private static byte ToAlphaByte(float fraction)
    {
        float clamped = fraction < 0f ? 0f : fraction > 1f ? 1f : fraction;
        return (byte)(clamped * 255f);
    }
}
