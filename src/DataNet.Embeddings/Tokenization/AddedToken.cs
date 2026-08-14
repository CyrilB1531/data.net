namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// One <c>added_tokens</c> entry: text matched ahead of the model, and the rules
/// that decide where it matches.
/// </summary>
/// <remarks>
/// The five flags are HuggingFace's, measured against <c>tokenizers</c> 0.23.1. See
/// <c>docs/decisions/0022-added-token-matching-flags.md</c> §1 for what the three
/// span-shaping flags do, §5 for <see cref="Special"/>, and §4 for why
/// <see cref="Normalized"/> alone defaults to something other than <see langword="false"/>.
/// </remarks>
/// <param name="Content">The text matched, exactly and ordinally.</param>
/// <param name="Id">The id the match produces.</param>
public sealed record AddedToken(string Content, int Id)
{
    /// <summary>Absorbs the whitespace immediately to the left of a match into it.</summary>
    /// <remarks>
    /// All of it, not one character. The id is unchanged; what disappears is the
    /// piece that whitespace would otherwise have produced — a <c>Ġ</c> on a
    /// byte-level model. <c>roberta-base</c> sets this on <c>&lt;mask&gt;</c>.
    /// </remarks>
    public bool Lstrip { get; init; }

    /// <summary>The mirror of <see cref="Lstrip"/>, on the right.</summary>
    public bool Rstrip { get; init; }

    /// <summary>Matches only where both neighbours are non-word characters or the ends of the text.</summary>
    /// <remarks>
    /// A word character is a letter, digit or <c>_</c>: <c>a</c>, <c>1</c>, <c>_</c> and
    /// <c>é</c> block a match; <c>.</c>, <c>-</c> and whitespace do not. Diverges from
    /// HuggingFace's code-point-based test the way <c>docs/equivalence.md</c> records for
    /// the BPE split pattern — measured and named as a known gap in
    /// <c>docs/decisions/0022-added-token-matching-flags.md</c> §8.
    /// </remarks>
    public bool SingleWord { get; init; }

    /// <summary>Whether the file marked this entry <c>special</c>.</summary>
    /// <remarks>
    /// One consequence, measured: it is the entry a decoder drops for
    /// <c>skip_special_tokens</c> — see
    /// <see cref="BpeTokenizer.Decode(IReadOnlyList{int}, bool)"/>. It decides
    /// nothing about <em>where</em> the entry matches; <see cref="Normalized"/> is
    /// the field that does, and the two are independent.
    /// </remarks>
    public bool Special { get; init; }

    /// <summary>Whether the model's normalizer applies to this entry, and to the text it is matched against.</summary>
    /// <remarks>
    /// <see langword="false"/> matches raw text, ahead of the normalizer;
    /// <see langword="true"/> normalizes both <see cref="Content"/> and the text it
    /// matches against. Not a synonym for <c>!</c><see cref="Special"/>, though every
    /// entry HuggingFace's constructors produce looks like one — see
    /// <c>docs/decisions/0022-added-token-matching-flags.md</c> §3 for the measurement
    /// telling them apart, §4 for the unset-value default.
    /// </remarks>
    public bool Normalized
    {
        get => _normalized ?? !Special;
        init => _normalized = value;
    }

    private readonly bool? _normalized;

    /// <summary>Compares the content and all five flags, defaults resolved.</summary>
    /// <remarks>
    /// The generated equality would compare the <em>backing field</em> of
    /// <see cref="Normalized"/> rather than its resolved value, so a token that left it
    /// unset and one that set it explicitly to the default would disagree while
    /// observably identical — exactly when comparing a vocabulary read from a file
    /// against one written by hand, which <see cref="BpeVocabulary"/> and
    /// <see cref="WordPieceVocabulary"/> both do element-wise through this method.
    /// </remarks>
    /// <param name="other">The token to compare against.</param>
    public bool Equals(AddedToken? other) =>
        other is not null
        && string.Equals(Content, other.Content, StringComparison.Ordinal)
        && Id == other.Id
        && Lstrip == other.Lstrip
        && Rstrip == other.Rstrip
        && SingleWord == other.SingleWord
        && Special == other.Special
        && Normalized == other.Normalized;

    /// <summary>Hashes the content, the id and the flags, defaults resolved.</summary>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 31) + StringComparer.Ordinal.GetHashCode(Content);
            hash = (hash * 31) + Id;
            hash = (hash * 31) + (Lstrip ? 1 : 0);
            hash = (hash * 31) + (Rstrip ? 1 : 0);
            hash = (hash * 31) + (SingleWord ? 1 : 0);
            hash = (hash * 31) + (Special ? 1 : 0);
            return (hash * 31) + (Normalized ? 1 : 0);
        }
    }
}
