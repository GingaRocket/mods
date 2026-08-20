namespace GtaCollectiblesMap.Core.Model;

/// <summary>One labelled status line for display.</summary>
/// <param name="Name">What the line describes.</param>
/// <param name="Text">Its current state, already formatted for reading.</param>
/// <param name="IsProblem">Whether it should be shown as an error.</param>
public readonly record struct DiagnosticLine(string Name, string Text, bool IsProblem);
