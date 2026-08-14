namespace DataNet.Embeddings.Tokenization;

/// <summary>The special tokens a model expects around a single sequence, and the token it pads with — as data, not a hardcoded convention.</summary>
/// <remarks>
/// The C# form of a HuggingFace <c>TemplateProcessing(single="[CLS] $A [SEP]", …)</c>
/// plus <c>enable_padding</c>'s <c>pad_token</c> — see
/// <c>docs/guides/embeddings.md</c>'s "Embed a batch" for the built-in families.
/// Tokens are named, not numbered: <see cref="ISubwordTokenizer.TryGetId"/>
/// resolves the id, so a vocabulary missing one fails loudly at encode time.
/// </remarks>
/// <param name="PrefixTokens">Tokens inserted before the text, e.g. <c>[CLS]</c>. May be empty.</param>
/// <param name="SuffixTokens">Tokens appended after the text, e.g. <c>[SEP]</c>. May be empty.</param>
/// <param name="PadToken">The token used to pad a sequence up to the batch length.</param>
public sealed record SpecialTokenTemplate(
    IReadOnlyList<string> PrefixTokens,
    IReadOnlyList<string> SuffixTokens,
    string PadToken)
{
    /// <summary>BERT and its descendants: <c>[CLS] … [SEP]</c>, padded with <c>[PAD]</c>.</summary>
    public static SpecialTokenTemplate Bert { get; } = new(["[CLS]"], ["[SEP]"], "[PAD]");

    /// <summary>RoBERTa and XLM-R: <c>&lt;s&gt; … &lt;/s&gt;</c>, padded with <c>&lt;pad&gt;</c>.</summary>
    public static SpecialTokenTemplate Roberta { get; } = new(["<s>"], ["</s>"], "<pad>");

    /// <summary>T5: no prefix, <c>… &lt;/s&gt;</c>, padded with <c>&lt;pad&gt;</c>.</summary>
    public static SpecialTokenTemplate T5 { get; } = new([], ["</s>"], "<pad>");

    /// <summary>No special tokens at all; padding still needs a token, and uses <c>[PAD]</c>.</summary>
    /// <remarks>
    /// The equivalent of a HuggingFace tokenizer with no <c>post_processor</c> —
    /// which is what DataNet's loaders accept, since they refuse a file that
    /// carries one.
    /// </remarks>
    public static SpecialTokenTemplate None { get; } = new([], [], "[PAD]");

    /// <summary>How many special tokens a sequence carries; the truncation budget is reduced by this much.</summary>
    public int SpecialTokenCount => PrefixTokens.Count + SuffixTokens.Count;

    /// <summary>Compares the pad token and both token lists element by element.</summary>
    /// <remarks>
    /// The generated equality would compare <see cref="PrefixTokens"/> and
    /// <see cref="SuffixTokens"/> by reference, so two templates spelling the same
    /// convention would be unequal — and a record advertises value equality.
    /// </remarks>
    public bool Equals(SpecialTokenTemplate? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        return other is not null
            && string.Equals(PadToken, other.PadToken, StringComparison.Ordinal)
            && SameTokens(PrefixTokens, other.PrefixTokens)
            && SameTokens(SuffixTokens, other.SuffixTokens);
    }

    /// <summary>Hashes the pad token and the two counts, which is O(1) and consistent with equality.</summary>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 31) + StringComparer.Ordinal.GetHashCode(PadToken);
            hash = (hash * 31) + PrefixTokens.Count;
            return (hash * 31) + SuffixTokens.Count;
        }
    }

    private static bool SameTokens(IReadOnlyList<string> a, IReadOnlyList<string> b)
    {
        if (a.Count != b.Count)
        {
            return false;
        }
        for (int i = 0; i < a.Count; i++)
        {
            if (!string.Equals(a[i], b[i], StringComparison.Ordinal))
            {
                return false;
            }
        }
        return true;
    }
}
