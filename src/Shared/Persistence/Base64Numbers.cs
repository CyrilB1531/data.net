using System.Buffers;
using System.Buffers.Binary;
using System.Buffers.Text;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Lodestar.Internal.Persistence;

/// <summary>
/// Reads and writes a numeric vector as one base64 string of raw little-endian
/// IEEE-754 bits — the encoding ADR 0011 chose for the parts of an artifact
/// nobody reads by eye.
/// </summary>
/// <remarks>
/// Encoding and bounds only: what a value <em>means</em> belongs to the artifact that owns the
/// vector — see ADR 0011, "Doubles". Raw bits round-trip exact by construction, and little-endian is
/// written explicitly so a file written on one architecture reads on another.
/// </remarks>
internal static class Base64Numbers
{
    /// <summary>The quotation mark that opens and closes a JSON string value.</summary>
    private const byte Quote = (byte)'"';

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
    /// <remarks>
    /// The encoded run is bounded <em>before</em> the decode as well as after it:
    /// checking only the decoded count would let the limit be satisfied by the
    /// allocation it exists to prevent. Four base64 characters carry three bytes.
    /// </remarks>
    public static double[] ReadDoubles(
        ref Utf8JsonReader reader,
        string artifact,
        string propertyName,
        in ArtifactLimits limits)
    {
        long encodedLength = ReadToken(ref reader, artifact, propertyName);
        limits.CheckArrayLength(encodedLength * 3 / (4 * sizeof(double)), propertyName);

        double[] values = Decode<double>(ref reader, artifact, propertyName, sizeof(double));

        limits.CheckArrayLength(values.LongLength, propertyName);
        SwapIfBigEndian64(MemoryMarshal.AsBytes(values.AsSpan()));
        return values;
    }

    /// <summary>Writes <paramref name="values"/> as a base64 property.</summary>
    /// <remarks>
    /// Bulk-copied rather than converted element by element the way
    /// <see cref="WriteDoubles"/> is. <c>BitConverter.SingleToInt32Bits</c> does not
    /// exist on <c>netstandard2.0</c>, and this block is the largest thing in any
    /// artifact — an embedding index is millions of floats where an idf vector is
    /// tens of thousands — which is why the little-endian path does not copy it at
    /// all: the buffer existed only to be swapped in place (#323).
    /// </remarks>
    public static void WriteSingles(Utf8JsonWriter writer, string propertyName, ReadOnlySpan<float> values)
    {
        if (BitConverter.IsLittleEndian)
        {
            writer.WriteBase64String(propertyName, MemoryMarshal.AsBytes(values));
            return;
        }

        byte[] raw = new byte[values.Length * sizeof(float)];
        MemoryMarshal.AsBytes(values).CopyTo(raw);
        SwapIfBigEndian32(raw);
        writer.WriteBase64String(propertyName, raw);
    }

    /// <summary>
    /// How much of the block one slice of the chunked write covers. A few hundred KB,
    /// and a multiple of 12 — 3 floats, 4 base64 groups — so no slice boundary falls
    /// inside a group and only the final slice can pad.
    /// </summary>
    /// <remarks>
    /// 245 760 bytes is 61 440 floats and encodes to 327 680, so both the source slice
    /// and its encoding stay well under the large-object-heap threshold and the rented
    /// scratch buffer is reused for every slice of every save.
    /// </remarks>
    private const int SliceBytes = 240 * 1024;

    /// <summary>
    /// Writes the same base64 string <see cref="WriteSingles"/> writes, as a quoted
    /// value straight to <paramref name="destination"/>, encoding it a slice at a time.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Utf8JsonWriter.WriteBase64String</c> encodes the whole block in one call, so the
    /// writer's internal buffer has to grow to hold the entire encoding — 20.48 MB for the
    /// benchmark index — by successive doubling, each growth a large-object-heap allocation
    /// whose pages the operating system commits on first touch, plus a copy of everything
    /// written so far. Measured, that is the dominant cost of a save: writing the vector
    /// block alone costs 16.3 ms of which the encode is 3.4, and the same block written in
    /// slices costs 8.3. The guide's "What a save actually spends its time on" section has
    /// the profile.
    /// </para>
    /// <para>
    /// The output is byte-for-byte what the one-shot call produces, and that is a property
    /// of the slice size rather than of the encoder: base64 maps each group of 3 input bytes
    /// onto 4 output characters independently, so a concatenation of slice encodings equals
    /// the encoding of the concatenation <em>exactly when</em> every slice but the last is a
    /// whole number of groups. <see cref="SliceBytes"/> is a multiple of 3 (and of 4, so a
    /// slice is also a whole number of floats), which is what makes that hold.
    /// </para>
    /// <para>
    /// The caller is responsible for having flushed the writer and for writing the rest of
    /// the document itself: nothing may go through the <c>Utf8JsonWriter</c> after this, and
    /// <see cref="ArtifactIo"/> is the only caller, which is why this takes a raw stream.
    /// </para>
    /// </remarks>
    public static void WriteSinglesChunked(Stream destination, ReadOnlySpan<float> values)
    {
        ReadOnlySpan<byte> raw = MemoryMarshal.AsBytes(values);
        byte[] scratch = ArrayPool<byte>.Shared.Rent(Base64.GetMaxEncodedToUtf8Length(SliceBytes));
        byte[]? swapped = BitConverter.IsLittleEndian ? null : ArrayPool<byte>.Shared.Rent(SliceBytes);
        try
        {
            destination.WriteByte(Quote);
            for (int offset = 0; offset < raw.Length; offset += SliceBytes)
            {
                int take = Math.Min(SliceBytes, raw.Length - offset);
                int written = EncodeSlice(raw.Slice(offset, take), scratch, swapped);
                destination.Write(scratch, 0, written);
            }
            destination.WriteByte(Quote);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(scratch);
            if (swapped is not null)
            {
                ArrayPool<byte>.Shared.Return(swapped);
            }
        }
    }

