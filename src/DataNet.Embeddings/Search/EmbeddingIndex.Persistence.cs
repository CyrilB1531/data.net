using System.Text.Json;
using DataNet.Embeddings.Persistence;
using DataNet.Internal.Persistence;

namespace DataNet.Embeddings.Search;

public sealed partial class EmbeddingIndex
{
    private const string ArtifactName = "embedding-index";
    private const int ArtifactVersion = 1;
    private const string DimensionProperty = "dimension";
    private const string NormalizeProperty = "normalize";
    private const string CountProperty = "count";
    private const string IdsProperty = "ids";
    private const string VectorsProperty = "vectors";

    /// <summary>
    /// Writes the index — configuration, ids and the vector block — to
    /// <paramref name="destination"/> as UTF-8 JSON.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Building an index runs an encoder over every document in the corpus. This is
    /// what makes that a one-off: the vectors are written as one base64 string of
    /// raw little-endian IEEE-754 bits, so a reloaded index scores bit for bit what
    /// the original scored.
    /// </para>
    /// <para>
    /// A non-finite component is refused here even though <see cref="Add(ReadOnlySpan{float})"/> accepts
    /// one. An artifact is read back by callers who will never see the code that
    /// built it, and a score that is <c>NaN</c> forever is worse than a save that
    /// failed at the point the broken vector was still in reach.
    /// </para>
    /// </remarks>
    /// <param name="destination">The stream to write to. Flushed but never disposed — the caller owns it.</param>
    /// <exception cref="InvalidDataException">A vector holds a non-finite component.</exception>
    public void Save(Stream destination) =>
        ArtifactIo.Save(destination, ArtifactName, ArtifactVersion, WriteArtifactBody);

    /// <summary>Writes the index to <paramref name="path"/>, replacing any existing file.</summary>
    /// <param name="path">The file to write. UTF-8 without a byte-order mark.</param>
    /// <exception cref="InvalidDataException">A vector holds a non-finite component.</exception>
    public void Save(string path)
    {
        // Before opening: OpenWrite truncates, so a refused save would otherwise
        // destroy a good artifact and leave a header where it used to be.
        EnsureFinite();
        using FileStream file = JsonArtifact.OpenWrite(path);
        Save(file);
    }

    /// <summary>Asynchronous counterpart of <see cref="Save(Stream)"/>.</summary>
    /// <param name="destination">The stream to write to; never disposed by this method.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <exception cref="InvalidDataException">A vector holds a non-finite component.</exception>
    public Task SaveAsync(Stream destination, CancellationToken cancellationToken = default) =>
        ArtifactIo.SaveAsync(destination, ArtifactName, ArtifactVersion, WriteArtifactBody, cancellationToken);

    private void WriteArtifactBody(Utf8JsonWriter writer)
    {
        EnsureFinite();
        writer.WriteNumber(DimensionProperty, _dim);
        writer.WriteBoolean(NormalizeProperty, _normalize);

        // Written before the block it describes, so a reader sizes its buffer from a
        // value it has already bounded rather than from the file's appetite.
        writer.WriteNumber(CountProperty, _count);

        if (_ids is not null)
        {
            writer.WriteStartArray(IdsProperty);
            for (int i = 0; i < _count; i++)
            {
                string? id = IdAt(i);
                if (id is null)
                {
                    writer.WriteNullValue();
                }
                else
                {
                    writer.WriteStringValue(id);
                }
            }
            writer.WriteEndArray();
        }

        Base64Numbers.WriteSingles(writer, VectorsProperty, _data.AsSpan(0, _length));
    }

