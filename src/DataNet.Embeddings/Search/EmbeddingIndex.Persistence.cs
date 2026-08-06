using System.Text.Json;
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

    /// <summary>Throws unless every stored component is a finite number.</summary>
    private void EnsureFinite()
    {
        ReadOnlySpan<float> data = _data.AsSpan(0, _length);
        for (int i = 0; i < data.Length; i++)
        {
            float value = data[i];
            if (float.IsNaN(value) || float.IsInfinity(value))
            {
                throw new InvalidDataException(
                    $"Cannot persist a non-finite value at item {i / _dim}, component {i % _dim}. "
                    + "Add accepts such a vector; the artifact does not, because it would score NaN "
                    + "for every query a reloaded index is ever given.");
            }
        }
    }
}
