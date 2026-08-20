namespace GtaCollectiblesMap.Features;

public enum TrackerState
{
    /// <summary>Turned off by the user.</summary>
    Disabled,

    /// <summary>Not scanned yet, or the world is not ready.</summary>
    Waiting,

    Ok,

    /// <summary>
    /// Broken — typically the stunt jump pool address failing to resolve. The tracker
    /// disables itself and the rest of the mod carries on.
    /// </summary>
    Failed,
}

/// <summary>
/// What a tracker is currently doing, surfaced in the settings panel's diagnostics block.
/// </summary>
/// <remarks>
/// <see cref="Message"/> exists because a silent failure is indistinguishable from success
/// here: if stunt jumps never resolve, the map shows no stunt jump markers, which looks
/// exactly like having already completed all fifty.
/// </remarks>
public sealed record TrackerStatus(TrackerState State, string? Message, int Remaining, int Total)
{
    public static TrackerStatus Disabled { get; } = new(TrackerState.Disabled, null, 0, 0);

    public static TrackerStatus Waiting { get; } = new(TrackerState.Waiting, null, 0, 0);

    public static TrackerStatus Failed(string message) => new(TrackerState.Failed, message, 0, 0);

    public int Collected => Total - Remaining;
}
