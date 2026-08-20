using System;
using System.Collections.Generic;
using GtaCollectiblesMap.Core.Config;
using GtaCollectiblesMap.Core.Model;

namespace GtaCollectiblesMap.Ui;

/// <summary>
/// Everything the settings panel is allowed to see. The only type that crosses the UI
/// boundary, and deliberately free of any game-UI or toolkit type so the implementation behind
/// <see cref="ISettingsUi"/> can be replaced without touching anything else.
/// </summary>
public sealed class SettingsViewModel
{
    public required ModSettings Settings { get; init; }

    public required IReadOnlyList<DiagnosticLine> Diagnostics { get; init; }

    /// <summary>
    /// Called by the UI after it edits a value.
    /// </summary>
    /// <remarks>
    /// Only records that something changed — the actual write is debounced, because the panel
    /// redraws every frame and a slider being dragged reports a change on each one.
    /// </remarks>
    public required Action NotifyChanged { get; init; }
}
