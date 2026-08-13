namespace DataNet.Embeddings.Tokenization;

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
    /// <para>
    /// Not "the tokens <see cref="Vocab"/> lacks". HuggingFace lists a special token in
    /// both tables at the same id — <c>&lt;|endoftext|&gt;</c> is 50256 in GPT-2's own
    /// <c>model.vocab</c> as well as in its <c>added_tokens</c> — and this property is
    /// the only input to <see cref="BpeTokenizer"/>'s pre-merge scan, so a token left
    /// out of it here is a token that tokenizes character by character instead of being
    /// matched whole. Overlap with <see cref="Vocab"/> is expected, and the two must
    /// agree on the id where they overlap.
    /// </para>
    /// <para>
    /// It is also what <c>skipSpecialTokens</c> drops in
    /// <see cref="BpeTokenizer.Decode(IReadOnlyList{int}, bool)"/>. The
    /// <c>special</c> flag a <c>tokenizer.json</c> records per entry is carried on
    /// <see cref="AddedToken.Special"/>; see <c>docs/equivalence.md</c>.
    /// </para>
    /// </remarks>
    public IReadOnlyList<AddedToken> AddedTokens { get; init; } = [];

    /// <summary>Whether text is mapped through the byte alphabet before merging.</summary>
    public bool ByteLevel { get; init; }

    /// <summary>Whether a space is prepended to the input.</summary>
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
    /// <para>
    /// An empty marker prefixes nothing, so it reads back as <see langword="null"/>: a
    /// <c>tokenizer.json</c> may declare <c>"continuing_subword_prefix": ""</c>, and the two spellings
    /// have to mean one thing on a public, constructible type — otherwise a loaded vocabulary and
    /// a hand-built one compare unequal while behaving identically.
    /// </para>
    /// <para>
    /// It cannot be paired with <see cref="ByteLevel"/>. Nothing forbids setting both
    /// here — this record restates what a file declared and decides nothing — but
    /// <see cref="BpeTokenizer"/>'s constructor refuses such a vocabulary, and
    /// <see cref="Persistence.TokenizerJsonLoader"/> refuses the file that spells it,
    /// because the prefix is applied to a byte-level model's merges and not to its
    /// symbols.
    /// </para>
    /// </remarks>
    public string? ContinuingSubwordPrefix
    {
        get => _continuingSubwordPrefix;
        init => _continuingSubwordPrefix = string.IsNullOrEmpty(value) ? null : value;
    }

    /// <summary>The unknown token, when the model declares one.</summary>
    public string? UnkToken { get; init; }

    /// <summary>
    /// The last pattern text is split on before merging; <see langword="null"/> to
    /// split on word boundaries when <see cref="PreSplit"/> is also
    /// <see langword="null"/>. There are two ways for a model to split text — this
    /// pattern and <see cref="PreSplit"/> — and they can combine; see <em>Remarks</em>
    /// for how.
    /// </summary>
    /// <remarks>
    /// <para>
    /// A <see langword="null"/> <see cref="PreSplit"/> falls back to word boundaries,
    /// isolating punctuation from letters and digits — HuggingFace's <c>Whitespace</c>
    /// pre-tokenizer type, not the coarser <c>WhitespaceSplit</c> that only collapses
    /// whitespace runs.
    /// </para>
    /// <para>
    /// When <see cref="PreSplit"/> is set and this pattern is <em>not</em>
    /// <see langword="null"/>, this pattern runs second, over every piece the
    /// pre-split produced. When <see cref="PreSplit"/> is set and this pattern
    /// <em>is</em> <see langword="null"/>, there is no word-boundary fallback: the
    /// pre-split is the only split there is, the state
    /// <see cref="Persistence.TokenizerJsonLoader"/> produces for a <c>Sequence</c>
    /// whose <c>ByteLevel</c> step declares <c>use_regex: false</c>.
    /// </para>
    /// <para>
    /// When this is the <em>only</em> pattern set (<see cref="PreSplit"/>
    /// <see langword="null"/>), it is not run under <see cref="SplitBehavior.Isolated"/>:
    /// the merge loop only ever sees the regex's own matches, with the gaps
    /// between them dropped — <see cref="SplitBehavior.Removed"/> with invert
    /// on — which is <see cref="BpePreTokenizer"/>'s own fallback rule when it
    /// is built with no <see cref="BpeSplitStep"/>, not a choice this property
    /// makes.
    /// </para>
    /// </remarks>
    public string? PreTokenizerPattern { get; init; }

    /// <summary>
    /// The <c>Split</c> step a <c>Sequence</c> pre-tokenizer declares, applied
    /// before <see cref="PreTokenizerPattern"/>; <see langword="null"/> when the
    /// file declares no such step.
    /// </summary>
    /// <remarks>
    /// HuggingFace's <c>Sequence</c> of <c>Split</c> then <c>ByteLevel</c> splits
    /// twice: this step first, then <c>ByteLevel</c>'s own pattern over each
    /// resulting piece, unless its <c>use_regex</c> is off. Measured against
    /// <c>tokenizers</c> 0.23.1 under Llama-3's own <c>Split</c> pattern, where
    /// <c>"aujourd'hui"</c> splits into <c>['aujourd', "'", 'hui']</c> with the
    /// second split and <c>['aujourd', "'hui"]</c> without it
    /// (<c>tests/oracles/bpe_sequence_split.json</c> cases 1 and 10).
    /// </remarks>
    public BpeSplitStep? PreSplit { get; init; }

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
            || !string.Equals(EndOfWordSuffix, other.EndOfWordSuffix, StringComparison.Ordinal)
            || !string.Equals(ContinuingSubwordPrefix, other.ContinuingSubwordPrefix, StringComparison.Ordinal)
            || !string.Equals(UnkToken, other.UnkToken, StringComparison.Ordinal)
            || !string.Equals(PreTokenizerPattern, other.PreTokenizerPattern, StringComparison.Ordinal)
            || PreSplit != other.PreSplit
            || Vocab.Count != other.Vocab.Count
            || Merges.Count != other.Merges.Count
            || AddedTokens.Count != other.AddedTokens.Count)
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
            hash = (hash * 31) + (ByteLevel ? 1 : 0);
            hash = (hash * 31) + (AddPrefixSpace ? 1 : 0);
            hash = (hash * 31) + (IgnoreMerges ? 1 : 0);
            hash = (hash * 31) + (FuseUnk ? 1 : 0);
            hash = (hash * 31) + (EndOfWordSuffix is null ? 0 : StringComparer.Ordinal.GetHashCode(EndOfWordSuffix));
            hash = (hash * 31) + (ContinuingSubwordPrefix is null ? 0 : StringComparer.Ordinal.GetHashCode(ContinuingSubwordPrefix));
            hash = (hash * 31) + (UnkToken is null ? 0 : StringComparer.Ordinal.GetHashCode(UnkToken));
            hash = (hash * 31) + (PreTokenizerPattern is null ? 0 : StringComparer.Ordinal.GetHashCode(PreTokenizerPattern));
            return (hash * 31) + (PreSplit is null ? 0 : PreSplit.GetHashCode());
        }
    }

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
