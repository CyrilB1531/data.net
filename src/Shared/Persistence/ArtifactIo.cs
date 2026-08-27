using System.Buffers;
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
    /// <summary>The brace that closes an artifact, written by hand when the writer cannot.</summary>
    private const byte CloseBrace = (byte)'}';

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

    /// <summary>
    /// Saves an artifact whose last property is a float block too large to buffer,
    /// writing that block to <paramref name="destination"/> a slice at a time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The split exists because <c>Utf8JsonWriter.WriteBase64String</c> takes the whole
    /// block in one call and so forces the writer's buffer to grow to hold the whole
    /// encoding — which the save profile in the performance guide measures as the
    /// dominant cost of saving an index. <paramref name="writeHead"/> writes every
    /// property before the block; this method writes the block itself.
    /// </para>
    /// <para>
    /// <b>Nothing goes through the <c>Utf8JsonWriter</c> after the block.</b> The writer
    /// emits the property name and is then flushed and disposed, the value is written to
    /// the stream directly, and the closing brace is written by hand — because a writer
    /// left on a property name refuses to close the object, which is
    /// <c>SkipValidation = false</c> doing exactly the job
    /// <see cref="JsonArtifact.WriterOptions"/> keeps it on for. Handing the artifacts a
    /// stranded writer to be careful with would be the alternative; owning the whole
    /// sequence here means no artifact can get it wrong.
    /// </para>
    /// </remarks>
    /// <param name="destination">The stream to write to; flushed but never disposed.</param>
    /// <param name="artifact">The artifact kind, for the header.</param>
    /// <param name="version">The artifact version, for the header.</param>
    /// <param name="writeHead">Writes every property that precedes the block.</param>
    /// <param name="blockProperty">The name of the block's property.</param>
    /// <param name="block">The float block, written as base64 raw little-endian bits.</param>
    public static void SaveWithBlock(
        Stream destination,
        string artifact,
        int version,
        Action<Utf8JsonWriter> writeHead,
        string blockProperty,
        ReadOnlySpan<float> block)
    {
        Guard.NotNull(destination);

        using (var writer = new Utf8JsonWriter(destination, JsonArtifact.WriterOptions))
        {
            writer.WriteStartObject();
            ArtifactHeader.Write(writer, artifact, version);
            writeHead(writer);
            writer.WritePropertyName(blockProperty);
            writer.Flush();
        }

        Base64Numbers.WriteSinglesChunked(destination, block);
        destination.WriteByte(CloseBrace);
        destination.Flush();
    }

    /// <summary>The asynchronous counterpart of <see cref="SaveWithBlock"/>.</summary>
    /// <remarks>
    /// No intermediate <see cref="MemoryStream"/>, which the synchronous-flush comment on
    /// <see cref="SaveAsync"/> is the reason for: the head is small and its one flush is
    /// bounded, and the block — every byte that makes this artifact large — is written
    /// through <c>WriteAsync</c> a slice at a time. Before the block was sliced there was
    /// no bounded flush to rely on, and the artifact was buffered twice, both times by
    /// doubling.
    /// </remarks>
    public static async Task SaveWithBlockAsync(
        Stream destination,
        string artifact,
        int version,
        Action<Utf8JsonWriter> writeHead,
        string blockProperty,
        ReadOnlyMemory<float> block,
        CancellationToken cancellationToken)
    {
        Guard.NotNull(destination);

        using (var writer = new Utf8JsonWriter(destination, JsonArtifact.WriterOptions))
        {
            writer.WriteStartObject();
            ArtifactHeader.Write(writer, artifact, version);
            writeHead(writer);
            writer.WritePropertyName(blockProperty);
            // CA1849 / SonarLint S6966: the head is at most a few hundred KB of ids and
            // scalars, and this is the only synchronous flush left on the path — the
            // block below, which is the whole reason an index artifact is large, is
            // written asynchronously.
#pragma warning disable S6966, CA1849
            writer.Flush();
#pragma warning restore S6966, CA1849
        }

        await Base64Numbers.WriteSinglesChunkedAsync(destination, block, cancellationToken).ConfigureAwait(false);

        byte[] closing = [CloseBrace];
#if NETSTANDARD2_0
        await destination.WriteAsync(closing, 0, 1, cancellationToken).ConfigureAwait(false);
#else
        await destination.WriteAsync(closing.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
#endif
        await destination.FlushAsync(cancellationToken).ConfigureAwait(false);
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

    /// <summary>The same reader over an artifact too large for one array (#377).</summary>
    /// <param name="payload">The artifact, in segments none of which exceeds one array.</param>
    /// <param name="artifact">The artifact kind, for the message.</param>
    /// <param name="limits">Bounds applied while reading.</param>
    public static Utf8JsonReader CreateReader(ReadOnlySequence<byte> payload, string artifact, in ArtifactLimits limits)
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