    /// <summary>The asynchronous counterpart of <see cref="WriteSinglesChunked"/>.</summary>
    /// <remarks>
    /// Takes <see cref="ReadOnlyMemory{T}"/> rather than a span because a span cannot cross
    /// an <see langword="await"/>. The encode of each slice is synchronous — it is CPU work
    /// on a rented buffer, and there is nothing to await — and only the write of the encoded
    /// slice is awaited, which is the 20.48 MB that actually reaches the device.
    /// </remarks>
    public static async Task WriteSinglesChunkedAsync(
        Stream destination,
        ReadOnlyMemory<float> values,
        CancellationToken cancellationToken)
    {
        byte[] scratch = ArrayPool<byte>.Shared.Rent(Base64.GetMaxEncodedToUtf8Length(SliceBytes));
        byte[]? swapped = BitConverter.IsLittleEndian ? null : ArrayPool<byte>.Shared.Rent(SliceBytes);
        byte[] quote = [Quote];
        try
        {
            await WriteAsync(destination, quote, 1, cancellationToken).ConfigureAwait(false);

            int totalBytes = values.Length * sizeof(float);
            for (int offset = 0; offset < totalBytes; offset += SliceBytes)
            {
                int take = Math.Min(SliceBytes, totalBytes - offset);
                int written = EncodeSlice(
                    MemoryMarshal.AsBytes(values.Span).Slice(offset, take), scratch, swapped);
                await WriteAsync(destination, scratch, written, cancellationToken).ConfigureAwait(false);
            }

            await WriteAsync(destination, quote, 1, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(scratch);
            if (swapped is not null)
            {
                ArrayPool<byte>.Shared.Return(swapped);
            }
        }
    }

    /// <summary>
    /// One asynchronous write of <paramref name="count"/> bytes from the front of
    /// <paramref name="buffer"/>, spelled the way each target framework wants it.
    /// </summary>
    private static Task WriteAsync(Stream destination, byte[] buffer, int count, CancellationToken cancellationToken) =>
#if NETSTANDARD2_0
        destination.WriteAsync(buffer, 0, count, cancellationToken);
#else
        destination.WriteAsync(buffer.AsMemory(0, count), cancellationToken).AsTask();
#endif

    /// <summary>Encodes one slice into <paramref name="scratch"/> and returns how many bytes it wrote.</summary>
    /// <param name="slice">The slice of the raw block, already a whole number of base64 groups unless it is the last.</param>
    /// <param name="scratch">The rented destination, large enough for any slice's encoding.</param>
    /// <param name="swapped">A rented buffer to swap into on a big-endian machine, or <see langword="null"/> on a little-endian one.</param>
    private static int EncodeSlice(ReadOnlySpan<byte> slice, byte[] scratch, byte[]? swapped)
    {
        if (swapped is not null)
        {
            slice.CopyTo(swapped);
            Span<byte> target = swapped.AsSpan(0, slice.Length);
            SwapIfBigEndian32(target);
            slice = target;
        }

        OperationStatus status = Base64.EncodeToUtf8(slice, scratch, out int consumed, out int written);
        if (status != OperationStatus.Done || consumed != slice.Length)
        {
            throw new InvalidOperationException(
                $"Encoding a {slice.Length}-byte slice reported {status} after {consumed} bytes.");
        }
        return written;
    }

    /// <summary>Reads a base64 property written by <see cref="WriteSingles"/>.</summary>
    /// <remarks>
    /// Deliberately not bounded against <see cref="ArtifactLimits.MaxArrayLength"/> like
    /// <see cref="ReadDoubles"/> — that vocabulary-scale limit, applied to a float count, refused a
    /// realistic 384-dimensional index at 2 604 vectors, the case
    /// <c>EmbeddingIndexHardeningTests.An_index_at_a_realistic_scale_loads_with_the_default_options</c>
    /// now pins passing. Still bounded, just earlier and in bytes: <see cref="JsonArtifact.ReadAllBytes"/>
    /// caps the whole artifact against <see cref="ArtifactLimits.MaxTotalBytes"/> before this runs.
    /// </remarks>
    public static float[] ReadSingles(
        ref Utf8JsonReader reader,
        string artifact,
        string propertyName)
    {
        ReadToken(ref reader, artifact, propertyName);
        float[] values = Decode<float>(ref reader, artifact, propertyName, sizeof(float));
        SwapIfBigEndian32(MemoryMarshal.AsBytes(values.AsSpan()));
        return values;
    }

    /// <summary>
    /// Turns a block of 32-bit values into little-endian, in place. A no-op on
    /// every platform .NET currently runs on — present so the format is defined by
    /// the file rather than by the architecture that happened to write it.
    /// </summary>
    private static void SwapIfBigEndian32(Span<byte> raw)
    {
        if (BitConverter.IsLittleEndian)
        {
            return;
        }
        Span<int> words = MemoryMarshal.Cast<byte, int>(raw);
        for (int i = 0; i < words.Length; i++)
        {
            words[i] = BinaryPrimitives.ReverseEndianness(words[i]);
        }
    }

    /// <summary>The 64-bit counterpart of <see cref="SwapIfBigEndian32"/>.</summary>
    private static void SwapIfBigEndian64(Span<byte> raw)
    {
        if (BitConverter.IsLittleEndian)
        {
            return;
        }
        Span<long> words = MemoryMarshal.Cast<byte, long>(raw);
        for (int i = 0; i < words.Length; i++)
        {
            words[i] = BinaryPrimitives.ReverseEndianness(words[i]);
        }
    }

    /// <summary>
    /// Decodes the reader's current string token into one array of
    /// <typeparamref name="T"/>, sized from the token's own encoded length so the
    /// base64 lands in its final destination rather than in an intermediate
    /// buffer that is then copied.
    /// </summary>
    /// <remarks>
    /// The canonical token — unescaped, one segment, a length base64 could have produced — decodes
    /// directly into its destination. Anything else falls through to <see cref="DecodeBase64"/>, which
    /// raises the same exception for every malformed token, at the cost of a second allocation.
    /// </remarks>
    private static T[] Decode<T>(ref Utf8JsonReader reader, string artifact, string propertyName, int elementSize)
        where T : struct
    {
        if (!reader.HasValueSequence
            && TryDecodedLength(reader.ValueSpan, out int decodedLength)
            && decodedLength % elementSize == 0)
        {
            // Uninitialized: returned only when the decode reports it filled every byte
            // of `destination`, and discarded for the fallback below otherwise.
            T[] values = Buffers.AllocateUninitialized<T>(decodedLength / elementSize);
            Span<byte> destination = MemoryMarshal.AsBytes(values.AsSpan());
            if (Base64.DecodeFromUtf8(reader.ValueSpan, destination, out int consumed, out int written) == OperationStatus.Done
                && consumed == reader.ValueSpan.Length
                && written == destination.Length)
            {
                return values;
            }
        }

        byte[] raw = DecodeBase64(ref reader, artifact, propertyName, elementSize);
        T[] fallback = Buffers.AllocateUninitialized<T>(raw.Length / elementSize);
        raw.AsSpan().CopyTo(MemoryMarshal.AsBytes(fallback.AsSpan()));
        return fallback;
    }

    /// <summary>
    /// How many bytes a canonical base64 run decodes to, or <c>false</c> when the
    /// run is not canonical. Never an error on its own: the caller falls through
    /// to the general path, which is what produces this type's diagnostics.
    /// </summary>
    private static bool TryDecodedLength(ReadOnlySpan<byte> encoded, out int decodedLength)
    {
        decodedLength = 0;
        if (encoded.Length % 4 != 0)
        {
            return false;
        }
        if (encoded.Length == 0)
        {
            return true;
        }

        int padding = 0;
        if (encoded[encoded.Length - 1] == (byte)'=')
        {
            padding++;
            if (encoded[encoded.Length - 2] == (byte)'=')
            {
                padding++;
            }
        }
        decodedLength = (encoded.Length / 4 * 3) - padding;
        return true;
    }

    /// <summary>Advances onto the property's string token and returns its encoded length.</summary>
    private static long ReadToken(ref Utf8JsonReader reader, string artifact, string propertyName)
    {
        if (!reader.Read() || reader.TokenType != JsonTokenType.String)
        {
            throw JsonArtifact.UnexpectedToken(artifact, propertyName, reader.TokenType);
        }
        return reader.HasValueSequence ? reader.ValueSequence.Length : reader.ValueSpan.Length;
    }

    /// <summary>
    /// Decodes the reader's current string token as base64, checked for valid
    /// base64 and a whole number of <paramref name="elementSize"/>-byte elements.
    /// Both format checks apply on every path; the array-length bound is applied
    /// by <see cref="ReadDoubles"/> around this call, and deliberately not by
    /// <see cref="ReadSingles"/> — see its remarks.
    /// </summary>
    private static byte[] DecodeBase64(ref Utf8JsonReader reader, string artifact, string propertyName, int elementSize)
    {
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
        return raw;
    }
}
