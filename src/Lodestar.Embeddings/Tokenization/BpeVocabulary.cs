using System.Text;

namespace Lodestar.Embeddings.Tokenization;

/// <summary>One line of a merge table: the two symbols it joins.</summary>
/// <remarks>
/// The pair's <em>index</em> in <see cref="BpeVocabulary.Merges"/> is its rank,
/// and rank is the whole algorithm — the lowest-ranked applicable merge is
/// always the next one applied. Reordering the list changes the tokenization.
/// </remarks>
/// <param name="Left">The symbol on the left of the join.</param>
/// <param name="Right">The symbol on the right of the join.</param>
public readonly record struct MergePair(string Left, string Right);

/// <summary>
/// A pretrained BPE model: its vocabulary, its ranked merge table, and the
/// pipeline flags that decide how text reaches them.
/// </summary>
/// <remarks>
/// Read from a <c>tokenizer.json</c> by <see cref="Persistence.TokenizerJsonLoader"/>
/// or from a <c>vocab.json</c>/<c>merges.txt</c> pair by
/// <see cref="Persistence.BpeFilesLoader"/>. It restates what the file declared
/// and decides nothing itself.
/// </remarks>
/// <param name="Vocab">Token to id.</param>
/// <param name="Merges">The merge table in rank order; index 0 is rank 0.</param>
public sealed record BpeVocabulary(
    IReadOnlyDictionary<string, int> Vocab,
    IReadOnlyList<MergePair> Merges)
{
    /// <summary>The whole <c>added_tokens</c> table: every token matched as literal text ahead of the merge loop.</summary>
    /// <remarks>
    /// Not "the tokens <see cref="Vocab"/> lacks" — overlap is expected and the two
    /// must agree on the id where they overlap, since a token left out here
    /// tokenizes character by character instead of being matched whole. See
    /// <c>docs/guides/embeddings.md</c>'s "Loading vocabularies" for why the overlap
    /// is kept rather than subtracted, and <c>docs/equivalence.md</c>'s
    /// <c>tokenizer.decode(ids)</c> row for what <c>skipSpecialTokens</c> drops.
    /// </remarks>
    public IReadOnlyList<AddedToken> AddedTokens { get; init; } = [];

    /// <summary>Whether text is mapped through the byte alphabet before merging.</summary>
    public bool ByteLevel { get; init; }

    /// <summary>Whether a space is prepended to each piece the <c>ByteLevel</c> step is handed.</summary>
    public bool AddPrefixSpace { get; init; }

    /// <summary>Whether a whole pre-tokenized piece present in the vocabulary skips merging.</summary>
    public bool IgnoreMerges { get; init; }

    /// <summary>
    /// Whether a run of consecutive characters the vocabulary does not cover
    /// collapses into a single unknown token — HuggingFace's <c>fuse_unk</c>.
    /// </summary>
    /// <remarks>
    /// A run stops at a pre-tokenizer boundary, and has no effect at all
    /// without an <see cref="UnkToken"/> — an uncovered character is dropped
    /// then, so there is nothing to fuse. Both are what
    /// <c>tokenizers</c> 0.23.1 does, measured.
    /// </remarks>
    public bool FuseUnk { get; init; }

    private readonly string? _endOfWordSuffix;

    /// <summary>The marker closing a word, e.g. <c>&lt;/w&gt;</c>; <see langword="null"/> for byte-level models.</summary>
    /// <remarks>
    /// An empty marker marks nothing, so it reads back as <see langword="null"/>: a
    /// <c>tokenizer.json</c> may declare <c>"end_of_word_suffix": ""</c>, and the two spellings
    /// have to mean one thing on a public, constructible type — otherwise a loaded vocabulary and
    /// a hand-built one compare unequal while behaving identically.
    /// </remarks>
    public string? EndOfWordSuffix
    {
        get => _endOfWordSuffix;
        init => _endOfWordSuffix = string.IsNullOrEmpty(value) ? null : value;
    }

    private readonly string? _continuingSubwordPrefix;

    /// <summary>The marker opening a non-initial piece; <see langword="null"/> when there is none.</summary>
    /// <remarks>
    /// An empty marker reads back as <see langword="null"/>, the same normalization
    /// <see cref="EndOfWordSuffix"/> gets. Pairing a non-empty prefix with
    /// <see cref="ByteLevel"/> is not forbidden on this record — it restates what a
    /// file declared — but is refused by <see cref="BpeTokenizer"/>'s constructor and
    /// by <see cref="Persistence.TokenizerJsonLoader"/>; see
    /// <c>docs/equivalence.md</c>'s <c>continuing_subword_prefix</c> row for why.
    /// </remarks>
    public string? ContinuingSubwordPrefix
    {
        get => _continuingSubwordPrefix;
        init => _continuingSubwordPrefix = string.IsNullOrEmpty(value) ? null : value;
    }

    /// <summary>The unknown token, when the model declares one.</summary>
    public string? UnkToken { get; init; }

    /// <summary>
    /// The last pattern text is split on before merging; <see langword="null"/> when
    /// <see cref="PreSplit"/> or <see cref="NoPreTokenizer"/> says what happens instead.
    /// </summary>
    /// <remarks>
    /// Set with <see cref="PreSplit"/>, re-splits each piece it produced, or — left
    /// <see langword="null"/> — that pre-split is the only split: <c>docs/equivalence.md</c>'s
    /// <c>Sequence([Split(pattern), ByteLevel(…)])</c> row. The classic lineage's own is
    /// <see cref="BpePatterns.Whitespace"/>, which a caller now writes rather than omits.
    /// </remarks>
    public string? PreTokenizerPattern { get; init; }

    /// <summary>Whether the model declares no pre-tokenizer, so each added-token segment reaches the merge loop whole; <see langword="false"/> unless the file says so.</summary>
    /// <remarks>
    /// Two file shapes mean this and they are not otherwise alike: an absent
    /// <c>pre_tokenizer</c>, and a <c>ByteLevel</c> whose <c>use_regex</c> is off.
    /// Measured against <c>tokenizers</c> 0.23.1, a model declaring no pre-tokenizer
    /// encodes <c>"aZ Za"</c> to <c>['a', '[UNK]', 'a']</c> where the <c>Whitespace</c>
    /// split gives four tokens — see <c>tests/oracles/bpe_no_split.json</c>, models
    /// <c>absent</c> and <c>whitespace</c>.
    /// </remarks>
    public bool NoPreTokenizer { get; init; }

    /// <summary>
    /// The <c>Split</c> step a <c>Sequence</c> pre-tokenizer declares, before
    /// <see cref="PreTokenizerPattern"/>; <see langword="null"/> when none.
    /// </summary>
    /// <remarks>
    /// Splits twice: this step, then <c>ByteLevel</c>'s own pattern per piece,
    /// unless <c>use_regex</c> is off — measured under Llama-3's pattern:
    /// <c>"aujourd'hui"</c> → <c>['aujourd', "'", 'hui']</c> with it,
    /// <c>['aujourd', "'hui"]</c> without (<c>bpe_sequence_split.json</c> cases 1, 10).
    /// </remarks>
    public BpeSplitStep? PreSplit { get; init; }

    /// <summary>
    /// The normalization forms the file declared, in the order it declared them,
    /// empty when it declared no normalizer.
    /// </summary>
    /// <remarks>
    /// A list rather than a single form because a <c>Sequence</c> may name several,
    /// and applied in order rather than collapsed to the last one: composing these
    /// four does reduce to the last through NFKC's idempotence, but a reader would
    /// have to verify that identity to trust the code, and the loop costs nothing.
    /// </remarks>
    public IReadOnlyList<NormalizationForm> NormalizationForms { get; init; } = [];

    /// <summary>The whitespace escape the file declared, or <see langword="null"/> when it declared none.</summary>
    /// <remarks>
    /// A file writes it two ways — a <c>Metaspace</c> pre-tokenizer, or a <c>Prepend</c>
    /// plus <c>Replace</c> normalizer sequence — and
    /// <see cref="Persistence.TokenizerJsonLoader"/> reduces both to one value (decision
    /// 0050 §2). Internal because nothing outside this assembly reads it: public, it would
    /// owe a <c>docs/reference/</c> entry and a <c>samples/Lodestar.Sample</c> member
    /// reference for a value only <see cref="BpeTokenizer"/> consumes.
    /// </remarks>
    internal MetaspaceEscape? Metaspace { get; init; }

    /// <summary>Number of entries in the vocabulary.</summary>
    public int Count => Vocab.Count;

    /// <summary>
    /// Compares the flags, then every merge, every token-to-id mapping and every
    /// <see cref="AddedTokens"/> entry, in order.
    /// </summary>
    /// <remarks>
    /// The generated equality compares <see cref="Vocab"/> and <see cref="Merges"/>
    /// by reference, so two vocabularies read from the same file would be
    /// unequal — the one comparison a caller has a reason to make.
    /// </remarks>
    public bool Equals(BpeVocabulary? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        if (other is null
            || ByteLevel != other.ByteLevel
            || AddPrefixSpace != other.AddPrefixSpace
            || IgnoreMerges != other.IgnoreMerges
            || FuseUnk != other.FuseUnk
            || NoPreTokenizer != other.NoPreTokenizer
            || !string.Equals(EndOfWordSuffix, other.EndOfWordSuffix, StringComparison.Ordinal)
            || !string.Equals(ContinuingSubwordPrefix, other.ContinuingSubwordPrefix, StringComparison.Ordinal)
            || !string.Equals(UnkToken, other.UnkToken, StringComparison.Ordinal)
            || !string.Equals(PreTokenizerPattern, other.PreTokenizerPattern, StringComparison.Ordinal)
            || PreSplit != other.PreSplit
            || !SameMetaspace(Metaspace, other.Metaspace)
            || Vocab.Count != other.Vocab.Count
            || Merges.Count != other.Merges.Count
            || AddedTokens.Count != other.AddedTokens.Count
            || NormalizationForms.Count != other.NormalizationForms.Count)
        {
            return false;
        }
        for (int i = 0; i < Merges.Count; i++)
        {
            if (!Merges[i].Equals(other.Merges[i]))
            {
                return false;
            }
        }
        for (int i = 0; i < AddedTokens.Count; i++)
        {
            if (!AddedTokens[i].Equals(other.AddedTokens[i]))
            {
                return false;
            }
        }
        for (int i = 0; i < NormalizationForms.Count; i++)
        {
            if (NormalizationForms[i] != other.NormalizationForms[i])
            {
                return false;
            }
        }
        return SameEntries(Vocab, other.Vocab);
    }

    /// <summary>Hashes the scalars and the counts, which is O(1) and consistent with equality.</summary>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 31) + Vocab.Count;
            hash = (hash * 31) + Merges.Count;
            hash = (hash * 31) + AddedTokens.Count;
            hash = (hash * 31) + NormalizationForms.Count;
            hash = (hash * 31) + (ByteLevel ? 1 : 0);
            hash = (hash * 31) + (AddPrefixSpace ? 1 : 0);
            hash = (hash * 31) + (IgnoreMerges ? 1 : 0);
            hash = (hash * 31) + (FuseUnk ? 1 : 0);
            hash = (hash * 31) + (NoPreTokenizer ? 1 : 0);
            hash = (hash * 31) + (EndOfWordSuffix is null ? 0 : StringComparer.Ordinal.GetHashCode(EndOfWordSuffix));
            hash = (hash * 31) + (ContinuingSubwordPrefix is null ? 0 : StringComparer.Ordinal.GetHashCode(ContinuingSubwordPrefix));
            hash = (hash * 31) + (UnkToken is null ? 0 : StringComparer.Ordinal.GetHashCode(UnkToken));
            hash = (hash * 31) + (PreTokenizerPattern is null ? 0 : StringComparer.Ordinal.GetHashCode(PreTokenizerPattern));
            hash = (hash * 31) + (PreSplit is null ? 0 : PreSplit.GetHashCode());
            return (hash * 31) + (Metaspace is null ? 0 : Metaspace.Replacement);
        }
    }

    /// <summary>Compares two escapes by value, which <see cref="MetaspaceEscape"/> itself does not.</summary>
    private static bool SameMetaspace(MetaspaceEscape? left, MetaspaceEscape? right) =>
        left is null
            ? right is null
            : right is not null
                && left.Replacement == right.Replacement
                && left.PrependScheme == right.PrependScheme
                && left.RemoveExtraWhitespaces == right.RemoveExtraWhitespaces;

    private static bool SameEntries(IReadOnlyDictionary<string, int> left, IReadOnlyDictionary<string, int> right)
    {
        foreach (KeyValuePair<string, int> entry in left)
        {
            if (!right.TryGetValue(entry.Key, out int id) || id != entry.Value)
            {
                return false;
            }
        }
        return true;
    }
}
