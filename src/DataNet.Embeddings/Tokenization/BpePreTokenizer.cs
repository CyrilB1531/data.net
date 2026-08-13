using System.Text.RegularExpressions;
using DataNet.Internal;

namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// Splits text into the pieces the merge loop runs over, independently.
/// </summary>
/// <remarks>
/// <para>
/// A byte-level model declares either one pattern or two. A bare
/// <c>ByteLevel</c> step declares one, and the classic lineage declares none,
/// splitting on word boundaries instead, isolating punctuation from letters
/// and digits (HuggingFace's <c>Whitespace</c> pre-tokenizer type,
/// <c>\w+|[^\w\s]+</c>). A <c>Sequence</c> of <c>Split</c> then <c>ByteLevel</c>
/// declares two: the <c>Split</c> step's pattern runs first, and then
/// <c>ByteLevel</c>'s own pattern re-splits every piece the first pass
/// produced, unless the file turns its <c>use_regex</c> off. Measured against
/// <c>tokenizers</c> 0.23.1; see issue #143. The split is not cosmetic — a
/// merge can never cross a piece boundary, so it decides which tokens are
/// reachable at all, and it is what puts an end-of-word suffix on
/// <c>world</c> rather than on <c>world!</c>.
/// </para>
/// <para>
/// Both patterns reach here from a model file, so they are caller-supplied in
/// every sense that matters. Each is compiled with
/// <see cref="RegexDefaults.MatchTimeout"/>, which turns unbounded backtracking
/// into an exception instead of a hung thread.
/// </para>
/// </remarks>
internal sealed class BpePreTokenizer
{
    // HuggingFace's "Whitespace" pre-tokenizer type -- what the classic (non-byte-level)
    // lineage declares -- splits on word boundaries, separating punctuation from
    // letters/digits, rather than merely collapsing whitespace runs (that is a
    // different type, "WhitespaceSplit", equivalent to \S+). A model whose last
    // word character is followed by punctuation, e.g. "world!", needs the split so
    // the end-of-word suffix lands on "world", not on "world!".
    private static readonly Regex Whitespace =
        new(@"\w+|[^\w\s]+", RegexOptions.Compiled | RegexOptions.CultureInvariant, RegexDefaults.MatchTimeout);

    private readonly Regex _first;
    private readonly Regex? _second;

    // RegexOptions.Compiled is deliberately not used here: compiling costs
    // milliseconds per distinct pattern, and a tokenizer is built once per model,
    // so that cost would be paid on a path that runs once.
    public BpePreTokenizer(string? preSplitPattern, string? pattern)
    {
        // Both null is the classic Whitespace split. Otherwise the non-null
        // patterns run in order, the pre-split first -- which is what a
        // Sequence of Split then ByteLevel does: ByteLevel re-splits every
        // piece the Split step produced, on its own pattern, unless use_regex
        // is off. Measured against tokenizers 0.23.1; see issue #143.
        if (preSplitPattern is null)
        {
            _first = pattern is null ? Whitespace : Compile(pattern);
            _second = null;
        }
        else
        {
            _first = Compile(preSplitPattern);
            _second = pattern is null ? null : Compile(pattern);
        }
    }

    private static Regex Compile(string pattern) =>
        new(pattern, RegexOptions.CultureInvariant, RegexDefaults.MatchTimeout);

    /// <summary>Appends the pieces of <paramref name="text"/> to <paramref name="pieces"/>.</summary>
    public void Split(string text, List<string> pieces)
    {
        if (_second is null)
        {
            Apply(_first, text, pieces);
            return;
        }

        // The second pattern runs over the pieces the first produced, on raw
        // text -- the byte mapping happens later, per final piece, in
        // BpeTokenizer. A local list rather than a field: this type is used
        // from a tokenizer documented as thread-safe after construction.
        List<string> staged = [];
        Apply(_first, text, staged);
        foreach (string piece in staged)
        {
            Apply(_second, piece, pieces);
        }
    }

    private static void Apply(Regex pattern, string text, List<string> pieces)
    {
        IEnumerable<Match> matches = pattern.Matches(text).Cast<Match>();
        pieces.AddRange(matches.Select(m => m.Value));
    }
}
