namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// One <c>added_tokens</c> entry: text matched ahead of the model, and the rules
/// that decide where it matches.
/// </summary>
/// <remarks>
/// <para>
/// The five flags are HuggingFace's, reproduced as measured against
/// <c>tokenizers</c> 0.23.1 — see
/// <c>docs/decisions/0022-added-token-matching-flags.md</c>.
/// </para>
/// <para>
/// Four of them — <see cref="Lstrip"/>, <see cref="Rstrip"/>,
/// <see cref="SingleWord"/> and <see cref="Special"/> — default to
/// <see langword="false"/>, which is the plain literal match this library did
/// before they existed. <see cref="Normalized"/> is the exception and defaults to
/// <c>!</c><see cref="Special"/>, because that is the rule HuggingFace's own
/// constructor applies and there is no value that would be inert: an entry is
/// matched against either the raw text or the normalized one, and it has to be
/// matched against something.
/// </para>
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
    /// <para>
    /// A word character is a letter, a digit or <c>_</c>, Unicode-aware:
    /// <c>a</c>, <c>1</c>, <c>_</c> and <c>é</c> all block a match, while
    /// <c>.</c>, <c>-</c> and whitespace do not.
    /// </para>
    /// <para>
    /// This diverges from HuggingFace on the same boundary <c>docs/equivalence.md</c>
    /// already records for the BPE split pattern: Rust's <c>char::is_alphanumeric</c>
    /// is code-point-based and treats the <c>Nl</c>/<c>No</c> Unicode categories
    /// (<c>²</c>, <c>Ⅷ</c>) as word characters, and a letter above the Basic
    /// Multilingual Plane as one too. .NET's <see cref="char.IsLetterOrDigit(char)"/>
    /// is <c>char</c>-based — it neither covers <c>Nl</c>/<c>No</c> nor recognizes an
    /// above-BMP letter, which arrives as one half of a surrogate pair in category
    /// <c>Cs</c>. Every case in the measured table agrees; this is a known,
    /// unmeasured gap rather than something this library observed and chose to
    /// diverge on.
    /// </para>
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
    /// <para>
    /// HuggingFace's own <c>normalized</c> field, and <strong>the</strong>
    /// discriminator for which pass an entry runs in:
    /// <see langword="false"/> matches the <em>raw</em> text, ahead of the
    /// normalizer; <see langword="true"/> normalizes the entry's own
    /// <see cref="Content"/> and matches it against the normalized text.
    /// <c>tokenizers</c> keeps one trie for each, and splits on the raw one first.
    /// </para>
    /// <para>
    /// It is not a synonym for <c>!</c><see cref="Special"/>, though every entry
    /// <c>add_special_tokens</c> and <c>add_tokens</c> produce looks like one:
    /// those set <c>normalized = !special</c>, and all four combinations are
    /// representable and round-trip through a <c>tokenizer.json</c>. Measured
    /// against <c>tokenizers</c> 0.23.1 over a <c>Lowercase</c> normalizer, with
    /// <c>[CLS]</c> added and the input <c>'a [cls] b'</c>: <c>special=true,
    /// normalized=true</c> matches and emits <c>[cls]</c>, while <c>special=true,
    /// normalized=false</c> does not match at all. The <c>special</c> flag is the
    /// same in both.
    /// </para>
    /// <para>
    /// This changes nothing for <see cref="BpeTokenizer"/>, whose loader refuses
    /// any normalizer at all: with nothing to normalize, both passes see the same
    /// text. It is <see cref="WordPieceTokenizer"/>, which reads <c>lowercase</c>
    /// from the file, where the two differ.
    /// </para>
    /// <para>
    /// Unset, it reads <c>!</c><see cref="Special"/> — Rust's
    /// <c>AddedToken::from(content, special)</c>, and the one place that rule
    /// lives. A loader that read it from a file and a caller that wrote
    /// <c>new AddedToken("&lt;x&gt;", 2)</c> by hand therefore describe the same
    /// token, which they did not while the default sat in the loader alone.
    /// </para>
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
    /// <see cref="Normalized"/>, so a token that left it unset and one that set it
    /// to the value the default already gives would be unequal while agreeing on
    /// every observable property — and unequal in exactly the case that matters,
    /// comparing a vocabulary read from a file against one written out by hand.
    /// <see cref="BpeVocabulary"/> and <see cref="WordPieceVocabulary"/> both
    /// compare their tables element-wise through this method, so that difference
    /// would surface as two equal vocabularies reporting themselves unequal.
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
