using System.Text.Json;
using DataNet.Internal.Persistence;

namespace DataNet.Text.Persistence;

/// <summary>
/// The save/load skeleton every DataNet.Text artifact shares: open the object,
/// write the header, let the artifact write its body, and on the way back read
/// the whole (byte-capped) payload into one buffer for a single reader pass.
/// </summary>
/// <remarks>
/// Streams passed in by the caller are never disposed here — the caller owns
/// them. The <c>string path</c> overloads on each artifact own the
/// <see cref="FileStream"/> they open, and dispose it.
/// </remarks>
internal static class ArtifactIo
{
    public static void Save(Stream destination, string artifact, int version, Action<Utf8JsonWriter> writeBody)
    {
        Guard.NotNull(destination);
        using var writer = new Utf8JsonWriter(destination, JsonArtifact.WriterOptions);
        WriteDocument(writer, artifact, version, writeBody);
        writer.Flush();
    }

    public static async Task SaveAsync(
        Stream destination,
        string artifact,
        int version,
        Action<Utf8JsonWriter> writeBody,
        CancellationToken cancellationToken)
    {
        Guard.NotNull(destination);

        // Utf8JsonWriter flushes to its destination synchronously whenever its
        // internal buffer fills, so writing straight to the stream would block
        // regardless of the await. Compose in memory, then do one async write.
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, JsonArtifact.WriterOptions))
        {
            WriteDocument(writer, artifact, version, writeBody);
            writer.Flush();
        }

        byte[] payload = buffer.GetBuffer();
        await destination.WriteAsync(payload, 0, (int)buffer.Length, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Creates a reader over <paramref name="payload"/> positioned on the artifact's opening brace.</summary>
    public static Utf8JsonReader CreateReader(byte[] payload, string artifact, in ArtifactLimits limits)
    {
        var reader = new Utf8JsonReader(payload, JsonArtifact.ReaderOptions(limits));
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidDataException($"A '{ArtifactHeader.SchemaFor(artifact)}' artifact must be a JSON object.");
        }
        return reader;
    }

    /// <summary>Checks that nothing but whitespace follows the artifact's closing brace.</summary>
    public static void EnsureEndOfDocument(ref Utf8JsonReader reader, string artifact)
    {
        if (reader.TokenType != JsonTokenType.EndObject)
        {
            throw JsonArtifact.Truncated(artifact);
        }
        if (reader.Read())
        {
            throw new InvalidDataException($"Trailing content after the end of a '{ArtifactHeader.SchemaFor(artifact)}' artifact.");
        }
    }

    private static void WriteDocument(Utf8JsonWriter writer, string artifact, int version, Action<Utf8JsonWriter> writeBody)
    {
        writer.WriteStartObject();
        ArtifactHeader.Write(writer, artifact, version);
        writeBody(writer);
        writer.WriteEndObject();
    }
}
