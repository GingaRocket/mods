using GtaCollectiblesMap.Core.Model;

namespace GtaCollectiblesMap.Game;

/// <summary>
/// Conversions between the game's types and Core's. Kept in one place so the Core boundary
/// stays a single, obvious hop.
/// </summary>
internal static class GameInterop
{
    public static Vec3 ToVec3(this GTA.Vector3 value) =>
        new(value.X, value.Y, value.Z);

    public static GTA.Vector3 ToGameVector(this Vec3 value) =>
        new(value.X, value.Y, value.Z);
}
