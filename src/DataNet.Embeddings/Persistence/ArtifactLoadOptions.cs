using DataNet.Internal.Persistence;

namespace DataNet.Embeddings.Persistence;

/// <summary>
/// The bounds applied when loading a vocabulary from disk.
/// </summary>
/// <remarks>
/// <para>
/// A pretrained vocabulary is a downloaded file, and every count it declares
/// sizes a buffer on load. These limits turn a hostile or corrupt
/// <c>vocab.txt</c>, <c>tokenizer.json</c> or <c>spiece.model</c> into an
/// <see cref="InvalidDataException"/> naming the limit and the offending value,
/// instead of an <see cref="OutOfMemoryException"/>. The defaults are generous
/// for real models — BERT ships 30 522 tokens, XLM-R 250 002 — so raising them
/// should be a deliberate act.
/// </para>
/// <para>
/// There is no equivalent in <c>tokenizers</c> or <c>sentencepiece</c>: both
/// trust the file. This is a deliberate addition, not a port.
/// </para>
/// <para>
/// The type is declared separately in each DataNet package rather than shared,
/// so that referencing both <c>DataNet.Text</c> and <c>DataNet.Embeddings</c>
/// never produces an ambiguous name.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var strict = new ArtifactLoadOptions { MaxVocabularySize = 50_000 };
/// WordPieceVocabulary vocab = VocabTxtLoader.Load("vocab.txt", strict);
/// </code>
/// </example>
public sealed record ArtifactLoadOptions
{
    /// <summary>Maximum number of vocabulary entries accepted. Default 1 000 000.</summary>
    public int MaxVocabularySize { get; init; } = ArtifactLimits.DefaultMaxVocabularySize;

    /// <summary>Maximum length, in characters, of a single token. Default 1024.</summary>
    public int MaxTokenLength { get; init; } = ArtifactLimits.DefaultMaxTokenLength;

    /// <summary>Maximum JSON nesting depth accepted. Default 32.</summary>
    public int MaxJsonDepth { get; init; } = ArtifactLimits.DefaultMaxJsonDepth;

    /// <summary>Maximum total number of bytes read from the source. Default 256 MiB.</summary>
    public long MaxTotalBytes { get; init; } = ArtifactLimits.DefaultMaxTotalBytes;

    /// <summary>
    /// Maximum length of any single array in the source, except an
    /// <c>EmbeddingIndex</c>'s vector block, which <see cref="MaxTotalBytes"/>
    /// bounds instead. Default 1 000 000.
    /// </summary>
    public int MaxArrayLength { get; init; } = ArtifactLimits.DefaultMaxArrayLength;

    internal ArtifactLimits ToLimits() =>
        new(MaxVocabularySize, MaxTokenLength, MaxJsonDepth, MaxTotalBytes, MaxArrayLength);

    internal static ArtifactLimits LimitsOf(ArtifactLoadOptions? options) =>
        options is null ? ArtifactLimits.Default : options.ToLimits();
}
