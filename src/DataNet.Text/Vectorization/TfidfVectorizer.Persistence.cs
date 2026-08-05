using System.Text.Json;
using DataNet.Internal.Persistence;
using DataNet.Text.Persistence;

namespace DataNet.Text.Vectorization;

public sealed partial class TfidfVectorizer
{
    private const string ArtifactName = "tfidf-vectorizer";
    private const int ArtifactVersion = 1;

    /// <summary>
    /// Writes the fitted vectorizer — options, sorted vocabulary and idf vector —
    /// to <paramref name="destination"/> as UTF-8 JSON.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The DataNet equivalent of <c>pickle.dump(vectorizer, f)</c> or
    /// <c>joblib.dump</c> on a fitted
    /// <c>sklearn.feature_extraction.text.TfidfVectorizer</c>. Reloading and
    /// transforming reproduces the original matrix element for element: idf
    /// weights are written in the invariant <c>"G17"</c> form, which round-trips a
    /// <see cref="double"/> exactly.
    /// </para>
    /// <para>The idf vector is always written, even when <c>UseIdf</c> is off, so the artifact stays lossless.</para>
    /// </remarks>
    /// <param name="destination">The stream to write to. It is flushed but never disposed — the caller owns it.</param>
    /// <exception cref="InvalidOperationException">The vectorizer has not been fitted.</exception>
    public void Save(Stream destination) =>
        ArtifactIo.Save(destination, ArtifactName, ArtifactVersion, WriteArtifactBody);

    /// <summary>Writes the fitted vectorizer to <paramref name="path"/>, replacing any existing file.</summary>
    /// <remarks>Equivalent to <c>joblib.dump(vectorizer, path)</c>; the file is UTF-8 without a byte-order mark.</remarks>
    /// <exception cref="InvalidOperationException">The vectorizer has not been fitted.</exception>
    public void Save(string path)
    {
        using FileStream file = JsonArtifact.OpenWrite(path);
        Save(file);
    }

    /// <summary>Asynchronous counterpart of <see cref="Save(Stream)"/>.</summary>
    /// <param name="destination">The stream to write to; never disposed by this method.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    public Task SaveAsync(Stream destination, CancellationToken cancellationToken = default) =>
        ArtifactIo.SaveAsync(destination, ArtifactName, ArtifactVersion, WriteArtifactBody, cancellationToken);

    /// <summary>
    /// Reads a vectorizer previously written by <see cref="Save(Stream)"/>, ready
    /// to <see cref="Transform"/> without being fitted again.
    /// </summary>
    /// <remarks>
    /// The DataNet equivalent of <c>pickle.load(f)</c> / <c>joblib.load</c> for a
    /// fitted <c>sklearn.feature_extraction.text.TfidfVectorizer</c> — reading
    /// data rather than code, under the bounds in <paramref name="options"/>.
    /// </remarks>
    /// <param name="source">The stream to read from; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="InvalidDataException">The artifact is malformed, of the wrong kind, of an unsupported version, or exceeds a limit.</exception>
    public static TfidfVectorizer Load(Stream source, ArtifactLoadOptions? options = null)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        return FromPayload(JsonArtifact.ReadAllBytes(source, limits), limits);
    }

    /// <summary>Reads a vectorizer from <paramref name="path"/>.</summary>
    /// <param name="path">The artifact file, as written by <see cref="Save(string)"/>.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="InvalidDataException">The artifact is malformed, of the wrong kind, of an unsupported version, or exceeds a limit.</exception>
    public static TfidfVectorizer Load(string path, ArtifactLoadOptions? options = null)
    {
        using FileStream file = JsonArtifact.OpenRead(path);
        return Load(file, options);
    }

    /// <summary>Asynchronous counterpart of <see cref="Load(Stream, ArtifactLoadOptions?)"/>.</summary>
    /// <param name="source">The stream to read from; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    public static async Task<TfidfVectorizer> LoadAsync(
        Stream source,
        ArtifactLoadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        byte[] payload = await JsonArtifact.ReadAllBytesAsync(source, limits, cancellationToken).ConfigureAwait(false);
        return FromPayload(payload, limits);
    }

    private void WriteArtifactBody(Utf8JsonWriter writer)
    {
        if (!_counts.IsFitted || _tfidf.FittedIdf is not { } idf)
        {
            throw new InvalidOperationException("The vectorizer has not been fitted. Call Fit or FitTransform first.");
        }

        string[] featureNames = _counts.FittedFeatureNames;
        VectorizerOptionsJson.Write(writer, "options", _counts.Options);
        VectorizerOptionsJson.Write(writer, "tfidf", _tfidf.Options);
        writer.WriteNumber(FeatureVocabularyJson.FeatureCountProperty, featureNames.Length);
        FeatureVocabularyJson.WriteVocabulary(writer, featureNames);
        FeatureVocabularyJson.WriteIdf(writer, idf);
    }

    private static TfidfVectorizer FromPayload(byte[] payload, in ArtifactLimits limits)
    {
        Utf8JsonReader reader = ArtifactIo.CreateReader(payload, ArtifactName, limits);
        var header = new ArtifactHeader(ArtifactName, ArtifactVersion);

        CountVectorizerOptions? countOptions = null;
        TfidfOptions? tfidfOptions = null;
        string[]? vocabulary = null;
        double[]? idf = null;
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
                    countOptions = VectorizerOptionsJson.ReadCount(ref reader, ArtifactName, limits);
                    break;
                case "tfidf":
                    tfidfOptions = VectorizerOptionsJson.ReadTfidf(ref reader, ArtifactName);
                    break;
                case FeatureVocabularyJson.FeatureCountProperty:
                    featureCount = FeatureVocabularyJson.ReadFeatureCount(ref reader, ArtifactName, limits);
                    break;
                case FeatureVocabularyJson.VocabularyProperty:
                    vocabulary = FeatureVocabularyJson.ReadVocabulary(ref reader, ArtifactName, limits, featureCount);
                    break;
                case FeatureVocabularyJson.IdfProperty:
                    idf = FeatureVocabularyJson.ReadIdf(ref reader, ArtifactName, limits, featureCount);
                    break;
                default:
                    throw JsonArtifact.UnknownProperty(ArtifactName, name);
            }
        }

        ArtifactIo.EnsureEndOfDocument(ref reader, ArtifactName);
        header.EnsureComplete();
        RequirePresent(countOptions, "options");
        RequirePresent(tfidfOptions, "tfidf");
        RequirePresent(vocabulary, FeatureVocabularyJson.VocabularyProperty);
        RequirePresent(idf, FeatureVocabularyJson.IdfProperty);
        if (featureCount < 0)
        {
            throw JsonArtifact.MissingProperty(ArtifactName, FeatureVocabularyJson.FeatureCountProperty);
        }
        FeatureVocabularyJson.EnsureDeclaredCount(ArtifactName, featureCount, vocabulary!.Length, FeatureVocabularyJson.VocabularyProperty);
        FeatureVocabularyJson.EnsureDeclaredCount(ArtifactName, featureCount, idf!.Length, FeatureVocabularyJson.IdfProperty);

        var vectorizer = new TfidfVectorizer(new TfidfVectorizerOptions { Count = countOptions!, Tfidf = tfidfOptions! });
        vectorizer._counts.RestoreVocabulary(vocabulary);
        vectorizer._tfidf.RestoreIdf(idf);
        return vectorizer;
    }

    private static void RequirePresent(object? value, string propertyName)
    {
        if (value is null)
        {
            throw JsonArtifact.MissingProperty(ArtifactName, propertyName);
        }
    }
}
