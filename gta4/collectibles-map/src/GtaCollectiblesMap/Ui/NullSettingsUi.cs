namespace GtaCollectiblesMap.Ui;

/// <summary>
/// A settings panel that does nothing.
/// </summary>
/// <remarks>
/// Exists so the mod stays fully functional with no UI at all — useful when a UI toolkit
/// misbehaves, and it keeps the abstraction honest by proving nothing outside the Ui folder
/// depends on a panel existing.
/// </remarks>
public sealed class NullSettingsUi : ISettingsUi
{
    public bool IsOpen => false;

    public void Toggle() { }

    public void Close() { }

    public void Render(SettingsViewModel model) { }
}
