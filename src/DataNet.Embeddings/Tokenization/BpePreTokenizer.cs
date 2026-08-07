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
/// </remarks>
internal sealed class BpePreTokenizer
{
    private static readonly Regex Whitespace =
        new(@"\S+", RegexOptions.Compiled | RegexOptions.CultureInvariant, RegexDefaults.MatchTimeout);

    private readonly Regex _pattern;

    // RegexOptions.Compiled is deliberately not used here: compiling costs
    // milliseconds per distinct pattern, and a tokenizer is built once per model,
    // so that cost would be paid on a path that runs once.
    public BpePreTokenizer(string? pattern) =>
        _pattern = pattern is null
            ? Whitespace
            : new Regex(pattern, RegexOptions.CultureInvariant, RegexDefaults.MatchTimeout);

    /// <summary>Appends the pieces of <paramref name="text"/> to <paramref name="pieces"/>.</summary>
    public void Split(string text, List<string> pieces)
    {
        IEnumerable<Match> matches = _pattern.Matches(text).Cast<Match>();
        pieces.AddRange(matches.Select(m => m.Value));
    }
}
