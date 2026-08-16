using Lodestar.Internal.Persistence;

namespace Lodestar.Text.Persistence;

/// <summary>
/// The bounds applied when loading a persisted Lodestar artifact.
/// </summary>
/// <remarks>
/// See the guide's "Reading a file you did not write" section
/// (<c>docs/guides/vectorization.md</c>) for the defaults and a worked example,
/// and <c>docs/decisions/0011-persistence-format.md</c> for the <c>pickle.load</c>
/// comparison and why this type is declared separately from
/// <c>Lodestar.Embeddings</c>'s rather than shared.
/// </remarks>
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
