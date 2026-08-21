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

    public MetaspaceEscape(char replacement, MetaspacePrependScheme prependScheme, bool removeExtraWhitespaces)
    {
        Replacement = replacement;
        PrependScheme = prependScheme;
        RemoveExtraWhitespaces = removeExtraWhitespaces;
    }

    public char Replacement { get; }

    public MetaspacePrependScheme PrependScheme { get; }

    public bool RemoveExtraWhitespaces { get; }

    /// <summary>Applies the escape to <paramref name="text"/>.</summary>
    public string Apply(string text)
    {
        string escaped = RemoveExtraWhitespaces ? Collapse(text) : text.Replace(' ', Replacement);

        // Nothing survived the collapse, so there is nothing to prefix — the unigram path
        // has always returned empty here rather than a lone symbol.
        return escaped.Length == 0 || PrependScheme == MetaspacePrependScheme.Never
            ? escaped
            : Replacement + escaped;
    }

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
