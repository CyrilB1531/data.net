using System.Text;

namespace DataNet.Text.Phonetics;

/// <summary>
/// Metaphone phonetic encoding (Lawrence Philips, 1990).
/// </summary>
/// <remarks>
/// <para>
/// Reference behavior: <c>jellyfish.metaphone</c>. Codes use the symbols
/// <c>B X S K J T F H L M N P R 0 W Y</c> (where <c>X</c> is the "sh" sound and
/// <c>0</c> the "th" sound); vowels appear only when they start the word.
/// Non-letters are ignored; the empty string encodes to the empty string.
/// </para>
/// <para>English-oriented heuristic, not Unicode-aware; thread-safe.</para>
/// </remarks>
public static class Metaphone
{
    /// <summary>Encodes a string to its Metaphone code.</summary>
    public static string Encode(string value) => Encode(value.AsSpan());

    /// <summary>Encodes <paramref name="value"/> to its Metaphone code (or empty).</summary>
    public static string Encode(ReadOnlySpan<char> value)
    {
        var letters = new StringBuilder(value.Length);
        foreach (char ch in value)
        {
            if (char.IsLetter(ch))
            {
                letters.Append(char.ToUpperInvariant(ch));
            }
        }
        if (letters.Length == 0)
        {
            return string.Empty;
        }

        string w = letters.ToString();

        // Initial-cluster exceptions.
        if (w.Length >= 2)
        {
            string p2 = w[..2];
            if (p2 is "AE" or "GN" or "KN" or "PN" or "WR")
            {
                w = w[1..];
            }
            else if (p2 == "WH")
            {
                w = "W" + w[2..];
            }
        }

        // Collapse runs of the same letter (except 'C') so later context checks see
        // the reduced form (e.g. "SSION" -> "SION" -> X).
        var reduced = new StringBuilder(w.Length);
        for (int k = 0; k < w.Length; k++)
        {
            if (k > 0 && w[k] == w[k - 1] && w[k] != 'C')
            {
                continue;
            }
            reduced.Append(w[k]);
        }
        w = reduced.ToString();

        var result = new StringBuilder(w.Length);
        int n = w.Length;

        for (int i = 0; i < n; i++)
        {
            char c = w[i];
            char prev = i > 0 ? w[i - 1] : '\0';
            char next = i + 1 < n ? w[i + 1] : '\0';
            char next2 = i + 2 < n ? w[i + 2] : '\0';

            switch (c)
            {
                case 'A' or 'E' or 'I' or 'O' or 'U':
                    if (i == 0)
                    {
                        result.Append(c);
                    }
                    break;

                case 'B':
                    // Silent in a terminal "MB".
                    if (!(i == n - 1 && prev == 'M'))
                    {
                        result.Append('B');
                    }
                    break;

                case 'C':
                    if (next == 'H')
                    {
                        result.Append('X'); // CH -> X
                    }
                    else if (next == 'I' && next2 == 'A')
                    {
                        result.Append('X'); // CIA
                    }
                    else if (next is 'I' or 'E' or 'Y')
                    {
                        result.Append('S'); // CI/CE/CY
                    }
                    else
                    {
                        result.Append('K');
                    }
                    break;

                case 'D':
                    if (next == 'G' && next2 is 'I' or 'E' or 'Y')
                    {
                        result.Append('J');
                        i++; // consume the G of DGE/DGI/DGY
                    }
                    else
                    {
                        result.Append('T');
                    }
                    break;

                case 'F':
                    result.Append('F');
                    break;

                case 'G':
                    if (next == 'H')
                    {
                        if (next2 == 'T')
                        {
                            i++; // "GHT": both G and H silent (Knight, Wright)
                        }
                        else
                        {
                            result.Append('K'); // GH -> K, the H is kept by the H rule (Rough, Ghost)
                        }
                    }
                    else if (next == 'N' && i + 2 >= n)
                    {
                        i++; // terminal GN: both letters silent
                    }
                    else if (next is 'I' or 'E' or 'Y')
                    {
                        result.Append('J');
                    }
                    else
                    {
                        result.Append('K');
                    }
                    break;

                case 'H':
                    // Silent when a preceding consonant already consumed the digraph
                    // (CH/SH/TH/PH), and when it sits between a vowel and a consonant.
                    if (prev is 'C' or 'S' or 'P' or 'T')
                    {
                        break;
                    }
                    if (IsVowel(prev) && !IsVowel(next))
                    {
                        break;
                    }
                    result.Append('H');
                    break;

                case 'J':
                    result.Append('J');
                    break;

                case 'K':
                    if (prev != 'C')
                    {
                        result.Append('K');
                    }
                    break;

                case 'L':
                    result.Append('L');
                    break;

                case 'M':
                    result.Append('M');
                    break;

                case 'N':
                    result.Append('N');
                    break;

                case 'P':
                    result.Append(next == 'H' ? 'F' : 'P');
                    break;

                case 'Q':
                    result.Append('K');
                    break;

                case 'R':
                    result.Append('R');
                    break;

                case 'S':
                    if (next == 'H' || (next == 'I' && next2 is 'O' or 'A'))
                    {
                        result.Append('X');
                    }
                    else
                    {
                        result.Append('S');
                    }
                    break;

                case 'T':
                    if (next == 'I' && next2 is 'O' or 'A')
                    {
                        result.Append('X');
                    }
                    else if (next == 'H')
                    {
                        result.Append('0');
                    }
                    else
                    {
                        result.Append('T');
                    }
                    break;

                case 'V':
                    result.Append('F');
                    break;

                case 'W' or 'Y':
                    if (IsVowel(next))
                    {
                        result.Append(c);
                    }
                    break;

                case 'X':
                    result.Append(i == 0 ? "S" : "KS"); // initial X sounds like S
                    break;

                case 'Z':
                    result.Append('S');
                    break;
            }
        }

        return result.ToString();
    }

    private static bool IsVowel(char c) => c is 'A' or 'E' or 'I' or 'O' or 'U';
}
