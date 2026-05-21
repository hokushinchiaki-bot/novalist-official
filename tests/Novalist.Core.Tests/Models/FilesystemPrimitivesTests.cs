using System.Text.Json;
using Novalist.Core.Models;
using Novalist.Core.Services;
using Xunit;

namespace Novalist.Core.Tests.Models;

/// <summary>
/// Phase-1 primitives for the filesystem-source-of-truth model: front-matter
/// parse/build/strip, content hashing, the draft index, and chapter markers.
/// </summary>
public class FilesystemPrimitivesTests
{
    // ── FileFrontMatter ──

    [Fact]
    public void Build_ProducesExactShape()
    {
        Assert.Equal("<!--nv v=1 id=abc-123 -->\n", FileFrontMatter.Build("abc-123"));
    }

    [Fact]
    public void Build_RoundTripsThroughParse()
    {
        var line = FileFrontMatter.Build("8f3c");
        Assert.True(FileFrontMatter.TryParse(line + "<p>body</p>", out var p));
        Assert.Equal(1, p.Version);
        Assert.Equal("8f3c", p.Id);
    }

    [Fact]
    public void TryParse_CleanContent_ReturnsFalse()
    {
        Assert.False(FileFrontMatter.TryParse("<p>no marker here</p>", out var p));
        Assert.Equal(default, p);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void TryParse_EmptyOrNull_ReturnsFalse(string? content)
    {
        Assert.False(FileFrontMatter.TryParse(content!, out _));
    }

    [Theory]
    [InlineData("<!--nv id=abc -->\n<p>x</p>")]       // missing version
    [InlineData("<!--nv v=x id=abc -->\n<p>x</p>")]   // non-numeric version
    [InlineData("<!--nv v=1 -->\n<p>x</p>")]          // missing id
    [InlineData("<!--nv v=1 id=abc --\n<p>x</p>")]    // truncated comment
    public void TryParse_Malformed_TreatedAsAbsent(string content)
    {
        Assert.False(FileFrontMatter.TryParse(content, out _));
        Assert.Equal(content, FileFrontMatter.Strip(content)); // strip is a no-op
    }

    [Fact]
    public void Strip_RemovesOnlyMarkerLine_PreservesBody()
    {
        var body = "<p>First</p>\n<p>Second</p>";
        var stamped = FileFrontMatter.Build("id1") + body;
        Assert.Equal(body, FileFrontMatter.Strip(stamped));
    }

    [Fact]
    public void Strip_LeavesNonNvLeadingComment_Intact()
    {
        var content = "<!-- editor note -->\n<p>x</p>";
        Assert.Equal(content, FileFrontMatter.Strip(content));
    }

    [Fact]
    public void Strip_CleanContent_IsNoOp()
    {
        Assert.Equal("<p>x</p>", FileFrontMatter.Strip("<p>x</p>"));
        Assert.Equal("", FileFrontMatter.Strip(""));
    }

    [Theory]
    [InlineData("<!--nv v=1 id=abc -->\r\n<p>x</p>", "<p>x</p>")] // CRLF after marker
    [InlineData("<!--nv v=1 id=abc -->\r<p>x</p>", "<p>x</p>")]   // CR after marker
    [InlineData("<!--nv v=1 id=abc -->\n<p>x</p>", "<p>x</p>")]   // LF after marker
    public void Strip_HandlesAllLineEndings(string content, string expected)
    {
        Assert.True(FileFrontMatter.TryParse(content, out _));
        Assert.Equal(expected, FileFrontMatter.Strip(content));
    }

    [Fact]
    public void Strip_PreservesBodyCrlf()
    {
        var stamped = FileFrontMatter.Build("id") + "<p>a</p>\r\n<p>b</p>";
        Assert.Equal("<p>a</p>\r\n<p>b</p>", FileFrontMatter.Strip(stamped));
    }

    [Fact]
    public void Stamp_PrependsWhenAbsent()
    {
        var stamped = FileFrontMatter.Stamp("<p>x</p>", "id1");
        Assert.True(FileFrontMatter.TryParse(stamped, out var p));
        Assert.Equal("id1", p.Id);
        Assert.Equal("<p>x</p>", FileFrontMatter.Strip(stamped));
    }

    [Fact]
    public void Stamp_AlreadyStamped_IsIdempotent()
    {
        var once = FileFrontMatter.Stamp("<p>x</p>", "id1");
        var twice = FileFrontMatter.Stamp(once, "id2"); // different id ignored
        Assert.Equal(once, twice);
    }

    [Fact]
    public void Stamp_PrependsAboveNonNvComment()
    {
        var content = "<!-- note -->\n<p>x</p>";
        var stamped = FileFrontMatter.Stamp(content, "id1");
        Assert.True(FileFrontMatter.TryParse(stamped, out _));
        Assert.Equal(content, FileFrontMatter.Strip(stamped));
    }

    [Fact]
    public void HasFrontMatter_Reflects_TryParse()
    {
        Assert.True(FileFrontMatter.HasFrontMatter(FileFrontMatter.Build("z")));
        Assert.False(FileFrontMatter.HasFrontMatter("<p>x</p>"));
    }

    // ── ContentHasher ──

    [Fact]
    public void Hash_IsInvariantToStamping()
    {
        var raw = "<p>The body text.</p>";
        var stamped = FileFrontMatter.Stamp(raw, "any-id");
        Assert.Equal(ContentHasher.Hash(raw), ContentHasher.Hash(stamped));
    }

    [Fact]
    public void Hash_SameContent_SameHash()
    {
        Assert.Equal(ContentHasher.Hash("<p>x</p>"), ContentHasher.Hash("<p>x</p>"));
    }

    [Fact]
    public void Hash_DifferentContent_DifferentHash()
    {
        Assert.NotEqual(ContentHasher.Hash("<p>x</p>"), ContentHasher.Hash("<p>y</p>"));
    }

    [Fact]
    public void Hash_EmptyContent_DoesNotThrow()
    {
        var h = ContentHasher.Hash("");
        Assert.StartsWith("sha1:", h);
        Assert.Equal(ContentHasher.Hash(""), ContentHasher.Hash(null!));
    }

    [Fact]
    public void Hash_HasExpectedShape()
    {
        var h = ContentHasher.Hash("<p>x</p>");
        Assert.StartsWith("sha1:", h);
        Assert.Equal("sha1:".Length + 40, h.Length); // 20-byte SHA1 => 40 hex chars
        Assert.Equal(h, h.ToLowerInvariant());
    }

    // ── DraftIndex ──

    [Fact]
    public void DraftIndex_RoundTrips()
    {
        var index = new DraftIndex();
        index.Entries["Chapters/01 - A/scene-01.novalist"] = new DraftIndexEntry
        {
            Id = "id1",
            Hash = "sha1:ab12",
            Size = 1423,
            MtimeUtc = new DateTime(2026, 5, 20, 10, 0, 0, DateTimeKind.Utc),
        };

        var json = JsonSerializer.Serialize(index);
        var back = JsonSerializer.Deserialize<DraftIndex>(json)!;

        Assert.Equal(1, back.Version);
        var e = back.Entries["Chapters/01 - A/scene-01.novalist"];
        Assert.Equal("id1", e.Id);
        Assert.Equal("sha1:ab12", e.Hash);
        Assert.Equal(1423, e.Size);
        Assert.Equal(index.Entries.Values.Single().MtimeUtc, e.MtimeUtc);
    }

    [Fact]
    public void DraftIndex_ToleratesUnknownFields()
    {
        var json = """{"version":1,"entries":{},"futureField":42}""";
        var back = JsonSerializer.Deserialize<DraftIndex>(json)!;
        Assert.Equal(1, back.Version);
        Assert.Empty(back.Entries);
    }

    // ── ChapterMarker ──

    [Fact]
    public void ChapterMarker_FromChapter_CopiesMetadata()
    {
        var chapter = new ChapterData
        {
            Guid = "g1",
            Title = "Arrival",
            Act = "Act I",
            Order = 3,
            Status = ChapterStatus.FirstDraft,
            Date = "2026-01-01",
            FolderName = "03 - Arrival",
        };

        var marker = ChapterMarker.FromChapter(chapter);
        Assert.Equal("g1", marker.Guid);
        Assert.Equal("Arrival", marker.Title);
        Assert.Equal("Act I", marker.Act);
        Assert.Equal(3, marker.Order);
        Assert.Equal(ChapterStatus.FirstDraft, marker.Status);
        Assert.Equal("2026-01-01", marker.Date);
    }

    [Fact]
    public void ChapterMarker_ToChapter_RestoresWithFolderName()
    {
        var marker = new ChapterMarker
        {
            Guid = "g2",
            Title = "End",
            Act = "Act III",
            Order = 9,
            Status = ChapterStatus.Final,
            Date = "x",
        };

        var chapter = marker.ToChapter("25 - End");
        Assert.Equal("g2", chapter.Guid);
        Assert.Equal("End", chapter.Title);
        Assert.Equal("Act III", chapter.Act);
        Assert.Equal(9, chapter.Order);
        Assert.Equal(ChapterStatus.Final, chapter.Status);
        Assert.Equal("x", chapter.Date);
        Assert.Equal("25 - End", chapter.FolderName);
    }

    [Fact]
    public void ChapterMarker_RoundTripsThroughJson_StatusAsString()
    {
        var marker = ChapterMarker.FromChapter(new ChapterData { Status = ChapterStatus.Revised });
        var json = JsonSerializer.Serialize(marker);
        Assert.Contains("\"Revised\"", json); // enum serialized as string name
        var back = JsonSerializer.Deserialize<ChapterMarker>(json)!;
        Assert.Equal(ChapterStatus.Revised, back.Status);
    }
}
