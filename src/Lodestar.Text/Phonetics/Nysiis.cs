using System.Text;

namespace Lodestar.Text.Phonetics;

// SonarLint S3776: cognitive complexity: a faithful implementation of a published rule-engine; decomposing it would break the 1:1 mapping with the reference that makes divergences auditable.
#pragma warning disable S3776

/// <summary>
/// NYSIIS (New York State Identification and Intelligence System) phonetic encoding.
/// </summary>
/// <remarks>
/// Reference behavior: <c>jellyfish.nysiis</c> — the modern, non-truncated variant
/// (the original 6-character limit is not applied). Non-letters are ignored; the
/// empty string encodes to the empty string. English-oriented heuristic, not
/// Unicode-aware; thread-safe.
/// </remarks>
public static class Nysiis
{
    /// <summary>Encodes a string to its NYSIIS code.</summary>
    public static string Encode(string value) => Encode(value.AsSpan());

    /// <summary>Encodes <paramref name="value"/> to its NYSIIS code (or empty).</summary>
    public static string Encode(ReadOnlySpan<char> value)
    {
        // Keep letters only, uppercase.
        var buffer = new StringBuilder(value.Length);
        foreach (char ch in value)
        {
            if (char.IsLetter(ch))
            {
                buffer.Append(char.ToUpperInvariant(ch));
            }
        }
        if (buffer.Length == 0)
        {
            return string.Empty;
        }

        string s = buffer.ToString();

        // Prefix transforms.
        if (s.StartsWith("MAC", StringComparison.Ordinal))
        {
            s = "MCC" + s[3..];
        }
        else if (s.StartsWith("KN", StringComparison.Ordinal))
        {
            s = "N" + s[2..];
        }
        else if (s.StartsWith('K'))
        {
            s = "C" + s[1..];
        }
        else if (s.StartsWith("PH", StringComparison.Ordinal) || s.StartsWith("PF", StringComparison.Ordinal))
        {
            s = "FF" + s[2..];
        }
        else if (s.StartsWith("SCH", StringComparison.Ordinal))
        {
            s = "SSS" + s[3..];
        }

        // Suffix transforms.
        if (s.EndsWith("EE", StringComparison.Ordinal) || s.EndsWith("IE", StringComparison.Ordinal))
        {
            s = s[..^2] + "Y";
        }
        else if (s.EndsWith("DT", StringComparison.Ordinal) || s.EndsWith("RT", StringComparison.Ordinal)
            || s.EndsWith("RD", StringComparison.Ordinal) || s.EndsWith("NT", StringComparison.Ordinal)
            || s.EndsWith("ND", StringComparison.Ordinal))
        {
            s = s[..^2] + "D";
        }

        var key = new StringBuilder();
        key.Append(s[0]);

        int i = 1;
        while (i < s.Length)
        {
            char c = s[i];
            char next = i + 1 < s.Length ? s[i + 1] : '\0';
            char prev = s[i - 1];
            string replacement;

            if (c == 'E' && next == 'V')
            {
                replacement = "AF";
                i++;
            }
            else if (IsVowel(c))
            {
                replacement = "A";
            }
            else if (c == 'Q')
            {
                replacement = "G";
            }
            else if (c == 'Z')
            {
                replacement = "S";
            }
            else if (c == 'M')
            {
                replacement = "N";
            }
            else if (c == 'K')
            {
                replacement = next == 'N' ? "N" : "C";
            }
            else if (c == 'S' && next == 'C' && i + 2 < s.Length && s[i + 2] == 'H')
            {
                replacement = "SSS";
                i += 2;
            }
            else if (c == 'P' && next == 'H')
            {
                replacement = "FF";
                i++;
            }
            else if (c == 'H')
            {
                // Kept between two vowels; after vowel+consonant it becomes A; after
                // a consonant it repeats that consonant, which then collapses away.
                if (!IsVowel(prev))
                {
                    replacement = prev.ToString();
                }
                else if (IsVowel(next))
                {
                    replacement = "H";
                }
                else
                {
                    replacement = "A";
                }
            }
            else if (c == 'W')
            {
                // W after a vowel takes that vowel; otherwise it stays.
                replacement = IsVowel(prev) ? prev.ToString() : "W";
            }
            else
            {
                replacement = c.ToString();
            }

            // Append each replacement char, collapsing adjacent duplicates.
            foreach (char rc in replacement)
            {
                if (key.Length == 0 || rc != key[^1])
                {
                    key.Append(rc);
                }
            }

            i++;
        }

        // Trailing cleanups.
        if (key.Length > 1 && key[^1] == 'S')
        {
            key.Length--;
        }
        if (key.Length > 1 && key[^1] == 'Y' && key[^2] == 'A')
        {
            key.Length--;      // "AY" -> "A"
            key[^1] = 'Y';     // then represent as trailing Y
        }
        if (key.Length > 1 && key[^1] == 'A')
        {
            key.Length--;
        }

        return key.ToString();
    }

    private static bool IsVowel(char c) => c is 'A' or 'E' or 'I' or 'O' or 'U';
}
