using GtaCollectiblesMap.Core.Model;

namespace GtaCollectiblesMap.Features;

/// <summary>Turns a tracker's state into a line the settings panel can show.</summary>
public static class TrackerDiagnostics
{
    public static DiagnosticLine Describe(ICollectibleTracker tracker)
    {
        TrackerStatus status = tracker.Status;

        return status.State switch
        {
            TrackerState.Ok =>
                new DiagnosticLine(tracker.Name, $"{status.Remaining} of {status.Total} remaining", false),
            TrackerState.Disabled =>
                new DiagnosticLine(tracker.Name, "off", false),
            TrackerState.Waiting =>
                new DiagnosticLine(tracker.Name, "waiting for the game", false),

            // Flagged as a problem so it renders in red. A tracker that has failed shows no
            // markers, which is indistinguishable from having collected everything.
            _ => new DiagnosticLine(tracker.Name, status.Message ?? "unavailable", true),
        };
    }
}
