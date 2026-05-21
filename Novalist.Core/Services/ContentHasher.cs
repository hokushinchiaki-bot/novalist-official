using System.Security.Cryptography;
using System.Text;
using Novalist.Core.Models;

namespace Novalist.Core.Services;

/// <summary>
/// Computes a stable content fingerprint for a scene file. The front-matter line is
/// stripped before hashing, so stamping a file with its identity comment does not
/// change its hash — the load-bearing invariant that lets the reconciler recognise a
/// moved file by content even after it gains an id.
/// </summary>
public static class ContentHasher
{
    /// <summary>
    /// Hashes <paramref name="content"/> after stripping any front-matter line.
    /// Returns <c>sha1:&lt;lowercase-hex&gt;</c>.
    /// </summary>
    public static string Hash(string content)
    {
        var body = FileFrontMatter.Strip(content ?? string.Empty);
        var bytes = SHA1.HashData(Encoding.UTF8.GetBytes(body));
        return "sha1:" + Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
