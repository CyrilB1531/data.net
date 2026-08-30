using System.Buffers.Binary;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using Lodestar.Internal.Persistence;

namespace Lodestar.Embeddings.Persistence;

/// <summary>A float block read from a <c>.npy</c> file, with the shape it was stored under.</summary>
/// <param name="Values">The elements, in C order.</param>
/// <param name="Shape">The dimensions; one entry for a vector, two for a matrix.</param>
public readonly record struct NpyBlock(ReadOnlyMemory<float> Values, IReadOnlyList<int> Shape)
{
    /// <summary>The array this block owns, or <see langword="null"/> when it borrows.</summary>
    /// <remarks>
    /// Only <see cref="NpyFile.Read(Stream, ArtifactLoadOptions?)"/> fills it, because only
    /// that path allocates an array nobody else holds. A block over a caller's bytes leaves
    /// it null, and so does one built by hand — which is what stops decision 0056's
    /// ownership transfer being reached without the method that documents it.
    /// </remarks>
    // CA1819: handing the array out is the contract -- FromOwnedBlock adopts it and the
    // block stops using it, so the defensive copy the rule wants would defeat the feature.
#pragma warning disable CA1819
    public float[]? OwnedArray { get; init; }
#pragma warning restore CA1819
}

/// <summary>Reads and writes a <see cref="float"/> block in numpy's <c>.npy</c> format.</summary>
/// <remarks>
/// Interop for a float matrix, not a second artifact format: a <c>.npy</c> carries no ids,
/// no normalize flag and no schema, and <c>EmbeddingIndex.Save</c> is untouched (#450).
/// <b>Its header is a Python dict literal and is never evaluated</b> — a fixed grammar only,
/// and <c>'|O'</c>, numpy's pickle-backed dtype, is refused by name (ADR 0011).
/// </remarks>
public static class NpyFile
{
    private const string SourceName = ".npy";

    /// <summary>numpy's file magic: <c>\x93NUMPY</c>.</summary>
    private static readonly byte[] Magic = [0x93, (byte)'N', (byte)'U', (byte)'M', (byte)'P', (byte)'Y'];

    /// <summary>Magic, two version bytes, then the header length.</summary>
    private const int VersionOffset = 6;

    /// <summary>The only dtype this reads and writes: little-endian IEEE-754 single.</summary>
    private const string LittleEndianSingle = "<f4";

    /// <summary>
    /// numpy pads the header so the payload starts on a 64-byte boundary, and this
    /// writes the same padding so a file we produce is byte-comparable with one numpy
    /// produced for the same array.
    /// </summary>
    private const int PayloadAlignment = 64;

    /// <summary>Reads a <c>.npy</c> file, the counterpart of <c>numpy.load</c>.</summary>
    /// <param name="source">The file's bytes; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <see langword="null"/> for the defaults.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="InvalidDataException">
    /// The file is not a <c>.npy</c>, is of an unsupported version, holds a dtype or
    /// layout this does not read, is truncated, or exceeds a limit.
    /// </exception>
    public static NpyBlock Read(Stream source, ArtifactLoadOptions? options = null)
    {
        Guard.NotNull(source);
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);

        // Twelve first, then the header it declares, then the payload straight into the
        // array the block keeps. No buffer holds the block on its way past (#466).
        Span<byte> prefix = stackalloc byte[MaxPrefix];
        int got = StreamFill.UpTo(source, prefix);
        if (got < MinPrefix)
        {
            return Read(prefix[..got], limits);
        }

        ReadOnlySpan<byte> read = prefix[..got];
        if (!HasMagic(read))
        {
            throw Malformed("does not open with numpy's magic.");
        }

        int total = HeaderTotal(read, out _);
        byte[] head = new byte[total];
        prefix[..Math.Min(got, total)].CopyTo(head);
        if (total > got)
        {
            StreamFill.Exactly(source, head.AsSpan(got), Malformed("ends inside its header.").Message);
        }

