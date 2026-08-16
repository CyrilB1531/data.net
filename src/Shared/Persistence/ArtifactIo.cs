using System.Text.Json;

namespace Lodestar.Internal.Persistence;

/// <summary>
/// The save/load skeleton every Lodestar.Text artifact shares: open the object,
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

        // Utf8JsonWriter flushes synchronously when its buffer fills, so writing
        // straight to the stream would block despite the await; compose in memory first.
        using var buffer = new MemoryStream();
        using (var writer = new Utf8JsonWriter(buffer, JsonArtifact.WriterOptions))
        {
            WriteDocument(writer, artifact, version, writeBody);
            // CA1849 / SonarLint S6966: the destination here is the MemoryStream above, whose
            // FlushAsync performs no I/O and returns an already-completed task. The
            // one async call is the write to the caller's stream below.
#pragma warning disable S6966, CA1849
            writer.Flush();
#pragma warning restore S6966, CA1849
        }

        byte[] payload = buffer.GetBuffer();
#if NETSTANDARD2_0
        await destination.WriteAsync(payload, 0, (int)buffer.Length, cancellationToken).ConfigureAwait(false);
#else
        await destination.WriteAsync(payload.AsMemory(0, (int)buffer.Length), cancellationToken).ConfigureAwait(false);
#endif
    }

    /// <summary>Creates a reader over <paramref name="payload"/> positioned on the artifact's opening brace.</summary>
    public static Utf8JsonReader CreateReader(ReadOnlySpan<byte> payload, string artifact, in ArtifactLimits limits)
    {
        var reader = new Utf8JsonReader(payload, JsonArtifact.ReaderOptions(limits));
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw new InvalidDataException($"A '{ArtifactHeader.SchemaFor(artifact)}' artifact must be a JSON object.");
        }
        return reader;
    }

    /// <summary>Checks that the artifact's object closed cleanly and that nothing follows it.</summary>
    /// <remarks>
    /// The final <c>Read</c> is what forces the point: the reader sees the whole
    /// payload as a final block, so a trailing token makes it raise a
    /// <see cref="JsonException"/> that <see cref="Malformed"/> restates. Without
    /// that call, an artifact with junk appended would load as if it were clean.
    /// The explicit throw after it is a belt-and-braces guard for a reader that
    /// might one day report the condition instead of raising.
    /// </remarks>
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

    /// <summary>
    /// Restates a JSON syntax error as the <see cref="InvalidDataException"/> the
    /// public API documents, keeping the parser's message as the inner exception.
    /// </summary>
    /// <remarks>
    /// Callers should not have to catch two exception types depending on whether a
    /// bad file broke the grammar or broke the schema.
    /// </remarks>
    public static InvalidDataException Malformed(string artifact, JsonException inner) =>
        new($"A '{ArtifactHeader.SchemaFor(artifact)}' artifact is not well-formed JSON: {inner.Message}", inner);

    private static void WriteDocument(Utf8JsonWriter writer, string artifact, int version, Action<Utf8JsonWriter> writeBody)
    {
        writer.WriteStartObject();
        ArtifactHeader.Write(writer, artifact, version);
        writeBody(writer);
        writer.WriteEndObject();
    }
}
