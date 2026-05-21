using System.Text.Json;
using Novalist.Core.Models;
using Novalist.Core.Services;

namespace Novalist.Core.Tests.TestHelpers;

/// <summary>
/// Builds a pre-v3 ("v2") Novalist project on disk for migration / reconciliation
/// regression tests. Reproduces the real-data quirks from the plan: chapter folder
/// <c>NN</c> prefix gaps, scene filename gaps (a chapter with no <c>scene-01</c>),
/// archived scenes, chapters referencing act-name strings while <c>draft.json.acts</c>
/// is empty, and raw HTML scene bodies with no front-matter.
///
/// Writes through an <see cref="IFileService"/> so the same fixture drives both
/// in-memory migrator unit tests and real-disk <c>LoadProjectAsync</c> integration tests.
/// </summary>
public sealed class V2ProjectBuilder
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true,
    };

    private readonly ProjectMetadata _project = new()
    {
        Version = 2,
        Id = "project-test",
        Name = "Test Project",
    };

    private readonly Dictionary<string, DraftBuild> _drafts = new();

    private sealed class DraftBuild
    {
        public required string BookFolder;
        public required string ChapterFolderName; // book.ChapterFolder, usually "Chapters"
        public required string DraftFolder;
        public readonly BookDraftData Data = new();
        public readonly ScenesManifest Manifest = new();
        public readonly Dictionary<string, string> SceneFiles = new(); // draft-relative path -> body
    }

    private static string Key(BookData book, BookDraftMetadata draft) => $"{book.Id}/{draft.Id}";

    // Real scene ids are GUIDs (space-free). The front-matter format is space-delimited,
    // so fixture ids must be space-free too — strip spaces from the folder-derived seed.
    private static string SceneId(string folderName, string fileName)
        => $"sc-{folderName.Replace(" ", "")}-{fileName}";

    public ProjectMetadata Project => _project;

    public BookData AddBook(string name, string folder)
    {
        var book = new BookData
        {
            Id = $"book-{_project.Books.Count + 1}",
            Name = name,
            FolderName = folder,
        };
        _project.Books.Add(book);
        if (_project.Books.Count == 1) _project.ActiveBookId = book.Id;
        return book;
    }

    public BookDraftMetadata AddDraft(BookData book, string folder)
    {
        var draft = new BookDraftMetadata
        {
            Id = $"draft-{book.Drafts.Count + 1}",
            Name = $"Draft {book.Drafts.Count + 1}",
            FolderName = folder,
        };
        book.Drafts.Add(draft);
        if (book.Drafts.Count == 1) book.ActiveDraftId = draft.Id;
        _drafts[Key(book, draft)] = new DraftBuild
        {
            BookFolder = book.FolderName,
            ChapterFolderName = book.ChapterFolder,
            DraftFolder = draft.FolderName,
        };
        return draft;
    }

    public ChapterData AddChapter(BookData book, BookDraftMetadata draft, string title, string folderName, int order, string act = "", ChapterStatus status = ChapterStatus.Outline)
    {
        var chapter = new ChapterData
        {
            Guid = $"ch-{folderName}",
            Title = title,
            FolderName = folderName,
            Order = order,
            Act = act,
            Status = status,
        };
        var build = _drafts[Key(book, draft)];
        build.Data.Chapters.Add(chapter);
        build.Manifest.Chapters[chapter.Guid] = new List<SceneData>();
        return chapter;
    }

    public SceneData AddScene(BookData book, BookDraftMetadata draft, ChapterData chapter, string fileName, string body, string? id = null)
    {
        var build = _drafts[Key(book, draft)];
        var list = build.Manifest.Chapters[chapter.Guid];
        var scene = new SceneData
        {
            Id = id ?? SceneId(chapter.FolderName, fileName),
            Title = fileName,
            FileName = fileName,
            ChapterGuid = chapter.Guid,
            Order = list.Count + 1,
        };
        list.Add(scene);
        build.SceneFiles[$"{build.ChapterFolderName}/{chapter.FolderName}/{fileName}"] = body;
        return scene;
    }

    /// <summary>Adds a manifest scene entry with <b>no</b> file on disk (dangling entry).</summary>
    public SceneData AddSceneEntryOnly(BookData book, BookDraftMetadata draft, ChapterData chapter, string fileName, string? id = null)
    {
        var build = _drafts[Key(book, draft)];
        var list = build.Manifest.Chapters[chapter.Guid];
        var scene = new SceneData
        {
            Id = id ?? SceneId(chapter.FolderName, fileName),
            Title = fileName,
            FileName = fileName,
            ChapterGuid = chapter.Guid,
            Order = list.Count + 1,
        };
        list.Add(scene);
        return scene;
    }

    public SceneData AddArchivedScene(BookData book, BookDraftMetadata draft, string fileName, string body, string? id = null)
    {
        var build = _drafts[Key(book, draft)];
        var scene = new SceneData
        {
            Id = id ?? $"sc-archive-{fileName}",
            Title = fileName,
            FileName = fileName,
            ArchivedAt = DateTime.UtcNow,
            Order = build.Manifest.Archived.Count + 1,
        };
        build.Manifest.Archived.Add(scene);
        build.SceneFiles[$"{build.ChapterFolderName}/__Archive/{fileName}"] = body;
        return scene;
    }

    /// <summary>Writes the whole project under <paramref name="projectRoot"/> via <paramref name="fs"/>.</summary>
    public async Task WriteToAsync(IFileService fs, string projectRoot)
    {
        var novalistDir = fs.CombinePath(projectRoot, ".novalist");
        await fs.CreateDirectoryAsync(novalistDir);
        await fs.WriteTextAsync(fs.CombinePath(novalistDir, "project.json"), JsonSerializer.Serialize(_project, JsonOptions));

        foreach (var book in _project.Books)
        {
            var bookRoot = fs.CombinePath(projectRoot, book.FolderName);
            foreach (var draft in book.Drafts)
            {
                var build = _drafts[Key(book, draft)];
                var draftRoot = fs.CombinePath(bookRoot, "Drafts", draft.FolderName);
                await fs.CreateDirectoryAsync(draftRoot);
                await fs.WriteTextAsync(fs.CombinePath(draftRoot, "draft.json"), JsonSerializer.Serialize(build.Data, JsonOptions));
                await fs.WriteTextAsync(fs.CombinePath(draftRoot, "scenes.json"), JsonSerializer.Serialize(build.Manifest, JsonOptions));
                foreach (var (rel, body) in build.SceneFiles)
                {
                    var parts = rel.Split('/');
                    var abs = fs.CombinePath(new[] { draftRoot }.Concat(parts).ToArray());
                    await fs.WriteTextAsync(abs, body);
                }
            }
        }
    }
}
