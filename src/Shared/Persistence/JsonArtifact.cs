using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace DataNet.Internal.Persistence;

/// <summary>
/// The JSON reading and writing primitives shared by every DataNet artifact:
/// byte-capped stream reads, exact <see cref="double"/> formatting, and the
/// exception shapes the public API documents.
/// </summary>
/// <remarks>
/// Artifacts are read in one pass over a single buffer rather than through a
/// <c>JsonDocument</c> node tree: the buffer is what <c>MaxTotalBytes</c> bounds,
/// and a <see cref="Utf8JsonReader"/> over it allocates nothing per token.
/// </remarks>
internal static class JsonArtifact
{
    /// <summary>UTF-8 without a byte-order mark — what every artifact is written in.</summary>
    /// <remarks>
    /// <see cref="Utf8JsonWriter"/> emits no preamble of its own, so writing is
    /// BOM-free by construction; this instance exists for the paths that need an
    /// explicit encoder, and throws on invalid surrogates rather than substituting.
    /// </remarks>
    public static readonly UTF8Encoding Utf8NoBom = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

    /// <summary>Writer options for artifacts: compact, and validated as it is written.</summary>
    /// <remarks>
    /// The relaxed encoder is deliberate: the default escapes every non-ASCII character as
    /// <c>\uXXXX</c>, and this library ships accented Snowball stop-word lists. "Unsafe" names an
    /// HTML-injection concern that does not apply — an artifact is read back by this library's own
    /// parser, never dropped into a <c>&lt;script&gt;</c> block. See ADR 0011, "Escaping".
    /// </remarks>
    public static JsonWriterOptions WriterOptions => new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
        Indented = false,
        // Kept on: the structural check is cheap for a writer to perform, and catches
        // a malformed artifact at the point a writer bug would produce one.
        SkipValidation = false,
    };

    /// <summary>Reader options honouring the caller's depth limit; comments and trailing commas are rejected.</summary>
    public static JsonReaderOptions ReaderOptions(in ArtifactLimits limits) => new()
    {
        MaxDepth = limits.MaxJsonDepth,
        CommentHandling = JsonCommentHandling.Disallow,
        AllowTrailingCommas = false,
    };

    /// <summary>
    /// Writes <paramref name="value"/> as a JSON number that reads back as the same
    /// <see cref="double"/>, bit for bit.
    /// </summary>
    /// <remarks>
    /// From <c>net8.0</c>, <see cref="Utf8JsonWriter.WriteNumberValue(double)"/> emits the shortest
    /// round-tripping form, exact since .NET Core 3.0. A <c>netstandard2.0</c> build may run on .NET
    /// Framework, where that is not guaranteed, so it keeps invariant <c>"G17"</c> instead — exact
    /// everywhere, at the cost of longer numbers. See ADR 0011, "Doubles", for what the contract
    /// actually promises: each build byte-reproducible against itself, not the two against each other.
    /// </remarks>
    public static void WriteExactDouble(Utf8JsonWriter writer, double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new InvalidDataException(
                $"Cannot persist the non-finite value {value.ToString(CultureInfo.InvariantCulture)}: JSON has no representation for it.");
        }
#if NETSTANDARD2_0
        writer.WriteRawValue(value.ToString("G17", CultureInfo.InvariantCulture), skipInputValidation: true);
#else
        writer.WriteNumberValue(value);
