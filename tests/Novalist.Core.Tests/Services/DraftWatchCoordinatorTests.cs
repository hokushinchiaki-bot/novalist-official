using Novalist.Core.Models;
using Novalist.Core.Services;
using Xunit;

namespace Novalist.Core.Tests.Services;

/// <summary>
/// Live-watch decision logic: relevance filtering, self-write suppression, burst coalescing,
/// and never letting a reconcile error escape. The native FileSystemWatcher + timer wrapper
/// (<see cref="DraftWatchService"/>) is excluded from coverage; this is where the behaviour lives.
/// </summary>
public sealed class DraftWatchCoordinatorTests
{
    private int _reconciles;
    private int _schedules;

    private DraftWatchCoordinator Make(Func<Task>? reconcile = null)
        => new(
            reconcile ?? (() => { _reconciles++; return Task.CompletedTask; }),
            () => _schedules++);

    [Theory]
    [InlineData("Chapters/01 - A/scene-01.novalist", true)]
    [InlineData("Chapters/01 - A/.nvchapter.json", true)]
    [InlineData("Drafts/default/draft.json", false)]
    [InlineData("Drafts/default/scenes.json", false)]
    [InlineData("Drafts/default/.nvindex.json", false)]
    [InlineData("Chapters/01 - A/notes.txt", false)]
    public void IsRelevant_FiltersToSceneAndMarkerFiles(string path, bool expected)
    {
        Assert.Equal(expected, DraftWatchCoordinator.IsRelevant(path));
    }

    [Fact]
    public async Task RelevantChange_SchedulesAndReconciles()
    {
        var c = Make();
        c.NotifyChange("Chapters/01/scene-01.novalist");
        Assert.Equal(1, _schedules);
        await c.FlushAsync();
        Assert.Equal(1, _reconciles);
    }

    [Fact]
    public async Task IrrelevantChange_Ignored()
    {
        var c = Make();
        c.NotifyChange("Chapters/01/notes.txt");
        Assert.Equal(0, _schedules);
        await c.FlushAsync();
        Assert.Equal(0, _reconciles);
    }

    [Fact]
    public async Task SuppressedSelfWrite_Ignored_ThenConsumed()
    {
        var c = Make();
        const string path = "Chapters/01/scene-01.novalist";
        c.Suppress(path);
        c.NotifyChange(path);             // our own write — swallowed
        Assert.Equal(0, _schedules);

        c.NotifyChange(path);             // suppression already consumed — now counts
        Assert.Equal(1, _schedules);
        await c.FlushAsync();
        Assert.Equal(1, _reconciles);
    }

    [Fact]
    public async Task Burst_CoalescesToSingleReconcile()
    {
        var c = Make();
        c.NotifyChange("Chapters/01/scene-01.novalist");
        c.NotifyChange("Chapters/01/scene-02.novalist");
        c.NotifyChange("Chapters/02/scene-01.novalist");
        Assert.Equal(3, _schedules);      // timer re-armed each event

        await c.FlushAsync();             // ...but one flush
        Assert.Equal(1, _reconciles);
    }

    [Fact]
    public async Task Flush_WhenNotDirty_DoesNothing()
    {
        var c = Make();
        await c.FlushAsync();
        Assert.Equal(0, _reconciles);
    }

    [Fact]
    public async Task Flush_ConsumesDirty_SecondFlushNoOp_UntilNewChange()
    {
        var c = Make();
        c.NotifyChange("Chapters/01/scene-01.novalist");
        await c.FlushAsync();
        await c.FlushAsync();             // nothing new
        Assert.Equal(1, _reconciles);

        c.NotifyChange("Chapters/01/scene-01.novalist");
        await c.FlushAsync();
        Assert.Equal(2, _reconciles);
    }

    [Fact]
    public async Task Flush_SwallowsReconcileError()
    {
        var c = Make(() => throw new InvalidOperationException("boom"));
        c.NotifyChange("Chapters/01/scene-01.novalist");
        await c.FlushAsync();             // must not throw

        // Dirty was cleared even though reconcile threw; a fresh change still flushes.
        var threw = false;
        try { await c.FlushAsync(); } catch { threw = true; }
        Assert.False(threw);
    }
}
