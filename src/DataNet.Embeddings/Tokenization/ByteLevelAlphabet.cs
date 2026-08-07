namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// The reversible byte-to-character alphabet GPT-2 tokenizes through.
/// </summary>
/// <remarks>
/// <para>
/// Byte-level BPE never sees text: it sees the 256 possible byte values, each
/// standing in as one printable character so that a merge table over characters
/// can address arbitrary bytes. That substitution is what makes the tokenizer
/// lossless over any UTF-8 byte sequence at all, valid text or not: no byte is
/// ever unrepresentable. The caller side of that promise is narrower, because
/// the input a tokenizer actually takes is a .NET <see cref="string"/> rather
/// than raw bytes -- <see cref="BpeTokenizer.Encode(string)"/> re-encodes it to
/// UTF-8 first, and a lone surrogate has no UTF-8 encoding to begin with, so
/// that one input shape is out of scope for this guarantee rather than covered
/// by it. See <see cref="BpeTokenizer.Encode(string)"/> for what happens then.
/// </para>
/// <para>
/// The table is <em>built from the published construction</em> rather than
/// transcribed: the printable ranges <c>!</c>–<c>~</c>, <c>¡</c>–<c>¬</c> and
/// <c>®</c>–<c>ÿ</c> stand for themselves, and the 68 bytes left over — the
/// control characters, the space, the delete, and the three holes in Latin-1 —
/// take <c>U+0100</c> onwards in byte order. A space therefore appears as
/// <c>Ġ</c> (U+0120) and a newline as <c>Ċ</c> (U+010A), which is why GPT-2
/// tokens look the way they do.
/// </para>
/// </remarks>
internal static class ByteLevelAlphabet
{
    private static readonly char[] Forward = BuildForward();

    // 188 of the 256 characters are Latin-1, and the rest run to U+0143, so a
    // dense array over that range costs 324 slots and turns the inverse into an
    // array index instead of a dictionary probe in the decode loop.
    private const char MaxMapped = 'Ń';
    private static readonly byte[] Inverse = BuildInverse();
    private static readonly bool[] Mapped = BuildMapped();

    /// <summary>The character standing for <paramref name="value"/>.</summary>
    public static char ToChar(byte value) => Forward[value];

    /// <summary>The byte <paramref name="mapped"/> stands for, when it stands for one.</summary>
    public static bool TryToByte(char mapped, out byte value)
    {
        if (mapped <= MaxMapped && Mapped[mapped])
        {
            value = Inverse[mapped];
            return true;
        }
        value = 0;
        return false;
    }

    private static char[] BuildForward()
    {
        var forward = new char[256];
        var taken = new bool[256];
        foreach ((int from, int to) in new[] { (0x21, 0x7E), (0xA1, 0xAC), (0xAE, 0xFF) })
        {
            for (int b = from; b <= to; b++)
            {
                forward[b] = (char)b;
                taken[b] = true;
            }
        }
        int spare = 0;
        for (int b = 0; b < 256; b++)
        {
            if (!taken[b])
            {
                forward[b] = (char)(256 + spare);
                spare++;
            }
        }
        return forward;
    }

    private static byte[] BuildInverse()
    {
        var inverse = new byte[MaxMapped + 1];
        for (int b = 0; b < 256; b++)
        {
            inverse[Forward[b]] = (byte)b;
        }
        return inverse;
    }

    private static bool[] BuildMapped()
    {
        var mapped = new bool[MaxMapped + 1];
        for (int b = 0; b < 256; b++)
        {
            mapped[Forward[b]] = true;
        }
        return mapped;
    }
}
