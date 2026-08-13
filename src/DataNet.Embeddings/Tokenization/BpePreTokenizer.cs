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

    private const int NoOpenPiece = -1;

    /// <summary>A behaviour and its invert flag, bundled so <see cref="Step"/> and
    /// <see cref="Close"/> take one parameter instead of two -- both sit at the
    /// S107 parameter-count limit otherwise.</summary>
    private readonly record struct SplitRule(SplitBehavior Behavior, bool Invert);

    /// <summary>A run of text as a position and a length, never yet substringed.</summary>
    /// <remarks>Named <c>Run</c>, not <c>Span</c>, so it does not shadow <see cref="System.Span{T}"/> in a
    /// package that leans on spans elsewhere.</remarks>
    private readonly record struct Run(int Start, int Length);

    private readonly Regex _first;
    private readonly Regex? _second;
    private readonly SplitRule _rule;

    // RegexOptions.Compiled is deliberately not used here: compiling costs
    // milliseconds per distinct pattern, and a tokenizer is built once per model,
    // so that cost would be paid on a path that runs once.
    public BpePreTokenizer(BpeSplitStep? preSplit, string? pattern)
    {
        // Both absent is the classic Whitespace split. Otherwise the pre-split
        // runs first and the second pattern re-splits its pieces (issue #143).
        // Only the pre-split carries a declared behaviour; a null pre-split still
        // needs one to drive Apply, and it is Removed with invert on -- the same
        // "keep the regex matches, drop everything else" rule the bridge in
        // BpeTokenizer states for the pre-split case (issue #145), not Isolated.
        // The two are interchangeable only when the pattern never leaves a gap,
        // which is true of every shipped byte-level pattern (Gpt2/Llama3/Qwen2)
        // but false of Whitespace's own \w+|[^\w\s]+, which never matches a run
        // of whitespace: under Isolated that whitespace would surface as its own
        // piece and reach the merge loop as an uncovered symbol, where measured
        // (bpe.json, e.g. " leading space") the reference produces no such piece
        // and no substituted token for one. Removed+invert keeps that path
        // byte-for-byte unchanged; BpeSplitBehaviorTests never exercises this
        // branch at all, since every corpus case supplies its own BpeSplitStep.
        if (preSplit is null)
        {
            _first = pattern is null ? Whitespace : Compile(pattern);
            _second = null;
            _rule = new SplitRule(SplitBehavior.Removed, Invert: true);
        }
        else
        {
            _first = Compile(preSplit.Pattern);
            _second = pattern is null ? null : Compile(pattern);
            _rule = new SplitRule(preSplit.Behavior, preSplit.Invert);
        }
    }

    private static Regex Compile(string pattern) =>
        new(pattern, RegexOptions.CultureInvariant, RegexDefaults.MatchTimeout);

    /// <summary>Appends the pieces of <paramref name="text"/> to <paramref name="pieces"/>.</summary>
    public void Split(string text, List<string> pieces)
    {
        if (_second is null)
        {
            Apply(_first, _rule, text, pieces);
            return;
        }

        // The second pattern runs over the pieces the first produced, on raw
        // text -- the byte mapping happens later, per final piece, in
        // BpeTokenizer. It always runs Isolated, invert off: the ByteLevel
        // step's own pattern has no behavior field in the tokenizer.json
        // format, and its arrangement is Isolated. A local list rather than a
        // field: this type is used from a tokenizer documented as thread-safe
        // after construction.
        List<string> staged = [];
        Apply(_first, _rule, text, staged);
        var isolated = new SplitRule(SplitBehavior.Isolated, Invert: false);
        foreach (string piece in staged)
        {
            Apply(_second, isolated, piece, pieces);
        }
    }

    /// <summary>Appends one piece, unless it is empty.</summary>
    /// <remarks>
    /// Empty pieces are dropped -- measured, an empty input yields nothing and a
    /// text the pattern covers entirely emits no gap under
    /// <see cref="SplitBehavior.Removed"/>. Nothing is allocated for one: with a
    /// pattern that matches every character, which is what every shipped model
    /// declares, this is one comparison per match and no string at all.
    /// </remarks>
    private static void Emit(string text, int start, int length, List<string> pieces)
    {
        if (length > 0)
        {
            pieces.Add(text.Substring(start, length));
        }
    }

    /// <summary>Emits a <see cref="Run"/> the same way <see cref="Emit(string, int, int, List{string})"/> does.</summary>
    private static void Emit(string text, Run run, List<string> pieces) =>
        Emit(text, run.Start, run.Length, pieces);

    /// <summary>
    /// Splits <paramref name="text"/> into alternating gaps and matches, swaps the
    /// two roles where <paramref name="rule"/>'s invert flag asks, and recombines
    /// them the way its behaviour says.
    /// </summary>
    /// <remarks>
    /// One model produces all ten combinations of the five behaviours and the flag,
    /// which is why there is no ten-way switch here. Measured against
    /// <c>tokenizers</c> 0.23.1; the grid is in issue #145.
    /// </remarks>
    private static void Apply(Regex pattern, SplitRule rule, string text, List<string> pieces)
    {
        int cursor = 0;
        int carried = NoOpenPiece;   // start of a piece still open, or NoOpenPiece
        // Not .Cast<Match>(): MatchCollection's own enumerator binds directly
        // to Match on both target frameworks, and .Cast<Match>() allocates an
        // iterator on a path this task claims the allocation budget of.
        foreach (Match match in pattern.Matches(text))
        {
            carried = Step(text, cursor, match.Index, match.Length, rule, carried, pieces);
            cursor = match.Index + match.Length;
        }
        Close(text, cursor, rule, carried, pieces);
    }

    /// <summary>
    /// Handles one regex match: the gap that precedes it, at
    /// <c>[cursor, matchStart)</c>, and the match itself, at
    /// <c>[matchStart, matchStart + matchLength)</c>. Returns the position a piece
    /// is still open at, for <see cref="Close"/> or the next call, or
    /// <see cref="NoOpenPiece"/>.
    /// </summary>
    private static int Step(
        string text, int cursor, int matchStart, int matchLength, SplitRule rule, int carried, List<string> pieces)
    {
        var gap = new Run(cursor, matchStart - cursor);
        return rule.Behavior switch
        {
            SplitBehavior.Isolated => StepIsolated(text, gap, matchStart, matchLength, pieces),
            SplitBehavior.Contiguous => StepContiguous(text, gap, matchStart, carried, pieces),
            SplitBehavior.Removed => StepRemoved(text, gap, matchStart, matchLength, rule.Invert, pieces),
            SplitBehavior.MergedWithPrevious or SplitBehavior.MergedWithNext =>
                StepMerged(text, gap, matchStart, matchLength, rule, carried, pieces),
            _ => throw new ArgumentOutOfRangeException(
                nameof(rule), rule.Behavior, "Not one of the five Split behaviours."),
        };
    }

    /// <summary>Every gap and every match, each its own piece, in text order.</summary>
    private static int StepIsolated(string text, Run gap, int matchStart, int matchLength, List<string> pieces)
    {
        Emit(text, gap, pieces);
        Emit(text, matchStart, matchLength, pieces);
        return NoOpenPiece;
    }

    /// <summary>
    /// Like <see cref="StepIsolated"/>, except a match with no gap before it joins
    /// the still-open run instead of starting its own piece.
    /// </summary>
    private static int StepContiguous(string text, Run gap, int matchStart, int carried, List<string> pieces)
    {
        if (gap.Length == 0)
        {
            return carried == NoOpenPiece ? matchStart : carried;
        }

        if (carried != NoOpenPiece)
        {
            Emit(text, carried, gap.Start - carried, pieces);
        }
        Emit(text, gap, pieces);
        return matchStart;
    }

    /// <summary>Keeps the gap and drops the match, or the reverse when <paramref name="invert"/> asks.</summary>
    private static int StepRemoved(string text, Run gap, int matchStart, int matchLength, bool invert, List<string> pieces)
    {
        if (invert)
        {
            Emit(text, matchStart, matchLength, pieces);
        }
        else
        {
            Emit(text, gap, pieces);
        }
        return NoOpenPiece;
    }

    /// <summary>
    /// <see cref="SplitBehavior.MergedWithPrevious"/> and
    /// <see cref="SplitBehavior.MergedWithNext"/> share one body because invert
    /// exchanges them: each joins a match to the gap on one side, and inverting
    /// picks the other side.
    /// </summary>
    private static int StepMerged(
        string text, Run gap, int matchStart, int matchLength, SplitRule rule, int carried, List<string> pieces)
    {
        if (!WithNext(rule))
        {
            // The gap immediately precedes the match, so the two are one
            // contiguous run of text -- no carry needed across calls.
            Emit(text, gap.Start, (matchStart + matchLength) - gap.Start, pieces);
            return NoOpenPiece;
        }

        if (carried == NoOpenPiece)
        {
            Emit(text, gap, pieces);
        }
        else
        {
            Emit(text, carried, (gap.Start + gap.Length) - carried, pieces);
        }
        return matchStart;
    }

    /// <summary>Whether <paramref name="rule"/> joins a match to the gap that follows it, after invert.</summary>
    private static bool WithNext(SplitRule rule) => (rule.Behavior == SplitBehavior.MergedWithNext) ^ rule.Invert;

    /// <summary>Closes out the text after the last match: the trailing gap, and any piece still open.</summary>
    private static void Close(string text, int cursor, SplitRule rule, int carried, List<string> pieces)
    {
        var gap = new Run(cursor, text.Length - cursor);
        switch (rule.Behavior)
        {
            case SplitBehavior.Removed:
                if (!rule.Invert)
                {
                    Emit(text, gap, pieces);
                }
                break;
            case SplitBehavior.MergedWithPrevious:
            case SplitBehavior.MergedWithNext:
                CloseMerged(text, gap, rule, carried, pieces);
                break;
            case SplitBehavior.Isolated:
            case SplitBehavior.Contiguous:
                // A run left open by Contiguous closes here; Isolated never
                // opens one, so carried is always NoOpenPiece and this only
                // emits the trailing gap.
                if (carried != NoOpenPiece)
                {
                    Emit(text, carried, gap.Start - carried, pieces);
                }
                Emit(text, gap, pieces);
                break;
            default:
                throw new ArgumentOutOfRangeException(
                    nameof(rule), rule.Behavior, "Not one of the five Split behaviours.");
        }
    }

    /// <summary>The trailing half of <see cref="StepMerged"/>: nothing follows the last match to attach to.</summary>
    private static void CloseMerged(string text, Run gap, SplitRule rule, int carried, List<string> pieces)
    {
        if (WithNext(rule) && carried != NoOpenPiece)
        {
            Emit(text, carried, (gap.Start + gap.Length) - carried, pieces);
        }
        else
        {
            // The trailing gap has no following match to join: either the merge
            // points backwards (MergedWithPrevious, after invert), which never
            // carries a piece across calls, or it points forwards but nothing
            // is open -- the text had no match at all. Either way it stands
            // alone, the same way a leading gap does under MergedWithNext.
            Emit(text, gap, pieces);
        }
    }
}
