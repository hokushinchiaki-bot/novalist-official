using System.Text.Json.Serialization;

namespace Novalist.Core.Models;

/// <summary>
/// Per-draft fingerprint cache stored at <c>Drafts/&lt;d&gt;/.nvindex.json</c>. Maps a
/// draft-relative scene path to its identity + content fingerprint so external
/// moves / new files / deletes can be detected on load. Rebuildable from disk, so
/// a missing or corrupt index is never fatal.
/// </summary>
public sealed class DraftIndex
{
    [JsonPropertyName("version")]
    public int Version { get; set; } = 1;

    /// <summary>Keyed by draft-relative path (e.g. <c>Chapters/01 - Arrival/scene-01.novalist</c>).</summary>
    [JsonPropertyName("entries")]
    public Dictionary<string, DraftIndexEntry> Entries { get; set; } = new();
}

/// <summary>One indexed scene file: its embedded id, content hash, and stat fast-path.</summary>
public sealed class DraftIndexEntry
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Hash of the file content with front-matter stripped (e.g. <c>sha1:ab12…</c>).</summary>
    [JsonPropertyName("hash")]
    public string Hash { get; set; } = string.Empty;

    /// <summary>File size in bytes — half of the re-hash skip fast-path.</summary>
    [JsonPropertyName("size")]
    public long Size { get; set; }

    /// <summary>Last write time (UTC) — the other half of the fast-path.</summary>
    [JsonPropertyName("mtimeUtc")]
    public DateTime MtimeUtc { get; set; }
}
