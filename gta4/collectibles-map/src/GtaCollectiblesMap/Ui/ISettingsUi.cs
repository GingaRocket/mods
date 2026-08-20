namespace GtaCollectiblesMap.Ui;

/// <summary>
/// The in-game settings panel.
/// </summary>
/// <remarks>
/// Kept behind an interface so the presentation toolkit is a swappable decision. The current
/// implementation uses the forms support built into ScriptHookDotNet.
/// </remarks>
public interface ISettingsUi
{
    bool IsOpen { get; }

    void Toggle();

    void Close();

    /// <summary>Synchronises the live panel. Called once per script tick.</summary>
    void Render(SettingsViewModel model);
}
