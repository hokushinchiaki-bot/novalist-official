using System;
using System.Diagnostics;

namespace Novalist.Desktop.Utilities;

/// <summary>
/// Lightweight log facade. Debug/Info go to the trace listener (visible in
/// a debugger) and additionally to stderr when NOVALIST_VERBOSE=1. Without
/// that flag, Release builds stay quiet — Debug.WriteLine is a no-op there
/// because of [Conditional("DEBUG")], so prior to this gate, every Log.Debug
/// call simply vanished in shipped binaries.
///
/// When the user opts in (Settings → Diagnostics), every line is also written
/// to a rotating text file under %APPDATA%/Novalist/logs. That file is
/// content-safe: callers must only pass structured, non-content data
/// (allowlist), and every line is additionally scrubbed by <see cref="LogRedactor"/>
/// as a backstop before it is written. See CLAUDE.md "Diagnostic log must never
/// contain story content".
/// </summary>
public static class Log
{
    private static readonly bool _verbose =
        Environment.GetEnvironmentVariable("NOVALIST_VERBOSE") == "1";

    private static LogFileSink? _sink;
    private static volatile bool _fileEnabled;

    /// <summary>Directory where diagnostic logs live (whether or not enabled).</summary>
    public static string LogDirectory => LogFileSink.DefaultDirectory;

    /// <summary>Path of the current day's diagnostic log file.</summary>
    public static string CurrentLogPath => (_sink ??= new LogFileSink()).CurrentLogPath;

    /// <summary>
    /// Turns the diagnostic file sink on or off. Live — no restart required.
    /// </summary>
    public static void EnableFileLogging(bool enabled)
    {
        if (enabled)
            _sink ??= new LogFileSink();
        _fileEnabled = enabled;
    }

    /// <summary>Deletes all diagnostic log files.</summary>
    public static void ClearLogFiles() => (_sink ??= new LogFileSink()).Clear();

    private static void ToFile(string line)
    {
        if (_fileEnabled)
            _sink?.Write(LogRedactor.Scrub(line));
    }

    public static void Debug(string message)
    {
        System.Diagnostics.Debug.WriteLine(message);
        if (_verbose) Console.Error.WriteLine(message);
        ToFile(message);
    }

    public static void Info(string message)
    {
        var line = $"[INFO] {message}";
        System.Diagnostics.Debug.WriteLine(line);
        if (_verbose) Console.Error.WriteLine(line);
        ToFile(line);
    }

    public static void Warn(string message)
    {
        var line = $"[WARN] {message}";
        System.Diagnostics.Debug.WriteLine(line);
        Console.Error.WriteLine(line);
        ToFile(line);
    }

    public static void Error(string message, Exception? ex = null)
    {
        var line = ex == null ? $"[ERROR] {message}" : $"[ERROR] {message} :: {ex}";
        System.Diagnostics.Debug.WriteLine(line);
        Console.Error.WriteLine(line);
        ToFile(line);
    }
}
