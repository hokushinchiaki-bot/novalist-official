using Novalist.Core.Models;

namespace Novalist.Core.Services;

/// <summary>
/// Filesystem-watch logic, isolated from the native <c>FileSystemWatcher</c> so it can be
/// unit-tested without real OS events or timers. Responsibilities:
/// <list type="bullet">
///   <item>filter raw paths to the ones that matter (scene files + chapter markers);</item>
///   <item>suppress the app's own writes so a save doesn't trigger a reconcile loop;</item>
///   <item>coalesce a burst of events into a single reconcile via an injected debounce;</item>
///   <item>dispatch the reconcile, never letting an exception escape (the session must live).</item>
/// </list>
/// The owning <see cref="DraftWatchService"/> supplies the debounce timer + reconcile.
/// </summary>
public sealed class DraftWatchCoordinator
{
    private readonly Func<Task> _reconcile;
    private readonly Action _scheduleFlush;
    private readonly object _gate = new();
    private readonly HashSet<string> _suppressed = new(StringComparer.OrdinalIgnoreCase);
    private bool _dirty;

    /// <param name="reconcile">Runs the actual reconcile pass (marshalled to the UI thread by the caller).</param>
    /// <param name="scheduleFlush">Arms / re-arms the debounce window; when it elapses the owner calls <see cref="FlushAsync"/>.</param>
    public DraftWatchCoordinator(Func<Task> reconcile, Action scheduleFlush)
    {
        _reconcile = reconcile;
        _scheduleFlush = scheduleFlush;
    }

    /// <summary>True if <paramref name="path"/> is a file the reconciler cares about.</summary>
    public static bool IsRelevant(string path)
    {
        var name = System.IO.Path.GetFileName(path);
        return name.EndsWith(".novalist", StringComparison.OrdinalIgnoreCase)
            || string.Equals(name, ChapterMarker.FileName, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Registers a path the app is about to write, so the resulting event is ignored once.</summary>
    public void Suppress(string path)
    {
        lock (_gate) _suppressed.Add(path);
    }

    /// <summary>
    /// Ingests a raw filesystem event. Irrelevant paths and suppressed self-writes are dropped;
    /// anything else marks the draft dirty and (re-)arms the debounce window.
    /// </summary>
    public void NotifyChange(string path)
    {
        if (!IsRelevant(path)) return;
        lock (_gate)
        {
            if (_suppressed.Remove(path)) return; // our own write — consume + ignore
            _dirty = true;
        }
        _scheduleFlush();
    }

    /// <summary>
    /// Called when the debounce window elapses. Reconciles once if anything changed since the
    /// last flush; coalesces a whole burst into this single pass. Swallows reconcile errors.
    /// </summary>
    public async Task FlushAsync()
    {
        lock (_gate)
        {
            if (!_dirty) return;
            _dirty = false;
        }

        try
        {
            await _reconcile();
        }
        catch
        {
            // A failed reconcile must never tear down the watch session.
        }
    }
}
