namespace GtaCollectiblesMap.Core.Abstractions;

/// <summary>
/// Source of elapsed time, so scheduling can be tested without waiting in real time.
/// </summary>
/// <remarks>
/// Deliberately mirrors <c>Environment.TickCount</c>: a 32-bit millisecond counter that wraps
/// roughly every 49 days. Callers must compare with unchecked subtraction rather than
/// <c>&gt;</c>, and modelling it as <see cref="int"/> keeps that requirement visible instead of
/// hiding it behind a type that never wraps.
/// </remarks>
public interface IClock
{
    int TickCount { get; }
}
