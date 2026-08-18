using System.Text.Json;
using Lodestar.Internal.Persistence;
using Lodestar.Text.Persistence;

namespace Lodestar.Text.Vectorization;

public sealed partial class CountVectorizer
{
    private const string ArtifactName = "count-vectorizer";
    private const int ArtifactVersion = 1;

    /// <summary>
    /// Writes the fitted vectorizer — its options and its sorted vocabulary — to
    /// <paramref name="destination"/> as UTF-8 JSON.
    /// </summary>
    /// <remarks>
    /// The Lodestar equivalent of <c>pickle.dump</c> / <c>joblib.dump</c> on a fitted
    /// <c>sklearn.feature_extraction.text.CountVectorizer</c>, as versioned JSON rather than an executable
    /// pickle — see <see cref="ArtifactLoadOptions"/> and the "Saving a fitted model" section of
    /// <c>docs/guides/vectorization.md</c>.
    /// </remarks>
    /// <param name="destination">The stream to write to. It is flushed but never disposed — the caller owns it.</param>
    /// <exception cref="InvalidOperationException">The vectorizer has not been fitted.</exception>
    /// <exception cref="ArgumentNullException">the stream or path is null.</exception>
    /// <exception cref="IOException">the stream or file system refuses the write.</exception>
    public void Save(Stream destination) =>
        ArtifactIo.Save(destination, ArtifactName, ArtifactVersion, WriteArtifactBody);

    /// <summary>Writes the fitted vectorizer to <paramref name="path"/>, replacing any existing file.</summary>
    /// <remarks>Equivalent to <c>joblib.dump(vectorizer, path)</c>; the file is UTF-8 without a byte-order mark.</remarks>
    /// <exception cref="InvalidOperationException">The vectorizer has not been fitted.</exception>
    /// <exception cref="ArgumentNullException">the stream or path is null.</exception>
    /// <exception cref="IOException">the stream or file system refuses the write.</exception>
    public void Save(string path)
    {
        // Checked before opening: OpenWrite truncates, so a check any later would
        // destroy a good artifact and leave a half-written header behind.
        EnsureFitted();
        using FileStream file = JsonArtifact.OpenWrite(path);
        Save(file);
    }

    /// <summary>Asynchronous counterpart of <see cref="Save(Stream)"/>.</summary>
    /// <param name="destination">The stream to write to; never disposed by this method.</param>
    /// <exception cref="ArgumentNullException">the stream is null.</exception>
    /// <exception cref="InvalidOperationException">nothing has been fitted yet.</exception>
    /// <exception cref="OperationCanceledException">the token is cancelled.</exception>
    /// <param name="cancellationToken">Cancels the write.</param>
    public Task SaveAsync(Stream destination, CancellationToken cancellationToken = default) =>
        ArtifactIo.SaveAsync(destination, ArtifactName, ArtifactVersion, WriteArtifactBody, cancellationToken);