#endif
    }

    /// <summary>Writes a named property whose value is an exactly round-tripping double.</summary>
    public static void WriteExactDouble(Utf8JsonWriter writer, string propertyName, double value)
    {
        writer.WritePropertyName(propertyName);
        WriteExactDouble(writer, value);
    }

    /// <summary>Reads <paramref name="stream"/> to its end, failing past <c>MaxTotalBytes</c>.</summary>
    /// <remarks>
    /// Never disposed — ownership stays with the caller. On the growable path the returned buffer
    /// is longer than the payload, so read it as the <see cref="ReadOnlyMemory{T}"/> it comes back
    /// as: reaching for the array behind it, or for <c>.ToArray()</c>, both reintroduce the copy this
    /// return type exists to remove and expose a tail of zeroes past the payload's end.
    /// </remarks>
    public static ReadOnlyMemory<byte> ReadAllBytes(Stream stream, in ArtifactLimits limits)
    {
        Guard.NotNull(stream);
        CheckDeclaredLength(stream, limits);

        if (TryReadDeclaredLength(stream, out byte[] exact, out int filled))
        {
            return new ReadOnlyMemory<byte>(exact, 0, filled);
        }

        var buffer = new byte[CopyBufferSize];
        using var accumulated = new MemoryStream();
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            limits.CheckTotalBytes(accumulated.Length + read);
            accumulated.Write(buffer, 0, read);
        }
        return new ReadOnlyMemory<byte>(accumulated.GetBuffer(), 0, (int)accumulated.Length);
    }

    /// <summary>Asynchronous counterpart of <see cref="ReadAllBytes"/>.</summary>
    public static async Task<ReadOnlyMemory<byte>> ReadAllBytesAsync(
        Stream stream,
        ArtifactLimits limits,
        CancellationToken cancellationToken)
    {
        Guard.NotNull(stream);
        CheckDeclaredLength(stream, limits);

        ReadOnlyMemory<byte>? exact = await TryReadDeclaredLengthAsync(stream, cancellationToken).ConfigureAwait(false);
        if (exact is ReadOnlyMemory<byte> payload)
        {
            return payload;
        }

        var buffer = new byte[CopyBufferSize];
        using var accumulated = new MemoryStream();
        int read;
        while ((read = await ReadChunkAsync(stream, buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
        {
            limits.CheckTotalBytes(accumulated.Length + read);

            // CA1849 / SonarLint S6966: the async call in this loop is the ReadAsync above, on
            // the caller's stream, which may be a file or a socket. The destination is
            // a MemoryStream: its WriteAsync performs no I/O, copies into the same
            // buffer and returns an already-completed task, so awaiting it would add a
            // state machine and allocations without ever yielding.
#pragma warning disable S6966, CA1849
            accumulated.Write(buffer, 0, read);
#pragma warning restore S6966, CA1849
        }
        return new ReadOnlyMemory<byte>(accumulated.GetBuffer(), 0, (int)accumulated.Length);
    }

    /// <summary>
    /// Fills one exactly-sized buffer from a stream that knows its own length —
    /// every <c>Load</c> that starts from a path, and every test that starts from a
    /// <see cref="MemoryStream"/>.
    /// </summary>
    /// <remarks>
    /// Returns <c>false</c> when there is no length to size from, that length exceeds what a single array
    /// can hold, or the stream holds more than declared — the position is then put back so the caller's
    /// growable path reads the whole thing rather than the prefix this would have truncated it to.
    /// </remarks>
    private static bool TryReadDeclaredLength(Stream stream, out byte[] buffer, out int filled)
    {
        buffer = [];
        filled = 0;
        if (!stream.CanSeek)
        {
            return false;
        }

        long origin = stream.Position;
        long declared = stream.Length - origin;
        if (declared < 0 || declared > MaxSingleBuffer)
        {
            return false;
        }

        buffer = new byte[declared];
        int read;
        while (filled < buffer.Length && (read = stream.Read(buffer, filled, buffer.Length - filled)) > 0)
        {
            filled += read;
        }

        if (filled == buffer.Length && stream.ReadByte() >= 0)
        {
            stream.Position = origin;
            buffer = [];
            filled = 0;
            return false;
        }
        return true;
    }

    /// <summary>Asynchronous counterpart of <see cref="TryReadDeclaredLength"/>; <c>null</c> where that one returns <c>false</c>.</summary>
    private static async Task<ReadOnlyMemory<byte>?> TryReadDeclaredLengthAsync(
        Stream stream,
        CancellationToken cancellationToken)
    {
        if (!stream.CanSeek)
        {
            return null;
        }

        long origin = stream.Position;
        long declared = stream.Length - origin;
        if (declared < 0 || declared > MaxSingleBuffer)
        {
            return null;
        }

        var buffer = new byte[declared];
        int filled = 0;
        int read;
        while (filled < buffer.Length
            && (read = await ReadChunkAsync(stream, buffer, filled, buffer.Length - filled, cancellationToken).ConfigureAwait(false)) > 0)
        {
            filled += read;
        }

        if (filled == buffer.Length)
        {
            var probe = new byte[1];
            if (await ReadChunkAsync(stream, probe, 0, 1, cancellationToken).ConfigureAwait(false) > 0)
            {
                stream.Position = origin;
                return null;
            }
        }
        return new ReadOnlyMemory<byte>(buffer, 0, filled);
    }

    /// <summary>Reads one chunk, using the allocation-free overload where it exists.</summary>
    /// <remarks>
    /// <c>Stream.ReadAsync(Memory&lt;byte&gt;, CancellationToken)</c> arrived with
    /// netstandard2.1, so the older target keeps the array overload. Wrapping the
    /// difference here keeps the read loops themselves free of conditional
    /// compilation.
    /// </remarks>
    private static ValueTask<int> ReadChunkAsync(
        Stream stream,
        byte[] buffer,
        int offset,
        int count,
        CancellationToken cancellationToken) =>
#if NETSTANDARD2_0
        new(stream.ReadAsync(buffer, offset, count, cancellationToken));
#else
        stream.ReadAsync(buffer.AsMemory(offset, count), cancellationToken);
#endif

    /// <summary>Opens <paramref name="path"/> for writing an artifact; the caller owns the returned stream.</summary>
    public static FileStream OpenWrite(string path)
    {
        Guard.NotNull(path);
        return new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, CopyBufferSize, useAsync: false);
    }

    /// <summary>Opens <paramref name="path"/> for reading an artifact; the caller owns the returned stream.</summary>
    public static FileStream OpenRead(string path, bool useAsync = false)
    {
        Guard.NotNull(path);
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, CopyBufferSize, useAsync);
    }

    /// <summary>Advances onto the value of the current property and returns it as a <see cref="bool"/>.</summary>
    public static bool ReadBoolean(ref Utf8JsonReader reader, string artifact, string propertyName)
    {
        if (!reader.Read() || (reader.TokenType != JsonTokenType.True && reader.TokenType != JsonTokenType.False))
        {
            throw UnexpectedToken(artifact, propertyName, reader.TokenType);
        }
        return reader.GetBoolean();
    }

    /// <summary>Advances onto the value of the current property and returns it as an <see cref="int"/>.</summary>
    public static int ReadInt32(ref Utf8JsonReader reader, string artifact, string propertyName)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out int value))
        {
            throw UnexpectedToken(artifact, propertyName, reader.TokenType);
        }
        return value;
    }

    /// <summary>Advances onto the value of the current property and returns it as a <see cref="double"/>.</summary>
    public static double ReadDouble(ref Utf8JsonReader reader, string artifact, string propertyName)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.Number || !reader.TryGetDouble(out double value))
        {
            throw UnexpectedToken(artifact, propertyName, reader.TokenType);
        }
        return value;
    }

    /// <summary>Advances onto the value of the current property and returns it as a non-null string.</summary>
    public static string ReadString(ref Utf8JsonReader reader, string artifact, string propertyName)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.String)
        {
            throw UnexpectedToken(artifact, propertyName, reader.TokenType);
        }
        return reader.GetString()!;
    }

    /// <summary>Advances onto the value of the current property, allowing an explicit JSON <c>null</c>.</summary>
    public static string? ReadNullableString(ref Utf8JsonReader reader, string artifact, string propertyName)
    {
        if (!reader.Read())
        {
            throw Truncated(artifact);
        }
        return reader.TokenType switch
        {
            JsonTokenType.Null => null,
            JsonTokenType.String => reader.GetString(),
            _ => throw UnexpectedToken(artifact, propertyName, reader.TokenType),
        };
    }

    /// <summary>Positions the reader on the opening brace of the current property's object value.</summary>
    public static void ReadStartObject(ref Utf8JsonReader reader, string artifact, string propertyName)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartObject)
        {
            throw UnexpectedToken(artifact, propertyName, reader.TokenType);
        }
    }

    /// <summary>Positions the reader on the opening bracket of the current property's array value.</summary>
    public static void ReadStartArray(ref Utf8JsonReader reader, string artifact, string propertyName)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.StartArray)
        {
            throw UnexpectedToken(artifact, propertyName, reader.TokenType);
        }
    }

    /// <summary>The exception raised when an artifact carries a property the schema does not define.</summary>
    public static InvalidDataException UnknownProperty(string artifact, string propertyName) =>
        new($"Unknown property '{propertyName}' in a '{artifact}' artifact. Unknown properties are rejected so a newer file is not silently misread.");

    /// <summary>The exception raised when a required property is absent.</summary>
    public static InvalidDataException MissingProperty(string artifact, string propertyName) =>
        new($"A '{artifact}' artifact is missing the required property '{propertyName}'.");

    /// <summary>The exception raised when a property holds the wrong JSON type.</summary>
    public static InvalidDataException UnexpectedToken(string artifact, string propertyName, JsonTokenType actual) =>
        new($"Property '{propertyName}' of a '{artifact}' artifact has unexpected JSON token type {actual}.");

    /// <summary>The exception raised when the bytes end mid-artifact.</summary>
    public static InvalidDataException Truncated(string artifact) =>
        new($"A '{artifact}' artifact ended unexpectedly: the input is truncated or malformed.");

    /// <summary>The exception raised when two persisted collections disagree on length.</summary>
    public static InvalidDataException Inconsistent(string artifact, string detail) =>
        new($"A '{artifact}' artifact is internally inconsistent: {detail}");

    private const int CopyBufferSize = 81920;

    /// <summary>
    /// The largest buffer the exact path will allocate — the CLR's own array
    /// ceiling, which <c>netstandard2.0</c> has no <c>Array.MaxLength</c> to name.
    /// A caller who raises <c>MaxTotalBytes</c> past it gets the growable path and
    /// whatever it did before, rather than a new exception from this method.
    /// </summary>
    private const long MaxSingleBuffer = 0x7FFFFFC7;

    private static void CheckDeclaredLength(Stream stream, in ArtifactLimits limits)
    {
        // Fail before allocating anything when the stream already knows it is too big.
        if (stream.CanSeek)
        {
            limits.CheckTotalBytes(stream.Length - stream.Position);
        }
    }
}
