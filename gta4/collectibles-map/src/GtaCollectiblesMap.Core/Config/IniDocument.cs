using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace GtaCollectiblesMap.Core.Config;

/// <summary>
/// A deliberately small flat INI reader/writer.
/// </summary>
/// <remarks>
/// Hand-rolled rather than taken from a package because Core must stay dependency-free —
/// every runtime dependency has to be deployed next to the script in the game folder and
/// becomes a load-failure risk. Comments and unknown keys are preserved on save so a
/// hand-edited file is not clobbered by the settings panel writing one value back.
/// </remarks>
public sealed class IniDocument
{
    private readonly Dictionary<string, string> _values =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly List<string> _lines = [];

    public static IniDocument Parse(string text)
    {
        IniDocument doc = new();

        foreach (string raw in text.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            doc._lines.Add(line);

            string trimmed = line.Trim();
            if (trimmed.Length == 0 || trimmed[0] is ';' or '#' or '[')
            {
                continue;
            }

            int eq = trimmed.IndexOf('=');
            if (eq <= 0)
            {
                continue;
            }

            string key = trimmed.Substring(0, eq).Trim();
            string value = trimmed.Substring(eq + 1).Trim();
            doc._values[key] = value;
        }

        return doc;
    }

    public static IniDocument Load(string path) =>
        File.Exists(path) ? Parse(File.ReadAllText(path)) : new IniDocument();

    public bool GetBool(string key, bool fallback) =>
        _values.TryGetValue(key, out string? raw) && bool.TryParse(raw, out bool parsed)
            ? parsed
            : fallback;

    public int GetInt(string key, int fallback) =>
        _values.TryGetValue(key, out string? raw)
        && int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
            ? parsed
            : fallback;

    public float GetFloat(string key, float fallback) =>
        _values.TryGetValue(key, out string? raw)
        && float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsed)
            ? parsed
            : fallback;

    public TEnum GetEnum<TEnum>(string key, TEnum fallback)
        where TEnum : struct, Enum =>
        _values.TryGetValue(key, out string? raw) && Enum.TryParse(raw, true, out TEnum parsed)
            ? parsed
            : fallback;

    public void Set(string key, string value) => _values[key] = value;

    public void Set(string key, bool value) => Set(key, value ? "true" : "false");

    public void Set(string key, int value) => Set(key, value.ToString(CultureInfo.InvariantCulture));

    public void Set(string key, float value) => Set(key, value.ToString("0.###", CultureInfo.InvariantCulture));

    public void SetEnum<TEnum>(string key, TEnum value)
        where TEnum : struct, Enum => Set(key, value.ToString()!);

    /// <summary>
    /// Renders the file, rewriting the values of keys that already had a line and appending
    /// any that did not. Layout and comments of the original survive.
    /// </summary>
    public string Render()
    {
        HashSet<string> written = new(StringComparer.OrdinalIgnoreCase);
        StringBuilder sb = new();

        foreach (string line in _lines)
        {
            string trimmed = line.Trim();
            int eq = trimmed.IndexOf('=');

            if (trimmed.Length == 0 || trimmed[0] is ';' or '#' or '[' || eq <= 0)
            {
                sb.AppendLine(line);
                continue;
            }

            string key = trimmed.Substring(0, eq).Trim();
            if (_values.TryGetValue(key, out string? value) && written.Add(key))
            {
                sb.AppendLine($"{key} = {value}");
            }
            else
            {
                sb.AppendLine(line);
            }
        }

        foreach (KeyValuePair<string, string> pair in _values)
        {
            if (written.Add(pair.Key))
            {
                sb.AppendLine($"{pair.Key} = {pair.Value}");
            }
        }

        return sb.ToString();
    }

    public void Save(string path) => File.WriteAllText(path, Render());
}
