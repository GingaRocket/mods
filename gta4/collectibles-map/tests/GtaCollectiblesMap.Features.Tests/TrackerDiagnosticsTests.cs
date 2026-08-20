using GtaCollectiblesMap.Core.Model;
using Xunit;

namespace GtaCollectiblesMap.Features.Tests;

internal sealed class StubTracker(string name, TrackerStatus status) : ICollectibleTracker
{
    public string Name { get; } = name;

    public TrackerStatus Status { get; } = status;

    public void Refresh(Core.Abstractions.IGameContext context, Core.Config.ModSettings settings) { }

    public void Clear() { }
}

public class TrackerDiagnosticsTests
{
    [Fact]
    public void ReportsProgressWhenRunning()
    {
        DiagnosticLine line = TrackerDiagnostics.Describe(
            new StubTracker("Flying Rats", new TrackerStatus(TrackerState.Ok, null, 137, 200)));

        Assert.Equal("Flying Rats", line.Name);
        Assert.Equal("137 of 200 remaining", line.Text);
        Assert.False(line.IsProblem);
    }

    [Theory]
    [InlineData(TrackerState.Disabled, "off")]
    [InlineData(TrackerState.Waiting, "waiting for the game")]
    public void DescribesTheNonRunningStates(TrackerState state, string expected)
    {
        DiagnosticLine line = TrackerDiagnostics.Describe(
            new StubTracker("Stunt Jumps", new TrackerStatus(state, null, 0, 0)));

        Assert.Equal(expected, line.Text);
        Assert.False(line.IsProblem);
    }

    /// <summary>
    /// A failed tracker draws no markers, which looks exactly like having collected everything.
    /// Surfacing the reason, flagged as a problem, is the only thing that distinguishes them.
    /// </summary>
    [Fact]
    public void FlagsFailureAsAProblemAndShowsWhy()
    {
        DiagnosticLine line = TrackerDiagnostics.Describe(
            new StubTracker("Stunt Jumps", TrackerStatus.Failed("unexpected entry size (96)")));

        Assert.True(line.IsProblem);
        Assert.Equal("unexpected entry size (96)", line.Text);
    }

    [Fact]
    public void FallsBackWhenAFailureCarriesNoMessage()
    {
        DiagnosticLine line = TrackerDiagnostics.Describe(
            new StubTracker("Stunt Jumps", new TrackerStatus(TrackerState.Failed, null, 0, 0)));

        Assert.True(line.IsProblem);
        Assert.Equal("unavailable", line.Text);
    }
}
