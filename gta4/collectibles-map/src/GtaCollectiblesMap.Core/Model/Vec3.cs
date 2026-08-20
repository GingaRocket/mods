namespace GtaCollectiblesMap.Core.Model;

/// <summary>
/// A world position, independent of any game SDK type.
/// </summary>
/// <remarks>
/// Core defines its own vector rather than using the SDK's so that this assembly
/// carries no game dependency. The Game layer converts at its boundary.
/// </remarks>
public readonly record struct Vec3(float X, float Y, float Z)
{
    public static readonly Vec3 Zero = new(0f, 0f, 0f);

    /// <summary>Squared distance, to avoid a square root in hot comparisons.</summary>
    public float DistanceSquaredTo(Vec3 other)
    {
        float dx = X - other.X;
        float dy = Y - other.Y;
        float dz = Z - other.Z;
        return (dx * dx) + (dy * dy) + (dz * dz);
    }

    public float DistanceTo(Vec3 other) => (float)System.Math.Sqrt(DistanceSquaredTo(other));

    /// <summary>
    /// Horizontal distance only. Radar proximity ignores height so a collectible
    /// directly below a bridge still registers as nearby.
    /// </summary>
    public float FlatDistanceSquaredTo(Vec3 other)
    {
        float dx = X - other.X;
        float dy = Y - other.Y;
        return (dx * dx) + (dy * dy);
    }

    public override string ToString() => $"({X:0.##}, {Y:0.##}, {Z:0.##})";
}
