namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// What a <c>Split</c> pre-tokenizer step does with the text around its matches,
/// matching HuggingFace <c>tokenizers</c>' <c>pre_tokenizers.Split(behavior=…)</c>.
/// </summary>
/// <remarks>
/// The names are the ones a <c>tokenizer.json</c> uses, in PascalCase — the
/// Python constructor's snake_case is refused, e.g. <c>"isolated"</c>; see
/// <c>docs/equivalence.md</c>'s <c>Split(pattern, behavior=…, invert=…)</c> row.
/// An empty piece never reaches the output, under any of the five.
/// </remarks>
public enum SplitBehavior
{
    /// <summary>Every match and every gap, each its own piece.</summary>
    Isolated,

    /// <summary>The gaps only; the matches are dropped.</summary>
    Removed,

    /// <summary>Each match joins the gap before it.</summary>
    MergedWithPrevious,

    /// <summary>Each match joins the gap after it.</summary>
    MergedWithNext,

    /// <summary>Like <see cref="Isolated"/>, except that adjacent matches become one piece.</summary>
    Contiguous,
}
