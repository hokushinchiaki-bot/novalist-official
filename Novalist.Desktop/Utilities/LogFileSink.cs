using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Novalist.Desktop.Utilities;

/// <summary>
/// Thread-safe file sink for the opt-in diagnostic log. Writes daily-named,
/// size-rotated text files under %APPDATA%/Novalist/logs. Every line passed
/// here has already been run through <see cref="LogRedactor"/> by <see cref="Log"/>.
///
/// Writing never throws into callers and is best-effort: a failed write is
/// swallowed rather than risk crashing the app over diagnostics.
/// </summary>
internal sealed class LogFileSink
{
    private const long MaxBytesPerFile = 5 * 1024 * 1024; // 5 MB
    private const int MaxRetainedFiles = 5;

    private readonly object _gate = new();
    private bool _headerWritten;

    public static string DefaultDirectory => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Novalist", "logs");

    public string Directory => DefaultDirectory;

    public string CurrentLogPath =>
        Path.Combine(DefaultDirectory, $"novalist-{DateTime.Now:yyyy-MM-dd}.log");

    public void Write(string line)
    {
        try
        {
            lock (_gate)
            {
                System.IO.Directory.CreateDirectory(DefaultDirectory);

                if (!_headerWritten)
                {
                    _headerWritten = true;
                    WriteSessionHeader();
                }

                var path = CurrentLogPath;
                RotateIfNeeded(path);
                File.AppendAllText(
                    path,
                    $"{DateTime.Now:HH:mm:ss.fff} {line}{Environment.NewLine}");
            }
        }
        catch
        {
            // Diagnostics must never take the app down.
        }
    }

    /// <summary>Removes all diagnostic log files. Best-effort.</summary>
    public void Clear()
    {
        try
        {
            lock (_gate)
            {
                if (!System.IO.Directory.Exists(DefaultDirectory)) return;
                foreach (var f in System.IO.Directory.GetFiles(DefaultDirectory, "novalist-*.log"))
                {
                    try { File.Delete(f); } catch { /* skip locked */ }
                }
                _headerWritten = false;
            }
        }
        catch { /* best effort */ }
    }

    private void WriteSessionHeader()
    {
        // Allowlisted, content-free environment facts only.
        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "unknown";
        var lines = new[]
        {
            "──────────────────────────────────────────────",
            $"Novalist diagnostic log — session start {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"App version: {version}",
            $"OS: {Environment.OSVersion} ({(Environment.Is64BitOperatingSystem ? "x64" : "x86")})",
            $"Runtime: {Environment.Version} / {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}",
            $"Culture: {CultureInfo.CurrentCulture.Name}",
            "No story content is recorded in this file.",
            "──────────────────────────────────────────────",
        };
        File.AppendAllText(CurrentLogPath, string.Join(Environment.NewLine, lines) + Environment.NewLine);
    }

    private static void RotateIfNeeded(string path)
    {
        try
        {
            var info = new FileInfo(path);
            if (!info.Exists || info.Length < MaxBytesPerFile) return;

            var rolled = Path.Combine(
                DefaultDirectory,
                $"novalist-{DateTime.Now:yyyy-MM-dd-HHmmss}.log");
            File.Move(path, rolled, overwrite: true);

            Prune();
        }
        catch { /* best effort */ }
    }

    private static void Prune()
    {
        try
        {
            var files = System.IO.Directory
                .GetFiles(DefaultDirectory, "novalist-*.log")
                .OrderByDescending(f => File.GetLastWriteTimeUtc(f))
                .Skip(MaxRetainedFiles)
                .ToList();
            foreach (var f in files)
            {
                try { File.Delete(f); } catch { /* skip */ }
            }
        }
        catch { /* best effort */ }
    }
}
