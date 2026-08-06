using System.Buffers.Binary;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace DataNet.Internal.Persistence;

/// <summary>
/// Reads and writes a numeric vector as one base64 string of raw little-endian
/// IEEE-754 bits — the encoding ADR 0011 chose for the parts of an artifact
/// nobody reads by eye.
/// </summary>
/// <remarks>
/// <para>
/// Encoding and bounds only: what a value <em>means</em> — whether a non-finite
/// entry is a broken model or the caller's own data — belongs to the artifact
/// that owns the vector, and is checked there.
/// </para>
/// <para>
/// Raw bits make the round trip exact by construction rather than by trusting a
/// decimal formatter, on any framework. Little-endian is written explicitly so a
/// file written on one architecture reads on another.
/// </para>
/// </remarks>
internal static class Base64Numbers
{
    /// <summary>Writes <paramref name="values"/> as a base64 property.</summary>
    public static void WriteDoubles(Utf8JsonWriter writer, string propertyName, IReadOnlyList<double> values)
    {
        byte[] raw = new byte[values.Count * sizeof(double)];
        for (int i = 0; i < values.Count; i++)
        {
            BinaryPrimitives.WriteInt64LittleEndian(
                raw.AsSpan(i * sizeof(double)),
                BitConverter.DoubleToInt64Bits(values[i]));
        }
        writer.WriteBase64String(propertyName, raw);
    }

    /// <summary>Reads a base64 property written by <see cref="WriteDoubles"/>.</summary>
    public static double[] ReadDoubles(
        ref Utf8JsonReader reader,
        string artifact,
        string propertyName,
        in ArtifactLimits limits)
    {
        byte[] raw = ReadRaw(ref reader, artifact, propertyName, limits, sizeof(double));
        var values = new double[raw.Length / sizeof(double)];
        for (int i = 0; i < values.Length; i++)
        {
            values[i] = BitConverter.Int64BitsToDouble(
                BinaryPrimitives.ReadInt64LittleEndian(raw.AsSpan(i * sizeof(double))));
        }
        return values;
    }

    /// <summary>Writes <paramref name="values"/> as a base64 property.</summary>
    /// <remarks>
    /// Bulk-copied rather than converted element by element the way
    /// <see cref="WriteDoubles"/> is. <c>BitConverter.SingleToInt32Bits</c> does not
    /// exist on <c>netstandard2.0</c>, and this block is the largest thing in any
    /// artifact — an embedding index is millions of floats where an idf vector is
    /// tens of thousands.
    /// </remarks>
    public static void WriteSingles(Utf8JsonWriter writer, string propertyName, ReadOnlySpan<float> values)
    {
        byte[] raw = new byte[values.Length * sizeof(float)];
        MemoryMarshal.AsBytes(values).CopyTo(raw);
        SwapIfBigEndian(raw);
        writer.WriteBase64String(propertyName, raw);
    }

    /// <summary>Reads a base64 property written by <see cref="WriteSingles"/>.</summary>
    public static float[] ReadSingles(
        ref Utf8JsonReader reader,
        string artifact,
        string propertyName,
        in ArtifactLimits limits)
    {
        byte[] raw = ReadRaw(ref reader, artifact, propertyName, limits, sizeof(float));
        SwapIfBigEndian(raw);
        var values = new float[raw.Length / sizeof(float)];
        MemoryMarshal.Cast<byte, float>(raw).CopyTo(values);
        return values;
    }

    /// <summary>
    /// Turns the buffer into little-endian, in place. A no-op on every platform
    /// .NET currently runs on — present so the format is defined by the file
    /// rather than by the architecture that happened to write it.
    /// </summary>
    private static void SwapIfBigEndian(byte[] raw)
    {
        if (BitConverter.IsLittleEndian)
        {
            return;
        }
        Span<int> words = MemoryMarshal.Cast<byte, int>(raw.AsSpan());
        for (int i = 0; i < words.Length; i++)
        {
            words[i] = BinaryPrimitives.ReverseEndianness(words[i]);
        }
    }

    /// <summary>Decodes the property's base64 payload, bounded on both sides of the decode.</summary>
    /// <remarks>
    /// The encoded run is bounded <em>before</em> decoding: <c>TryGetBytesFromBase64</c>
    /// materialises the whole decoded buffer first, so checking only the decoded count
    /// would let the limit be satisfied by the allocation it exists to prevent. Four
    /// base64 characters carry three bytes.
    /// </remarks>
    private static byte[] ReadRaw(
        ref Utf8JsonReader reader,
        string artifact,
        string propertyName,
        in ArtifactLimits limits,
        int elementSize)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.String)
        {
            throw JsonArtifact.UnexpectedToken(artifact, propertyName, reader.TokenType);
        }

        long encodedLength = reader.HasValueSequence ? reader.ValueSequence.Length : reader.ValueSpan.Length;
        limits.CheckArrayLength(encodedLength * 3 / (4 * elementSize), propertyName);

        if (!reader.TryGetBytesFromBase64(out byte[]? raw))
        {
            throw JsonArtifact.Inconsistent(artifact, $"'{propertyName}' is not valid base64.");
        }
        if (raw.Length % elementSize != 0)
        {
            throw JsonArtifact.Inconsistent(
                artifact,
                $"'{propertyName}' does not hold a whole number of {elementSize * 8}-bit values ({raw.Length} bytes).");
        }

        limits.CheckArrayLength(raw.Length / elementSize, propertyName);
        return raw;
    }
}
