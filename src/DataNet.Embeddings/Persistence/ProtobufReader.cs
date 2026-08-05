namespace DataNet.Embeddings.Persistence;

/// <summary>
/// The slice of the protocol-buffers wire format a <c>spiece.model</c> needs:
/// varints, length-delimited fields and 32-bit floats.
/// </summary>
/// <remarks>
/// <para>
/// Hand-written on purpose. Reading four field types is a hundred lines; taking
/// <c>protobuf-net</c> or <c>Google.Protobuf</c> would put a runtime dependency
/// into a package whose selling point is not having any, to parse one file
/// format. The wire format is also frozen, so this cannot rot.
/// </para>
/// <para>
/// Every read is bounds-checked against the buffer: the input is a downloaded
/// file, and a truncated length prefix must produce
/// <see cref="InvalidDataException"/> rather than a read past the end.
/// </para>
/// </remarks>
internal ref struct ProtobufReader
{
    /// <summary>Wire type 0 — a base-128 varint.</summary>
    public const int WireVarint = 0;

    /// <summary>Wire type 1 — eight fixed bytes.</summary>
    public const int WireFixed64 = 1;

    /// <summary>Wire type 2 — a length-prefixed byte run.</summary>
    public const int WireLengthDelimited = 2;

    /// <summary>Wire type 5 — four fixed bytes.</summary>
    public const int WireFixed32 = 5;

    private readonly ReadOnlySpan<byte> _data;
#if NETSTANDARD2_0
    private readonly byte[] _scratch;
#endif
    private int _position;

    public ProtobufReader(ReadOnlySpan<byte> data)
    {
        _data = data;
#if NETSTANDARD2_0
        _scratch = new byte[4];
#endif
        _position = 0;
    }

    /// <summary>Whether every byte has been consumed.</summary>
    public readonly bool End => _position >= _data.Length;

    /// <summary>Reads the next field header, or returns <c>false</c> at the end of the buffer.</summary>
    public bool TryReadTag(out int fieldNumber, out int wireType)
    {
        if (End)
        {
            fieldNumber = 0;
            wireType = 0;
            return false;
        }

        ulong tag = ReadVarint();
        fieldNumber = (int)(tag >> 3);
        wireType = (int)(tag & 0x7);
        if (fieldNumber <= 0)
        {
            throw Malformed($"field number {fieldNumber} is not valid");
        }
        return true;
    }

    /// <summary>Reads a base-128 varint.</summary>
    public ulong ReadVarint()
    {
        ulong result = 0;
        for (int shift = 0; shift < 64; shift += 7)
        {
            if (_position >= _data.Length)
            {
                throw Malformed("a varint runs past the end of the input");
            }
            byte b = _data[_position++];
            result |= (ulong)(b & 0x7F) << shift;
            if ((b & 0x80) == 0)
            {
                return result;
            }
        }
        throw Malformed("a varint is longer than ten bytes");
    }

    /// <summary>Reads a signed 32-bit field, which the wire format stores as a 64-bit varint when negative.</summary>
    public int ReadInt32() => unchecked((int)(long)ReadVarint());

    /// <summary>Reads a length-prefixed run of bytes.</summary>
    public ReadOnlySpan<byte> ReadLengthDelimited()
    {
        ulong length = ReadVarint();
        if (length > (ulong)(_data.Length - _position))
        {
            throw Malformed($"a length-delimited field declares {length} bytes but only {_data.Length - _position} remain");
        }

        ReadOnlySpan<byte> slice = _data.Slice(_position, (int)length);
        _position += (int)length;
        return slice;
    }

    /// <summary>Reads a little-endian 32-bit float.</summary>
    public float ReadFloat()
    {
        if (_data.Length - _position < 4)
        {
            throw Malformed("a 32-bit field runs past the end of the input");
        }

#if NETSTANDARD2_0
        // netstandard2.0 has no Int32BitsToSingle, so the bytes go through a buffer.
        // It is allocated once per reader — one per piece, which is why the modern
        // target below composes the bits instead.
        _scratch[0] = _data[_position];
        _scratch[1] = _data[_position + 1];
        _scratch[2] = _data[_position + 2];
        _scratch[3] = _data[_position + 3];
        _position += 4;
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(_scratch);
        }
        return BitConverter.ToSingle(_scratch, 0);
#else
        // Explicit shifts, so the wire's little-endian order is honoured whatever
        // the machine's is, and nothing is allocated.
        int bits = _data[_position]
            | (_data[_position + 1] << 8)
            | (_data[_position + 2] << 16)
            | (_data[_position + 3] << 24);
        _position += 4;
        return BitConverter.Int32BitsToSingle(bits);
#endif
    }

    /// <summary>Consumes a field whose contents are of no interest.</summary>
    public void SkipField(int wireType)
    {
        switch (wireType)
        {
            case WireVarint:
                _ = ReadVarint();
                break;
            case WireFixed64:
                Advance(8);
                break;
            case WireLengthDelimited:
                _ = ReadLengthDelimited();
                break;
            case WireFixed32:
                Advance(4);
                break;
            default:
                throw Malformed($"wire type {wireType} is not supported");
        }
    }

    private void Advance(int count)
    {
        if (_data.Length - _position < count)
        {
            throw Malformed("a fixed-width field runs past the end of the input");
        }
        _position += count;
    }

    private static InvalidDataException Malformed(string detail) =>
        new($"The SentencePiece model is not a well-formed protocol-buffers message: {detail}.");
}
