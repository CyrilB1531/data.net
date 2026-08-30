namespace Lodestar.Internal.Persistence;

/// <summary>Fills a span from a stream, or refuses because the stream ended first.</summary>
internal static class StreamFill
{
    /// <summary>Reads exactly <paramref name="destination"/>.Length bytes, or throws.</summary>
    /// <remarks>
    /// The net10 path reads into the destination directly, which is the whole point: a
    /// block read this way is copied once. netstandard2.0 has no Stream.Read(Span), so it
    /// stages through a chunk and pays a second copy — the deliberate split VectorMath.Dot
    /// already makes, one API and one behaviour at two speeds.
    /// </remarks>
    /// <exception cref="InvalidDataException">The stream ended before the span was full.</exception>
    public static void Exactly(Stream stream, Span<byte> destination, string shortMessage)
    {
        int filled = 0;
#if NET7_0_OR_GREATER
        while (filled < destination.Length)
        {
            int read = stream.Read(destination[filled..]);
            if (read == 0)
            {
                throw new InvalidDataException(shortMessage);
            }
            filled += read;
        }
#else
        byte[] chunk = new byte[Math.Min(81_920, destination.Length)];
        while (filled < destination.Length)
        {
            int read = stream.Read(chunk, 0, Math.Min(chunk.Length, destination.Length - filled));
            if (read == 0)
            {
                throw new InvalidDataException(shortMessage);
            }
            chunk.AsSpan(0, read).CopyTo(destination[filled..]);
            filled += read;
        }
#endif
    }

    /// <summary>Reads up to <paramref name="destination"/>.Length bytes, returning how many.</summary>
    /// <remarks>Used where a short read is a refusal the caller words, not this one.</remarks>
    public static int UpTo(Stream stream, Span<byte> destination)
    {
        int filled = 0;
        while (filled < destination.Length)
        {
#if NET7_0_OR_GREATER
            int read = stream.Read(destination[filled..]);
#else
            byte[] chunk = new byte[destination.Length - filled];
            int read = stream.Read(chunk, 0, chunk.Length);
            chunk.AsSpan(0, read).CopyTo(destination[filled..]);
#endif
            if (read == 0)
            {
                break;
            }
            filled += read;
        }
        return filled;
    }
}