    /// <summary>
    /// Reads an index previously written by <see cref="Save(Stream)"/>, ready to
    /// <see cref="Search"/> without embedding the corpus again.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Vectors are restored exactly as they were stored, never replayed through
    /// <see cref="Add(ReadOnlySpan{float})"/>: they were normalized on insertion if the index normalized
    /// at all, and normalizing them a second time would move their bits.
    /// </para>
    /// <para>
    /// The normalization flag comes from the file and cannot be supplied by the
    /// caller. Reloading vectors under the other setting would produce scores that
    /// are quietly wrong rather than obviously so.
    /// </para>
    /// </remarks>
    /// <param name="source">The stream to read from; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="InvalidDataException">The artifact is malformed, of the wrong kind, of an unsupported version, internally inconsistent, or exceeds a limit.</exception>
    public static EmbeddingIndex Load(Stream source, ArtifactLoadOptions? options = null)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        return FromPayload(JsonArtifact.ReadAllBytes(source, limits), limits);
    }

    /// <summary>Reads an index from <paramref name="path"/>.</summary>
    /// <param name="path">The artifact file, as written by <see cref="Save(string)"/>.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="InvalidDataException">The artifact is malformed, of the wrong kind, of an unsupported version, internally inconsistent, or exceeds a limit.</exception>
    public static EmbeddingIndex Load(string path, ArtifactLoadOptions? options = null)
    {
        using FileStream file = JsonArtifact.OpenRead(path);
        return Load(file, options);
    }

    /// <summary>Asynchronous counterpart of <see cref="Load(Stream, ArtifactLoadOptions?)"/>.</summary>
    /// <param name="source">The stream to read from; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <exception cref="InvalidDataException">The artifact is malformed, of the wrong kind, of an unsupported version, internally inconsistent, or exceeds a limit.</exception>
    public static async Task<EmbeddingIndex> LoadAsync(
        Stream source,
        ArtifactLoadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        byte[] payload = await JsonArtifact.ReadAllBytesAsync(source, limits, cancellationToken).ConfigureAwait(false);
        return FromPayload(payload, limits);
    }

    private static EmbeddingIndex FromPayload(byte[] payload, in ArtifactLimits limits)
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

    private static EmbeddingIndex Parse(byte[] payload, in ArtifactLimits limits)
    {
        Utf8JsonReader reader = ArtifactIo.CreateReader(payload, ArtifactName, limits);
        var header = new ArtifactHeader(ArtifactName, ArtifactVersion);

        int? dimension = null;
        int? count = null;
        bool? normalize = null;
        string?[]? ids = null;
        float[]? vectors = null;

        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            string name = reader.GetString()!;
            if (header.TryConsume(ref reader, name))
            {
                continue;
            }
            switch (name)
            {
                case DimensionProperty:
                    dimension = JsonArtifact.ReadInt32(ref reader, ArtifactName, DimensionProperty);
                    break;
                case NormalizeProperty:
                    normalize = JsonArtifact.ReadBoolean(ref reader, ArtifactName, NormalizeProperty);
                    break;
                case CountProperty:
                    count = ReadCount(ref reader, limits);
                    break;
                case IdsProperty:
                    ids = ReadIds(ref reader, limits, count);
                    break;
                case VectorsProperty:
                    vectors = Base64Numbers.ReadSingles(ref reader, ArtifactName, VectorsProperty);
                    break;
                default:
                    throw JsonArtifact.UnknownProperty(ArtifactName, name);
            }
        }

        ArtifactIo.EnsureEndOfDocument(ref reader, ArtifactName);
        header.EnsureComplete();
        return Restore(dimension, count, normalize, ids, vectors);
    }

    private static int ReadCount(ref Utf8JsonReader reader, in ArtifactLimits limits)
    {
        int count = JsonArtifact.ReadInt32(ref reader, ArtifactName, CountProperty);
        if (count < 0)
        {
            throw JsonArtifact.Inconsistent(ArtifactName, $"'{CountProperty}' is negative ({count}).");
        }

        // A count is a count of vectors — the vocabulary-adjacent scale
        // MaxArrayLength was written for. Unlike the vectors block below, this
        // value sizes nothing by itself until it is multiplied by the dimension
        // (see Restore), so it keeps the bound the block deliberately no longer has.
        limits.CheckArrayLength(count, CountProperty);
        return count;
    }

    /// <summary>Reads the id array, sized from the declared count when it arrived first.</summary>
    /// <remarks>
    /// The reader accepts reordered properties, so <c>ids</c> can precede the
    /// <c>count</c> that would have sized this buffer. The ceiling keeps a declared
    /// count from sizing the allocation on its own: the file has to actually deliver
    /// the entries before the buffer grows past it.
    /// </remarks>
    private static string?[] ReadIds(ref Utf8JsonReader reader, in ArtifactLimits limits, int? declaredCount)
    {
        JsonArtifact.ReadStartArray(ref reader, ArtifactName, IdsProperty);

        string?[] ids = new string?[
            declaredCount is int declared && declared > 0 ? Math.Min(declared, MaxPreallocatedIds) : 0];
        int read = 0;
        while (reader.Read() && reader.TokenType is JsonTokenType.String or JsonTokenType.Null)
        {
            string? id = reader.TokenType == JsonTokenType.Null ? null : reader.GetString();
            if (id is not null)
            {
                limits.CheckTokenLength(id.Length);
            }

            // Checked against the prospective count before the array grows to hold
            // it: doubling first and checking after would let the resize itself
            // reach roughly twice the limit before the exception fires.
            limits.CheckArrayLength(read + 1L, IdsProperty);
            if (read == ids.Length)
            {
                Array.Resize(ref ids, ids.Length == 0 ? 4 : ids.Length * 2);
            }
            ids[read++] = id;
        }
        if (reader.TokenType != JsonTokenType.EndArray)
        {
            throw JsonArtifact.UnexpectedToken(ArtifactName, IdsProperty, reader.TokenType);
        }

        if (read != ids.Length)
        {
            Array.Resize(ref ids, read);
        }
        return ids;
    }

    private const int MaxPreallocatedIds = 65_536;

    private static EmbeddingIndex Restore(
        int? dimension,
        int? count,
        bool? normalize,
        string?[]? ids,
        float[]? vectors)
    {
        if (dimension is not int dim)
        {
            throw JsonArtifact.MissingProperty(ArtifactName, DimensionProperty);
        }
        if (normalize is not bool normalizeFlag)
        {
            throw JsonArtifact.MissingProperty(ArtifactName, NormalizeProperty);
        }
        if (count is not int itemCount)
        {
            throw JsonArtifact.MissingProperty(ArtifactName, CountProperty);
        }
        if (vectors is null)
        {
            throw JsonArtifact.MissingProperty(ArtifactName, VectorsProperty);
        }
        if (dim < 1)
        {
            throw JsonArtifact.Inconsistent(
                ArtifactName,
                $"'{DimensionProperty}' must be at least 1, but the file declares {dim}.");
        }

        long expected = (long)itemCount * dim;
        if (vectors.LongLength != expected)
        {
            throw JsonArtifact.Inconsistent(
                ArtifactName,
                $"'{CountProperty}' is {itemCount} and '{DimensionProperty}' is {dim}, "
                + $"which needs {expected} values, but '{VectorsProperty}' holds {vectors.LongLength}.");
        }
        if (ids is not null && ids.Length != itemCount)
        {
            throw JsonArtifact.Inconsistent(
                ArtifactName,
                $"'{CountProperty}' is {itemCount} but '{IdsProperty}' holds {ids.Length} entries.");
        }
        EnsureFinite(vectors, dim);

        var index = new EmbeddingIndex(dim, normalizeFlag);
        index._data = vectors;
        index._length = vectors.Length;
        index._count = itemCount;
        index._ids = ids;
        return index;
    }

    /// <summary>Throws unless every stored component is a finite number.</summary>
    private void EnsureFinite() => EnsureFinite(_data.AsSpan(0, _length), _dim);

    private static void EnsureFinite(ReadOnlySpan<float> data, int dimension)
    {
        for (int i = 0; i < data.Length; i++)
        {
            float value = data[i];
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new InvalidDataException(
                    $"Cannot persist a non-finite value at item {i / dimension}, component {i % dimension}. "
                    + "Add accepts such a vector; the artifact does not, because it would score NaN "
                    + "for every query a reloaded index is ever given.");
            }
        }
    }
}
