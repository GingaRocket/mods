using GtaCollectiblesMap.Core.Config;

namespace GtaCollectiblesMap.Core.Abstractions;

/// <summary>Loads and persists user settings.</summary>
/// <remarks>
/// A port rather than a static helper so persistence can be substituted — both for tests and
/// if the backing format ever changes.
/// </remarks>
public interface ISettingsStore
{
    ModSettings Load();

    void Save(ModSettings settings);
}
