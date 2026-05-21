using System.Text.RegularExpressions;

namespace Novalist.Core.Models;

/// <summary>
/// Parses / builds / strips the one-line identity comment embedded at the top of
/// every <c>.novalist</c> scene file:
/// <code>&lt;!--nv v=1 id=8f3c… --&gt;</code>
/// The comment carries the durable <see cref="SceneData.Id"/> so a scene survives
/// folder moves and filename changes when the project is edited outside Novalist.
/// It is an HTML comment so older builds (and exporters) ignore it, and it is
/// stripped before render / word-count / export.
/// </summary>
public static partial class FileFrontMatter
{
    /// <summary>Current front-matter schema version written by <see cref="Build"/>.</summary>
    public const int CurrentVersion = 1;

    // Anchored to the very start (no Multiline) so only a leading comment matches.
    // id is non-whitespace, so it stops at the space before "-->". A trailing line
    // break (CRLF / CR / LF) is consumed if present; the body keeps its own endings.
    [GeneratedRegex(@"^<!--nv v=(?<v>\d+) id=(?<id>\S+) -->(?:\r\n|\r|\n)?")]
    private static partial Regex MarkerRegex();

    /// <summary>A parsed front-matter line.</summary>
    public readonly record struct Parsed(int Version, string Id);

    /// <summary>
    /// Attempts to read the leading front-matter line. Returns false (and leaves
    /// <paramref name="parsed"/> default) for clean or malformed content.
    /// </summary>
    public static bool TryParse(string content, out Parsed parsed)
    {
        parsed = default;
        if (string.IsNullOrEmpty(content)) return false;
        var m = MarkerRegex().Match(content);
        if (!m.Success) return false;
        parsed = new Parsed(int.Parse(m.Groups["v"].Value), m.Groups["id"].Value);
        return true;
    }

    /// <summary>True if <paramref name="content"/> already starts with a front-matter line.</summary>
    public static bool HasFrontMatter(string content) => TryParse(content, out _);

    /// <summary>
    /// Returns <paramref name="content"/> with the leading front-matter line (and its
    /// trailing line break) removed. No-op for content without front-matter; the body's
    /// own line endings are preserved.
    /// </summary>
    public static string Strip(string content)
    {
        if (string.IsNullOrEmpty(content)) return content;
        return MarkerRegex().Replace(content, string.Empty);
    }

    /// <summary>Builds the front-matter line (including trailing newline) for an id.</summary>
    public static string Build(string id) => $"<!--nv v={CurrentVersion} id={id} -->\n";

    /// <summary>
    /// Prepends front-matter for <paramref name="id"/> if absent; returns content
    /// unchanged when it already carries front-matter (idempotent).
    /// </summary>
    public static string Stamp(string content, string id)
        => HasFrontMatter(content) ? content : Build(id) + content;
}
