using System.Text.RegularExpressions;
using DataNet.Internal;

namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// Splits text into the pieces the merge loop runs over, independently.
/// </summary>
/// <remarks>
/// A model declares two split patterns (<c>Sequence</c> of <c>Split</c> then
/// <c>ByteLevel</c>), one (<c>ByteLevel</c>, or the classic lineage's
/// <see cref="BpePatterns.Whitespace"/>), or none at all — <c>docs/equivalence.md</c>'s
/// <c>Split(pattern, …)</c> and <c>Sequence(...)</c> rows. Each is caller-supplied,
/// compiled with <see cref="RegexDefaults.MatchTimeout"/> against a hung thread.
/// </remarks>
internal sealed class BpePreTokenizer
{
    private const int NoOpenPiece = -1;

    /// <summary>A behaviour and its invert flag, bundled so <see cref="Step"/> and
    /// <see cref="Close"/> take one parameter instead of two -- both sit at the
    /// S107 parameter-count limit otherwise.</summary>
    private readonly record struct SplitRule(SplitBehavior Behavior, bool Invert);

    /// <summary>A run of text as a position and a length, never yet substringed.</summary>
    /// <remarks>Named <c>Run</c>, not <c>Span</c>, so it does not shadow <see cref="System.Span{T}"/> in a
    /// package that leans on spans elsewhere.</remarks>
    private readonly record struct Run(int Start, int Length);

    /// <summary>The first pattern, or <see langword="null"/> when nothing is split at all.</summary>
    private readonly Regex? _first;
    private readonly Regex? _second;
    private readonly SplitRule _rule;

    // Compiled on none of the four patterns, so one policy covers them all: it buys
    // 1.44x matching for 6-26 ms a build, on an i7-4770S over 976 KiB of prose (#122).
    public BpePreTokenizer(BpeSplitStep? preSplit, string? pattern, bool noSplit)
    {
        if (noSplit)
        {
            // No pattern to match, so no rule to arrange matches with: Split emits
            // the text whole. The behaviour rules below have nothing to govern here.
            _first = null;
            _second = null;
            _rule = default;
            return;
        }

        // long-comment: the default rule when no Split step is declared is not
        // obvious, and the measured case is what keeps it from looking arbitrary.
        // A pre-split runs first and the second pattern re-splits its pieces
        // (issue #143). Only the pre-split carries a declared behaviour; a null
        // pre-split still needs one to drive Apply, and it is Removed with invert
        // on -- "keep the regex matches, drop everything else" -- not Isolated.
        // The two are interchangeable only when the pattern never leaves a gap,
        // which is true of every shipped byte-level pattern (Gpt2/Llama3/Qwen2)
        // but false of BpePatterns.Whitespace's \w+|[^\w\s]+, which never matches
        // a run of whitespace: under Isolated that whitespace would surface as its
        // own piece and reach the merge loop as an uncovered symbol, where measured
        // (bpe.json, e.g. " leading space") the reference produces no such piece
        // and no substituted token for one. Removed+invert keeps that path
        // byte-for-byte unchanged; BpeSplitBehaviorTests never exercises this
        // branch at all, since every corpus case supplies its own BpeSplitStep.
        if (preSplit is null)
        {
            // Not null here: BpeTokenizer.EnsurePreTokenizerIsDeclared refuses a
            // vocabulary declaring no pre-split, no pattern and not the mode.
            _first = Compile(pattern!);
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
        // The mode: no pattern, so there are no matches, and #145's SplitBehavior
        // and invert have nothing to arrange -- the text is the one piece.
        if (_first is null)
        {
            Emit(text, 0, text.Length, pieces);
            return;
        }

        if (_second is null)
        {
            Apply(_first, _rule, text, pieces);
            return;
        }

        // Re-splits the first pattern's raw-text pieces, always Isolated/invert-off --
        // docs/equivalence.md's Split(...) row. A local list: the type is thread-safe.
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
        // Not .Cast<Match>(): MatchCollection's own enumerator binds directly to
        // Match on both target frameworks, so .Cast<Match>() would only add an iterator.
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
                // A run left open by Contiguous closes here; Isolated never opens
                // one, so this only ever emits the trailing gap for that behaviour.
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
            // Nothing follows the last match to join: the merge points backwards
            // (after invert) or nothing is open at all -- either way the gap stands alone.
            Emit(text, gap, pieces);
        }
    }
}
