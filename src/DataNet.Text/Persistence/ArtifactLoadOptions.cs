using DataNet.Internal.Persistence;

namespace DataNet.Text.Persistence;

/// <summary>
/// The bounds applied when loading a persisted DataNet artifact.
/// </summary>
/// <remarks>
/// <para>
/// A saved model is a file, and a file can come from anywhere: every count it
/// declares sizes a buffer on load. These limits turn a hostile or corrupt
/// artifact into an <see cref="InvalidDataException"/> naming the limit and the
/// offending value, instead of an <see cref="OutOfMemoryException"/> or a hang.
/// The defaults are generous enough for real models — a 1 000 000-term
/// vocabulary is far past what scikit-learn users fit in practice — so raising
/// them should be a deliberate act.
/// </para>
/// <para>
/// There is no Python equivalent: <c>pickle.load</c>, the usual way a fitted
/// scikit-learn model is restored, executes arbitrary code by design and has no
/// such bounds. This type is the reason DataNet's format can be pointed at an
/// untrusted file at all.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var strict = new ArtifactLoadOptions { MaxVocabularySize = 50_000, MaxTotalBytes = 8L * 1024 * 1024 };
/// TfidfVectorizer model = TfidfVectorizer.Load("model.json", strict);
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

    /// <summary>Maximum length of any single JSON array in the artifact. Default 1 000 000.</summary>
    public int MaxArrayLength { get; init; } = ArtifactLimits.DefaultMaxArrayLength;

    internal ArtifactLimits ToLimits() =>
        new(MaxVocabularySize, MaxTokenLength, MaxJsonDepth, MaxTotalBytes, MaxArrayLength);

    internal static ArtifactLimits LimitsOf(ArtifactLoadOptions? options) =>
        options is null ? ArtifactLimits.Default : options.ToLimits();
}
