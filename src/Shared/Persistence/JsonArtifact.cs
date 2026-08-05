using System.Globalization;
using System.Text;
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
    public static JsonWriterOptions WriterOptions => new()
    {
        Indented = false,
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
    /// Writes <paramref name="value"/> as a JSON number using the invariant
    /// <c>"G17"</c> form, which round-trips a <see cref="double"/> exactly on every
    /// supported framework.
    /// </summary>
    public static void WriteExactDouble(Utf8JsonWriter writer, double value)
    {
        if (double.IsNaN(value) || double.IsInfinity(value))
        {
            throw new InvalidDataException(
                $"Cannot persist the non-finite value {value.ToString(CultureInfo.InvariantCulture)}: JSON has no representation for it.");
        }
        writer.WriteRawValue(value.ToString("G17", CultureInfo.InvariantCulture), skipInputValidation: true);
    }

    /// <summary>Writes a named property whose value is an exactly round-tripping double.</summary>
    public static void WriteExactDouble(Utf8JsonWriter writer, string propertyName, double value)
    {
        writer.WritePropertyName(propertyName);
        WriteExactDouble(writer, value);
    }

    /// <summary>Reads <paramref name="stream"/> to its end, failing past <c>MaxTotalBytes</c>.</summary>
    /// <remarks>The stream is never disposed — ownership stays with the caller.</remarks>
    public static byte[] ReadAllBytes(Stream stream, in ArtifactLimits limits)
    {
        Guard.NotNull(stream);
        CheckDeclaredLength(stream, limits);

        var buffer = new byte[CopyBufferSize];
        using var accumulated = new MemoryStream();
        int read;
        while ((read = stream.Read(buffer, 0, buffer.Length)) > 0)
        {
            limits.CheckTotalBytes(accumulated.Length + read);
            accumulated.Write(buffer, 0, read);
        }
        return accumulated.ToArray();
    }

    /// <summary>Asynchronous counterpart of <see cref="ReadAllBytes"/>.</summary>
    public static async Task<byte[]> ReadAllBytesAsync(
        Stream stream,
        ArtifactLimits limits,
        CancellationToken cancellationToken)
    {
        Guard.NotNull(stream);
        CheckDeclaredLength(stream, limits);

        var buffer = new byte[CopyBufferSize];
        using var accumulated = new MemoryStream();
        int read;
        while ((read = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken).ConfigureAwait(false)) > 0)
        {
            limits.CheckTotalBytes(accumulated.Length + read);
            accumulated.Write(buffer, 0, read);
        }
        return accumulated.ToArray();
    }

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

    private static void CheckDeclaredLength(Stream stream, in ArtifactLimits limits)
    {
        // Fail before allocating anything when the stream already knows it is too big.
        if (stream.CanSeek)
        {
            limits.CheckTotalBytes(stream.Length - stream.Position);
        }
    }
}
