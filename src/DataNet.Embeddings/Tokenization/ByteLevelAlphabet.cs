namespace DataNet.Embeddings.Tokenization;

/// <summary>The reversible byte-to-character alphabet GPT-2 tokenizes through.</summary>
/// <remarks>
/// Byte-level BPE sees the 256 byte values, each standing in for one printable
/// character, so a merge table over characters addresses arbitrary bytes — a
/// lone surrogate has no UTF-8 encoding and is out of scope; see
/// <see cref="BpeTokenizer.Encode(string)"/>. Built from the published
/// <c>bytes_to_unicode</c> construction, asserted against <c>ByteLevel.alphabet()</c>
/// by <c>tools/generate_oracles.py</c>.
/// </remarks>
internal static class ByteLevelAlphabet
{
    private static readonly char[] Forward = BuildForward();

    // 188 of the 256 map to Latin-1; the rest run to U+0143. A dense array over that
    // range turns the inverse into an index instead of a dictionary probe.
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