    /// <summary>
    /// Reads a vectorizer previously written by <see cref="Save(Stream)"/>, ready
    /// to <see cref="Transform"/> without being fitted again.
    /// </summary>
    /// <remarks>
    /// The Lodestar equivalent of <c>pickle.load(f)</c> / <c>joblib.load</c> for a
    /// fitted <c>sklearn.feature_extraction.text.CountVectorizer</c> — with the
    /// difference that this reads data, never code, and enforces the bounds in
    /// <paramref name="options"/>.
    /// </remarks>
    /// <param name="source">The stream to read from; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="InvalidDataException">The artifact is malformed, of the wrong kind, of an unsupported version, or exceeds a limit.</exception>
    /// <exception cref="ArgumentNullException">the stream or path is null.</exception>
    public static CountVectorizer Load(Stream source, ArtifactLoadOptions? options = null)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        return FromPayload(JsonArtifact.ReadAllBytes(source, limits), limits);
    }

    /// <summary>Reads a vectorizer from <paramref name="path"/>.</summary>
    /// <param name="path">The artifact file, as written by <see cref="Save(string)"/>.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="InvalidDataException">The artifact is malformed, of the wrong kind, of an unsupported version, or exceeds a limit.</exception>
    /// <exception cref="ArgumentNullException">the stream or path is null.</exception>
    public static CountVectorizer Load(string path, ArtifactLoadOptions? options = null)
    {
        using FileStream file = JsonArtifact.OpenRead(path);
        return Load(file, options);
    }

    /// <summary>Asynchronous counterpart of <see cref="Load(Stream, ArtifactLoadOptions?)"/>.</summary>
    /// <param name="source">The stream to read from; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="ArgumentNullException">the stream is null.</exception>
    /// <exception cref="InvalidDataException">the content is not a saved vectorizer, or exceeds a bound.</exception>
    /// <exception cref="OperationCanceledException">the token is cancelled.</exception>
    /// <param name="cancellationToken">Cancels the read.</param>
    public static async Task<CountVectorizer> LoadAsync(
        Stream source,
        ArtifactLoadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        ReadOnlyMemory<byte> payload = await JsonArtifact.ReadAllBytesAsync(source, limits, cancellationToken).ConfigureAwait(false);
        return FromPayload(payload, limits);
    }

    /// <summary>The options this vectorizer was built with — needed by <see cref="TfidfVectorizer"/>'s artifact.</summary>
    internal CountVectorizerOptions Options => _options;

    /// <summary>The fitted feature names, or an empty array if never fitted.</summary>
    internal string[] FittedFeatureNames => _featureNames;

    /// <summary>Whether <see cref="Fit"/> has run.</summary>
    internal bool IsFitted => _vocabulary is not null;

    /// <summary>Rebuilds the fitted state from an artifact's already-validated vocabulary.</summary>
    internal void RestoreVocabulary(string[] sortedFeatureNames)
    {
        var vocabulary = new Dictionary<string, int>(sortedFeatureNames.Length, StringComparer.Ordinal);
        for (int i = 0; i < sortedFeatureNames.Length; i++)
        {
            vocabulary[sortedFeatureNames[i]] = i;
        }
        _featureNames = sortedFeatureNames;
        _vocabulary = vocabulary;
    }

    /// <summary>Writes the fitted body of a <c>datanet/count-vectorizer</c> artifact.</summary>
    internal void WriteArtifactBody(Utf8JsonWriter writer)
    {
        EnsureFitted();
        VectorizerOptionsJson.Write(writer, "options", _options);
        writer.WriteNumber(FeatureVocabularyJson.FeatureCountProperty, _featureNames.Length);
        FeatureVocabularyJson.WriteVocabulary(writer, _featureNames);
    }

    private static CountVectorizer FromPayload(ReadOnlyMemory<byte> payload, in ArtifactLimits limits)
    {
        try
        {
            return Parse(payload, limits);
        }
        catch (JsonException e)
        {
            throw ArtifactIo.Malformed(ArtifactName, e);
        }
    }

    private static CountVectorizer Parse(ReadOnlyMemory<byte> payload, in ArtifactLimits limits)
    {
        Utf8JsonReader reader = ArtifactIo.CreateReader(payload.Span, ArtifactName, limits);
        var header = new ArtifactHeader(ArtifactName, ArtifactVersion);

        CountVectorizerOptions? options = null;
        string[]? vocabulary = null;
        int featureCount = -1;

        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            string name = reader.GetString()!;
            if (header.TryConsume(ref reader, name))
            {
                continue;
            }
            switch (name)
            {
                case "options":
                    options = VectorizerOptionsJson.ReadCount(ref reader, ArtifactName, limits);
                    break;
                case FeatureVocabularyJson.FeatureCountProperty:
                    featureCount = FeatureVocabularyJson.ReadFeatureCount(ref reader, ArtifactName, limits);
                    break;
                case FeatureVocabularyJson.VocabularyProperty:
                    vocabulary = FeatureVocabularyJson.ReadVocabulary(ref reader, ArtifactName, limits, featureCount);
                    break;
                default:
                    throw JsonArtifact.UnknownProperty(ArtifactName, name);
            }
        }

        ArtifactIo.EnsureEndOfDocument(ref reader, ArtifactName);
        header.EnsureComplete();
        if (options is null)
        {
            throw JsonArtifact.MissingProperty(ArtifactName, "options");
        }
        if (vocabulary is null)
        {
            throw JsonArtifact.MissingProperty(ArtifactName, FeatureVocabularyJson.VocabularyProperty);
        }
        if (featureCount < 0)
        {
            throw JsonArtifact.MissingProperty(ArtifactName, FeatureVocabularyJson.FeatureCountProperty);
        }
        FeatureVocabularyJson.EnsureDeclaredCount(ArtifactName, featureCount, vocabulary.Length, FeatureVocabularyJson.VocabularyProperty);

        var vectorizer = new CountVectorizer(options);
        vectorizer.RestoreVocabulary(vocabulary);
        return vectorizer;
    }
}
