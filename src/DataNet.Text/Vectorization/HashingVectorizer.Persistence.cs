using System.Text.Json;
using DataNet.Internal.Persistence;
using DataNet.Text.Persistence;

namespace DataNet.Text.Vectorization;

public sealed partial class HashingVectorizer
{
    private const string ArtifactName = "hashing-vectorizer";
    private const int ArtifactVersion = 1;

    /// <summary>Writes the vectorizer's configuration to <paramref name="destination"/> as UTF-8 JSON.</summary>
    /// <remarks>
    /// <para>
    /// The DataNet equivalent of <c>joblib.dump</c> on a
    /// <c>sklearn.feature_extraction.text.HashingVectorizer</c>. Hashing is
    /// stateless — there is no vocabulary to learn — but the configuration is not:
    /// a pipeline reloaded with a different <c>NumFeatures</c>, <c>AlternateSign</c>
    /// or analyzer produces different columns for the same document, and nothing
    /// downstream would notice. That is what this persists.
    /// </para>
    /// </remarks>
    /// <param name="destination">The stream to write to. It is flushed but never disposed — the caller owns it.</param>
    public void Save(Stream destination) =>
        ArtifactIo.Save(destination, ArtifactName, ArtifactVersion, WriteArtifactBody);

    /// <summary>Writes the vectorizer's configuration to <paramref name="path"/>, replacing any existing file.</summary>
    /// <remarks>Equivalent to <c>joblib.dump(vectorizer, path)</c>; the file is UTF-8 without a byte-order mark.</remarks>
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

    /// <summary>Reads a vectorizer configuration previously written by <see cref="Save(Stream)"/>.</summary>
    /// <remarks>The DataNet equivalent of <c>joblib.load</c> for a <c>sklearn.feature_extraction.text.HashingVectorizer</c>.</remarks>
    /// <param name="source">The stream to read from; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="InvalidDataException">The artifact is malformed, of the wrong kind, of an unsupported version, or exceeds a limit.</exception>
    public static HashingVectorizer Load(Stream source, ArtifactLoadOptions? options = null)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        return FromPayload(JsonArtifact.ReadAllBytes(source, limits), limits);
    }

    /// <summary>Reads a vectorizer configuration from <paramref name="path"/>.</summary>
    /// <param name="path">The artifact file, as written by <see cref="Save(string)"/>.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="InvalidDataException">The artifact is malformed, of the wrong kind, of an unsupported version, or exceeds a limit.</exception>
    public static HashingVectorizer Load(string path, ArtifactLoadOptions? options = null)
    {
        using FileStream file = JsonArtifact.OpenRead(path);
        return Load(file, options);
    }

    /// <summary>Asynchronous counterpart of <see cref="Load(Stream, ArtifactLoadOptions?)"/>.</summary>
    /// <param name="source">The stream to read from; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    public static async Task<HashingVectorizer> LoadAsync(
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
        VectorizerOptionsJson.Write(writer, "options", _options.Count);
        writer.WriteNumber("numFeatures", _options.NumFeatures);
        writer.WriteBoolean("alternateSign", _options.AlternateSign);
        VectorizerOptionsJson.WriteNorm(writer, "norm", _options.Norm);
    }

    private static HashingVectorizer FromPayload(byte[] payload, in ArtifactLimits limits)
    {
        Utf8JsonReader reader = ArtifactIo.CreateReader(payload, ArtifactName, limits);
        var header = new ArtifactHeader(ArtifactName, ArtifactVersion);

        var result = new HashingVectorizerOptions();
        CountVectorizerOptions? countOptions = null;

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
                case "numFeatures":
                    result = result with { NumFeatures = JsonArtifact.ReadInt32(ref reader, ArtifactName, name) };
                    break;
                case "alternateSign":
                    result = result with { AlternateSign = JsonArtifact.ReadBoolean(ref reader, ArtifactName, name) };
                    break;
                case "norm":
                    result = result with
                    {
                        Norm = VectorizerOptionsJson.ParseNorm(JsonArtifact.ReadNullableString(ref reader, ArtifactName, name), ArtifactName),
                    };
                    break;
                default:
                    throw JsonArtifact.UnknownProperty(ArtifactName, name);
            }
        }

        ArtifactIo.EnsureEndOfDocument(ref reader, ArtifactName);
        header.EnsureComplete();
        if (countOptions is null)
        {
            throw JsonArtifact.MissingProperty(ArtifactName, "options");
        }
        if (result.NumFeatures < 1)
        {
            throw JsonArtifact.Inconsistent(ArtifactName, $"numFeatures must be at least 1 but is {result.NumFeatures}.");
        }
        return new HashingVectorizer(result with { Count = countOptions });
    }
}
