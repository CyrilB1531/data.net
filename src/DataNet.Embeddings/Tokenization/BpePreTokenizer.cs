using System.Text;
using System.Text.RegularExpressions;
using DataNet.Internal;

namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// Splits text into the pieces the merge loop runs over, independently.
/// </summary>
/// <remarks>
/// <para>
/// A byte-level model declares the pattern it was trained with; the classic
/// lineage splits on whitespace instead. The split is not cosmetic — a merge
/// can never cross a piece boundary, so it decides which tokens are reachable
/// at all.
/// </para>
/// <para>
/// The pattern reaches here from a model file, so it is caller-supplied in every
/// sense that matters. It is compiled with <see cref="RegexDefaults.MatchTimeout"/>,
/// which turns unbounded backtracking into an exception instead of a hung thread.
/// </para>
/// <para>
/// A declared pattern also means the model is byte-level (HuggingFace's own
/// <c>ByteLevel</c> pre-tokenizer does the regex split and the byte-to-character
/// mapping as one stage), so each match is re-encoded through
/// <see cref="ByteLevelAlphabet"/> before it becomes a piece — that mapping is
/// what lets the merge loop address arbitrary bytes through a character-keyed
/// vocabulary. The classic, whitespace-only path has no such vocabulary and
/// stays untouched.
/// </para>
/// </remarks>
internal sealed class BpePreTokenizer
{
    private static readonly Regex Whitespace =
        new(@"\S+", RegexOptions.Compiled | RegexOptions.CultureInvariant, RegexDefaults.MatchTimeout);

    private readonly Regex _pattern;
    private readonly bool _byteLevel;

    // RegexOptions.Compiled is deliberately not used here: compiling costs
    // milliseconds per distinct pattern, and a tokenizer is built once per model,
    // so that cost would be paid on a path that runs once.
    public BpePreTokenizer(string? pattern)
    {
        _byteLevel = pattern is not null;
        _pattern = pattern is null
            ? Whitespace
            : new Regex(pattern, RegexOptions.CultureInvariant, RegexDefaults.MatchTimeout);
    }

    /// <summary>Appends the pieces of <paramref name="text"/> to <paramref name="pieces"/>.</summary>
    public void Split(string text, List<string> pieces)
    {
        IEnumerable<Match> matches = _pattern.Matches(text).Cast<Match>();
        pieces.AddRange(_byteLevel ? matches.Select(m => ByteLevelEncode(m.Value)) : matches.Select(m => m.Value));
    }

    // Each match is UTF-8 bytes waiting to happen: encode it, then stand each
    // byte in for the printable character ByteLevelAlphabet assigns it. A piece
    // therefore grows when it holds multi-byte characters -- "é" contributes two
    // mapped characters, not one -- which is exactly how the byte-level
    // vocabulary was trained.
    private static string ByteLevelEncode(string piece)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(piece);
        var mapped = new char[bytes.Length];
        for (int i = 0; i < bytes.Length; i++)
        {
            mapped[i] = ByteLevelAlphabet.ToChar(bytes[i]);
        }
        return new string(mapped);
    }
}
