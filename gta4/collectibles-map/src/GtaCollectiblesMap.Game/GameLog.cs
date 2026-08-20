using System;
using System.IO;
using GtaCollectiblesMap.Core.Abstractions;
using GTA;

namespace GtaCollectiblesMap.Game;

/// <summary>Routes log output to the ScriptHookDotNet console.</summary>
public sealed class GameLog(string prefix, Func<bool> isDebugEnabled) : ILog
{
    private static readonly object FileLock = new();

    private readonly string _filePath = ResolveLogPath();

    public void Info(string message) => Write("Info", message);

    public void Warn(string message) => Write("Warning", message);

    public void Error(string message) => Write("Error", message);

    public void Debug(string message)
    {
        // Guarded rather than always emitted: the refresh loop runs roughly once a second and
        // would otherwise flood the console.
        if (isDebugEnabled())
        {
            Write("Debug", message);
        }
    }

    private void Write(string level, string message)
    {
        string formatted = Format(level, message);
        GTA.Game.Console.Print(formatted);

        try
        {
            lock (FileLock)
            {
                File.AppendAllText(
                    _filePath,
                    $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} {formatted}{Environment.NewLine}");
            }
        }
        catch
        {
            // Logging must never take the game down.
        }
    }

    private string Format(string level, string message) => $"[{prefix}/{level}] {message}";

    /// <summary>Resolves the log file beside the script, falling back to the working directory.</summary>
    /// <remarks>
    /// Guarded because this runs from a field initialiser: <c>Game.InstallFolder</c> is a call
    /// into the game, and letting it throw there would take the whole script's construction
    /// down rather than merely costing us a log file.
    /// </remarks>
    private static string ResolveLogPath()
    {
        try
        {
            return Path.Combine(GTA.Game.InstallFolder, "scripts", "GtaCollectiblesMap.log");
        }
        catch
        {
            return "GtaCollectiblesMap.log";
        }
    }
}
