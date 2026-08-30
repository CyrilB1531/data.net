namespace Lodestar.Embeddings.Tokenization;

/// <summary>Where the replacement is prepended, which a file spells three ways.</summary>
internal enum MetaspacePrependScheme
{
    /// <summary>Never prepended.</summary>
    Never,

    /// <summary>Prepended to the first piece only.</summary>
    First,

    /// <summary>Prepended to every piece.</summary>
    Always,
}

/// <summary>Escapes whitespace to a meta symbol, the way SentencePiece does.</summary>
internal sealed class MetaspaceEscape
{
    private static readonly char[] Spaces = [' '];

    public MetaspaceEscape(
        char replacement,
        MetaspacePrependScheme prependScheme,
        bool removeExtraWhitespaces,
        bool skipPrependWhenAlreadyPrefixed)
    {
        Replacement = replacement;
        PrependScheme = prependScheme;
        RemoveExtraWhitespaces = removeExtraWhitespaces;
        SkipPrependWhenAlreadyPrefixed = skipPrependWhenAlreadyPrefixed;
    }

    public char Replacement { get; }

    public MetaspacePrependScheme PrependScheme { get; }

    public bool RemoveExtraWhitespaces { get; }

    /// <summary>Whether the prepend is skipped when the escaped text already begins with the replacement.</summary>
    /// <remarks>
    /// The one field the two declarations disagree on: a <c>Metaspace</c> block guards its
    /// prepend on <c>starts_with</c>, and the <c>Prepend</c> + <c>Replace</c> normalizer
    /// sequence prepends unconditionally, since <c>Prepend</c> runs before <c>Replace</c>
    /// and knows nothing of the symbol. Decision 0062 measures the boundary and amends
    /// 0050 §2's "two writings of one value" to hold everywhere but here.
    /// </remarks>
    public bool SkipPrependWhenAlreadyPrefixed { get; }

    /// <summary>Applies the escape to <paramref name="text"/>.</summary>
    /// <param name="text">The piece to escape.</param>
    /// <param name="isFirstSplit">
    /// Whether this piece is the first the input produced, which is what
    /// <see cref="MetaspacePrependScheme.First"/> prepends to. An added token counts as a
    /// piece and so consumes it — measured against <c>tokenizers</c> 0.23.1, where
    /// <c>"&lt;s&gt;the cat"</c> under <c>first</c> is <c>['&lt;s&gt;', 'the', '▁cat']</c>.
    /// </param>
    public string Apply(string text, bool isFirstSplit)
    {
        string escaped = RemoveExtraWhitespaces ? Collapse(text) : text.Replace(' ', Replacement);

        // Nothing survived the collapse, so there is nothing to prefix — the unigram path
        // has always returned empty here rather than a lone symbol.
        if (escaped.Length == 0 || !Prepends(isFirstSplit))
        {
            return escaped;
        }

        // The guard reads the escaped text, where tokenizers applies it too: a leading
        // space begins with the symbol only once the replace has run.
        return SkipPrependWhenAlreadyPrefixed && escaped[0] == Replacement
            ? escaped
            : Replacement + escaped;
    }

    /// <summary>Whether this piece is one the scheme prepends to at all.</summary>
    private bool Prepends(bool isFirstSplit) =>
        PrependScheme == MetaspacePrependScheme.Always
        || (PrependScheme == MetaspacePrependScheme.First && isFirstSplit);

    /// <summary>Runs of U+0020 become one replacement, and the ends lose theirs.</summary>
    /// <remarks>
    /// Splitting on the space and dropping the empties collapses and trims in one pass.
    /// U+0020 only: a tab no normalizer rewrote stays as it is, which is what
    /// <c>docs/equivalence.md</c>'s Unigram row records.
    /// </remarks>
    private string Collapse(string text)
    {
        string[] parts = text.Split(Spaces, StringSplitOptions.RemoveEmptyEntries);
        return parts.Length == 0 ? string.Empty : string.Join(Replacement.ToString(), parts);
    }
}
