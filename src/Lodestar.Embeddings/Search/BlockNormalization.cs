namespace Lodestar.Embeddings.Search;

/// <summary>How a block handed to a bulk ingest relates to the index's normalization.</summary>
/// <remarks>
/// One argument rather than a <c>normalize</c> flag beside an <c>alreadyNormalized</c> one: the
/// index's flag governs the query as well as the store, so a pair would make a fourth combination
/// representable that means nothing. <see cref="Normalize"/> is the zero value, so an accidental
/// <c>default</c> yields the correct-but-slower behaviour rather than a silently wrong score.
/// </remarks>
public enum BlockNormalization
{
    /// <summary>
    /// The index normalizes, and what it stores is normalized — the copy for
    /// <see cref="EmbeddingIndex.FromBlock"/>, the caller's own array for
    /// <see cref="EmbeddingIndex.FromOwnedBlock"/>.
    /// </summary>
    Normalize,

    /// <summary>
    /// The index normalizes and the block already is, so it is stored bit for bit. This is a
    /// promise the caller keeps: an unnormalized block taken this way scores wrong and raises
    /// nothing.
    /// </summary>
    AlreadyNormalized,

    /// <summary>The index does not normalize, on insertion or on query.</summary>
    Off,
}
