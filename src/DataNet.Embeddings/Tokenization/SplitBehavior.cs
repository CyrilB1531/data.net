namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// What a <c>Split</c> pre-tokenizer step does with the text around its matches,
/// matching HuggingFace <c>tokenizers</c>' <c>pre_tokenizers.Split(behavior=…)</c>.
/// </summary>
/// <remarks>
/// The names are the ones a <c>tokenizer.json</c> uses. The reference's Python
/// constructor spells the same values in snake_case; the format does not, and a
/// document declaring <c>"isolated"</c> is refused there with
/// <c>unknown variant `isolated`</c> — measured against <c>tokenizers</c> 0.23.1.
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