        ReadHeader(head, out NpyHeader header);
        long elements = Elements(header, limits);
        float[] values = Buffers.AllocateUninitialized<float>((int)elements);
        StreamFill.Exactly(
            source,
            MemoryMarshal.AsBytes(values.AsSpan()),
            ShortPayload(elements * sizeof(float)));

        return new NpyBlock(values, header.Shape) { OwnedArray = values };
    }

    /// <summary>Reads the file at <paramref name="path"/>.</summary>
    /// <param name="path">The <c>.npy</c> file.</param>
    /// <param name="options">Bounds applied while reading, or <see langword="null"/> for the defaults.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
    /// <exception cref="InvalidDataException">As <see cref="Read(Stream, ArtifactLoadOptions?)"/>.</exception>
    public static NpyBlock Read(string path, ArtifactLoadOptions? options = null)
    {
        using FileStream file = JsonArtifact.OpenRead(path);
        return Read(file, options);
    }

    /// <summary>Magic, version and the widest header length: the prefix one read always covers.</summary>
    private const int MaxPrefix = 12;

    /// <summary>Magic, version and the narrowest header length.</summary>
    private const int MinPrefix = 10;

    /// <summary>The element count a header declares, refused before anything is allocated.</summary>
    /// <remarks>
    /// The header is what announces the size on a staged read, so it is what has to be
    /// disbelieved. Divided rather than multiplied: two large dimensions overflow (#468).
    /// </remarks>
    private static long Elements(in NpyHeader header, in ArtifactLimits limits)
    {
        long elements = 1;
        foreach (int dimension in header.Shape)
        {
            elements *= dimension;
        }

        if (elements > limits.MaxTotalBytes / sizeof(float))
        {
            throw Malformed(
                $"declares {elements} elements, more than ArtifactLoadOptions.MaxTotalBytes "
                + $"({limits.MaxTotalBytes}) allows.");
        }
        return elements;
    }

    /// <summary>The message a payload shorter than its shape gets, on either read path.</summary>
    private static string ShortPayload(long expected) =>
        $"holds fewer than {expected} bytes of data where its shape needs {expected}.";

    /// <summary>Writes <paramref name="values"/> as a <c>.npy</c>, the counterpart of <c>numpy.save</c>.</summary>
    /// <param name="destination">The stream to write to. Flushed but never disposed — the caller owns it.</param>
    /// <param name="values">The elements, in C order.</param>
    /// <param name="shape">
    /// The dimensions the block is stored under; their product must equal
    /// <paramref name="values"/>'s length. One entry writes a vector, two a matrix.
    /// </param>
    /// <exception cref="ArgumentNullException"><paramref name="destination"/> or <paramref name="shape"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="shape"/> is empty, holds a negative dimension, or does not describe <paramref name="values"/>.</exception>
    public static void Write(Stream destination, ReadOnlySpan<float> values, params int[] shape)
    {
        Guard.NotNull(destination);
        Guard.NotNull(shape);
        CheckShape(values.Length, shape);

        byte[] header = BuildHeader(shape);
        destination.Write(header, 0, header.Length);
        WriteBlock(destination, values);
        destination.Flush();
    }

    /// <summary>Writes <paramref name="values"/> to <paramref name="path"/>, replacing any existing file.</summary>
    /// <param name="path">The file to write.</param>
    /// <param name="values">The elements, in C order.</param>
    /// <param name="shape">As <see cref="Write(Stream, ReadOnlySpan{float}, int[])"/>.</param>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> or <paramref name="shape"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="shape"/> does not describe <paramref name="values"/>.</exception>
    public static void Write(string path, ReadOnlySpan<float> values, params int[] shape)
    {
        using FileStream file = JsonArtifact.OpenWrite(path);
        Write(file, values, shape);
    }

    /// <summary>The read, on a payload already in memory and on resolved limits.</summary>
    private static NpyBlock Read(ReadOnlySpan<byte> payload, in ArtifactLimits limits)
    {
        int dataStart = ReadHeader(payload, out NpyHeader header);
        long elements = Elements(header, limits);

        long expected = elements * sizeof(float);
        long available = payload.Length - dataStart;
        if (available < expected)
        {
            throw Malformed(
                $"holds {available} bytes of data where its shape needs {expected}.");
        }

        // Uninitialized: every element is written by the copy below.
        float[] values = Buffers.AllocateUninitialized<float>((int)elements);
        payload.Slice(dataStart, (int)expected).CopyTo(MemoryMarshal.AsBytes(values.AsSpan()));
        return new NpyBlock(values, header.Shape);
    }

    /// <summary>Whether <paramref name="data"/> opens with numpy's magic bytes.</summary>
    private static bool HasMagic(ReadOnlySpan<byte> data) =>
        data.Length >= Magic.Length && data[..Magic.Length].SequenceEqual(Magic);

    /// <summary>Validates the magic and version, parses the header, and returns where the data starts.</summary>
    private static int ReadHeader(ReadOnlySpan<byte> payload, out NpyHeader header)
    {
        if (payload.Length < MinPrefix || !HasMagic(payload))
        {
            throw Malformed("does not open with numpy's magic.");
        }

        int total = HeaderTotal(payload, out int headerStart);
        if (total < headerStart || payload.Length < total)
        {
            throw Malformed("ends inside its header.");
        }

        header = ParseHeader(Encoding.UTF8.GetString(payload.Slice(headerStart, total - headerStart).ToArray()));
        return total;
    }

    /// <summary>The width of a version's header-length field: 2 for 1.0, 4 for 2.0.</summary>
    /// <remarks>
    /// Shared with the staged stream read, which needs the width before it knows how many
    /// bytes the header occupies. 3.0 is UTF-8 and would likely read the same, but is
    /// refused rather than assumed: none has been seen.
    /// </remarks>
    private static int LengthSize(byte major, byte minor) => (major, minor) switch
    {
        (1, 0) => 2,
        (2, 0) => 4,
        _ => throw Malformed($"is version {major}.{minor}, which this does not read."),
    };

    /// <summary>Header text refused beyond this.</summary>
    /// <remarks>
    /// Not because numpy stays under it -- a 2.0 header exists precisely for when it
    /// doesn't -- but because of what this reader accepts: one dtype and at most two
    /// dimensions parse to well under a kilobyte of text.
    /// </remarks>
    private const int MaxHeaderLength = 65_536;

    /// <summary>How many bytes the header occupies, prefix included, and where its text starts.</summary>
    /// <remarks>
    /// Shared with the staged stream read, which needs the total before it knows how many
    /// bytes to ask for. Twelve is the largest fixed prefix — six of magic, two of version,
    /// four of length — so one read of that size always carries the declared length.
    /// </remarks>
    private static int HeaderTotal(ReadOnlySpan<byte> prefix, out int headerStart)
    {
        int size = LengthSize(prefix[VersionOffset], prefix[VersionOffset + 1]);
        int lengthOffset = VersionOffset + 2;
        if (prefix.Length < lengthOffset + size)
        {
            throw Malformed("ends inside its header length.");
        }

        long declared = size == 2
            ? BinaryPrimitives.ReadUInt16LittleEndian(prefix[lengthOffset..])
            : BinaryPrimitives.ReadUInt32LittleEndian(prefix[lengthOffset..]);

        // Disbelieved by name before it sizes anything: unbounded, a length near
        // uint.MaxValue would overflow the offset below rather than being refused (#466).
        if (declared > MaxHeaderLength)
        {
            throw Malformed(
                $"declares a header of {declared} bytes, more than NpyFile.MaxHeaderLength "
                + $"({MaxHeaderLength}) allows.");
        }

        headerStart = lengthOffset + size;
        return headerStart + (int)declared;
    }

    /// <summary>
    /// Parses the header's three known keys out of numpy's dict literal, accepting a
    /// fixed grammar and refusing everything else.
    /// </summary>
    /// <remarks>
    /// Not a Python parser and not an evaluator: it accepts one dtype string, one
    /// boolean and a tuple of non-negative integers. Anything else — another dtype, a
    /// nested structure, an extra key — is refused with what it held. ADR 0011's
    /// reasoning about <c>pickle.load</c> is why it has this shape.
    /// </remarks>
    private static NpyHeader ParseHeader(string header)
    {
        string descr = RequiredValue(header, "descr");
        string fortran = RequiredValue(header, "fortran_order");
        string shape = RequiredValue(header, "shape");

        // First and by name: '|O' is numpy's object dtype and its payload is a pickle,
        // which is arbitrary code. ADR 0011 rules that out, wherever the file came from.
        if (descr is "|O" or "O")
        {
            throw Malformed(
                "holds pickled objects ('|O'), which are executable and are never read here.");
        }

        if (descr != LittleEndianSingle)
        {
            throw Malformed(
                $"holds '{descr}' where only '{LittleEndianSingle}' (little-endian float32) is read.");
        }

        if (fortran != "False")
        {
            throw Malformed($"is stored fortran_order={fortran}; only C order is read.");
        }

        return new NpyHeader(ParseShape(shape));
    }

    /// <summary>The value of one key, as the literal text between its quotes or up to the next comma.</summary>
    private static string RequiredValue(string header, string key)
    {
        int at = header.IndexOf($"'{key}':", StringComparison.Ordinal);
        if (at < 0)
        {
            throw Malformed($"has no '{key}' in its header.");
        }

        int from = at + key.Length + 3;
        while (from < header.Length && header[from] == ' ')
        {
            from++;
        }
        if (from >= header.Length)
        {
            throw Malformed($"ends inside its '{key}'.");
        }

        // A quoted dtype, a parenthesised shape, or a bare token up to the comma.
        return header[from] switch
        {
            '\'' => Delimited(header, from, '\'', key, trim: true),
            '(' => Delimited(header, from, ')', key, trim: false),
            _ => BareToken(header, from),
        };
    }

    /// <summary>The text from one opener to its closer, with the opener implied by the caller.</summary>
    private static string Delimited(string header, int from, char close, string key, bool trim)
    {
        int end = header.IndexOf(close, from + 1);
        if (end < 0)
        {
            throw Malformed($"never closes its '{key}'.");
        }
        return trim ? header[(from + 1)..end] : header[from..(end + 1)];
    }

    /// <summary>A bare value — <c>True</c>, <c>False</c> — up to the next comma or brace.</summary>
    private static string BareToken(string header, int from)
    {
        int end = from;
        while (end < header.Length && header[end] != ',' && header[end] != '}')
        {
            end++;
        }
        return header[from..end].Trim();
    }

    /// <summary>Reads <c>(3, 4)</c> or <c>(5,)</c> into its dimensions.</summary>
    private static int[] ParseShape(string shape)
    {
        string inner = shape.Trim();
        if (inner.Length < 2 || inner[0] != '(' || inner[^1] != ')')
        {
            throw Malformed($"has a shape this does not read: {shape}.");
        }

        inner = inner[1..^1];
        var dimensions = new List<int>();
        foreach (string part in inner.Split(','))
        {
            string trimmed = part.Trim();
            if (trimmed.Length == 0)
            {
                continue;
            }
            if (!int.TryParse(trimmed, NumberStyles.None, CultureInfo.InvariantCulture, out int dimension))
            {
                throw Malformed($"has a shape this does not read: {shape}.");
            }
            dimensions.Add(dimension);
        }

        if (dimensions.Count is 0 or > 2)
        {
            throw Malformed(
                $"is {dimensions.Count}-dimensional; only a vector or a matrix is read.");
        }

        return [.. dimensions];
    }

    /// <summary>Builds the magic, version, length and padded dict literal numpy expects.</summary>
    private static byte[] BuildHeader(int[] shape)
    {
        string dimensions = shape.Length == 1
            ? $"{shape[0].ToString(CultureInfo.InvariantCulture)},"
            : string.Join(", ", Array.ConvertAll(shape, d => d.ToString(CultureInfo.InvariantCulture)));

        string dictionary =
            $"{{'descr': '{LittleEndianSingle}', 'fortran_order': False, 'shape': ({dimensions}), }}";

        // The payload starts on a 64-byte boundary, which is what numpy does and what
        // lets a reader map the block without copying it out of alignment.
        int prefix = Magic.Length + 2 + 2;
        int unpadded = prefix + Encoding.ASCII.GetByteCount(dictionary) + 1;
        int padding = (PayloadAlignment - (unpadded % PayloadAlignment)) % PayloadAlignment;
        string padded = dictionary + new string(' ', padding) + "\n";

        byte[] text = Encoding.ASCII.GetBytes(padded);
        var header = new byte[prefix + text.Length];
        Magic.CopyTo(header, 0);
        header[VersionOffset] = 1;
        header[VersionOffset + 1] = 0;
        BinaryPrimitives.WriteUInt16LittleEndian(header.AsSpan(VersionOffset + 2), checked((ushort)text.Length));
        text.CopyTo(header, prefix);
        return header;
    }

    /// <summary>Writes the block itself, little-endian, a slice at a time.</summary>
    /// <remarks>
    /// Sliced on <c>netstandard2.0</c> for the reason the artifact's own base64 writer is
    /// sliced — nothing here should grow a buffer to hold the whole block. <c>net10.0</c>
    /// writes the span straight out, which needs no buffer at all.
    /// </remarks>
    private static void WriteBlock(Stream destination, ReadOnlySpan<float> values)
    {
        ReadOnlySpan<byte> raw = MemoryMarshal.AsBytes(values);
        if (BitConverter.IsLittleEndian)
        {
#if NETSTANDARD2_0
            byte[] buffer = new byte[Math.Min(raw.Length, 240 * 1024)];
            for (int offset = 0; offset < raw.Length; offset += buffer.Length)
            {
                int take = Math.Min(buffer.Length, raw.Length - offset);
                raw.Slice(offset, take).CopyTo(buffer);
                destination.Write(buffer, 0, take);
            }
#else
            destination.Write(raw);
#endif
            return;
        }

        byte[] swapped = new byte[values.Length * sizeof(float)];
        raw.CopyTo(swapped);
        Span<int> words = MemoryMarshal.Cast<byte, int>(swapped.AsSpan());
        for (int i = 0; i < words.Length; i++)
        {
            words[i] = BinaryPrimitives.ReverseEndianness(words[i]);
        }
        destination.Write(swapped, 0, swapped.Length);
    }

    /// <summary>Refuses a shape that does not describe the block it is given.</summary>
    private static void CheckShape(int length, int[] shape)
    {
        if (shape.Length is 0 or > 2)
        {
            throw new ArgumentException(
                $"A shape must have one or two dimensions, not {shape.Length}.", nameof(shape));
        }

        long product = 1;
        foreach (int dimension in shape)
        {
            if (dimension < 0)
            {
                throw new ArgumentException(
                    $"A dimension cannot be negative ({dimension}).", nameof(shape));
            }
            product *= dimension;
        }

        if (product != length)
        {
            throw new ArgumentException(
                $"A shape of {product} elements does not describe {length} values.", nameof(shape));
        }
    }

    private static InvalidDataException Malformed(string what) =>
        new($"A '{SourceName}' file {what}");

    /// <summary>What the restricted parser takes out of the header.</summary>
    private readonly record struct NpyHeader(int[] Shape);
}
