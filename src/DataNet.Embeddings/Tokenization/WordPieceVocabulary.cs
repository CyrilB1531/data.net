namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// A pretrained WordPiece vocabulary, as read from a <c>vocab.txt</c> or the
/// <c>model</c> section of a <c>tokenizer.json</c>.
/// </summary>
/// <remarks>
/// Carries the settings that change tokenization and that a caller building the
/// table by hand would have to guess: which token stands for the unknown, what
/// marks a continuation piece, and whether the model was trained lowercased.
/// Getting any of them wrong silently produces embeddings for a different model.
/// </remarks>
/// <param name="Vocab">Token to id, in the order the file declared them.</param>
/// <param name="UnkToken">The unknown token, e.g. <c>[UNK]</c>; present in <paramref name="Vocab"/>.</param>
/// <param name="ContinuationPrefix">Marks non-initial word pieces, e.g. <c>##</c>.</param>
/// <param name="Lowercase">Whether text is lowercased before tokenizing.</param>
public sealed record WordPieceVocabulary(
    IReadOnlyDictionary<string, int> Vocab,
    string UnkToken,
    string ContinuationPrefix,
    bool Lowercase)
{
    /// <summary>The <c>added_tokens</c> table, matched as literal text ahead of the model.</summary>
    /// <remarks>
    /// Not folded into <see cref="Vocab"/>: a folded entry is matchable as a whole
    /// word only, which is a different tokenizer as soon as an entry carries a
    /// matching flag. See <c>docs/decisions/0022-added-token-matching-flags.md</c>.
    /// </remarks>
    public IReadOnlyList<AddedToken> AddedTokens { get; init; } = [];

    /// <summary>Number of entries in the vocabulary.</summary>
    public int Count => Vocab.Count;

    /// <summary>
    /// Compares the settings, then every token-to-id mapping and every
    /// <see cref="AddedTokens"/> entry, in order.
    /// </summary>
    /// <remarks>
    /// The generated equality would compare <see cref="Vocab"/> by reference, so two
    /// vocabularies loaded from the same file would be unequal. A record advertises
    /// value equality; this is what it takes to deliver it.
    /// </remarks>
    public bool Equals(WordPieceVocabulary? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        if (other is null
            || Lowercase != other.Lowercase
            || !string.Equals(UnkToken, other.UnkToken, StringComparison.Ordinal)
            || !string.Equals(ContinuationPrefix, other.ContinuationPrefix, StringComparison.Ordinal)
            || Vocab.Count != other.Vocab.Count
            || AddedTokens.Count != other.AddedTokens.Count)
        {
            return false;
        }
        for (int i = 0; i < AddedTokens.Count; i++)
        {
            if (!AddedTokens[i].Equals(other.AddedTokens[i]))
            {
                return false;
            }
        }
        foreach (KeyValuePair<string, int> entry in Vocab)
        {
            if (!other.Vocab.TryGetValue(entry.Key, out int id) || id != entry.Value)
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Hashes the scalars and the entry count, which is O(1).</summary>
    /// <remarks>
    /// Equal vocabularies necessarily agree on all of these; unequal ones are
    /// allowed to collide. Hashing thirty thousand entries would turn the operation
    /// that must stay cheap into the expensive one.
    /// </remarks>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 31) + Vocab.Count;
            hash = (hash * 31) + AddedTokens.Count;
            hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(UnkToken);
            hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(ContinuationPrefix);
            return (hash * 31) + (Lowercase ? 1 : 0);
        }
    }
}
