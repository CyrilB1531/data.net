using System.Text;

namespace DataNet.Text.Phonetics;

/// <summary>
/// American Soundex phonetic encoding: an initial letter followed by three digits.
/// </summary>
/// <remarks>
/// <para>
/// Reference behavior: <c>jellyfish.soundex</c>. Non-letters are ignored; the empty
/// string encodes to the empty string. Codes: <c>bfpv→1</c>, <c>cgjkqsxz→2</c>,
/// <c>dt→3</c>, <c>l→4</c>, <c>mn→5</c>, <c>r→6</c>; vowels (and <c>y</c>) separate
/// equal codes, while <c>h</c>/<c>w</c> are transparent.
/// </para>
/// <para>Soundex is an English-oriented heuristic and is not Unicode-aware; it is thread-safe.</para>
/// </remarks>
public static class Soundex
{
    /// <summary>Encodes <paramref name="value"/> to its 4-character Soundex code (or empty).</summary>
    public static string Encode(ReadOnlySpan<char> value)
    {
        // First letter.
        int i = 0;
        char first = '\0';
        while (i < value.Length)
        {
            char c = char.ToUpperInvariant(value[i]);
            i++;
            if (c is >= 'A' and <= 'Z')
            {
                first = c;
                break;
            }
        }
        if (first == '\0')
        {
            return string.Empty;
        }

        var sb = new StringBuilder(4);
        sb.Append(first);
        int previous = Code(first);

        for (; i < value.Length && sb.Length < 4; i++)
        {
            char c = char.ToUpperInvariant(value[i]);
            if (c is < 'A' or > 'Z')
            {
                continue;
            }
            if (c is 'H' or 'W')
            {
                continue; // transparent: do not reset the previous code
            }
            if (c is 'A' or 'E' or 'I' or 'O' or 'U' or 'Y')
            {
                previous = 0; // vowels separate equal consonant codes
                continue;
            }

            int code = Code(c);
            if (code != previous)
            {
                sb.Append((char)('0' + code));
            }
            previous = code;
        }

        while (sb.Length < 4)
        {
            sb.Append('0');
        }
        return sb.ToString();
    }

    /// <summary>Encodes a string to its Soundex code.</summary>
    public static string Encode(string value) => Encode(value.AsSpan());

    private static int Code(char c) => c switch
    {
        'B' or 'F' or 'P' or 'V' => 1,
        'C' or 'G' or 'J' or 'K' or 'Q' or 'S' or 'X' or 'Z' => 2,
        'D' or 'T' => 3,
        'L' => 4,
        'M' or 'N' => 5,
        'R' => 6,
        _ => 0, // vowels, H, W, Y
    };
}
