using System.Text;

namespace Lodestar.Text.Phonetics;

// SonarLint S3776: cognitive complexity: a faithful implementation of a published rule-engine; decomposing it would break the 1:1 mapping with the reference that makes divergences auditable.
#pragma warning disable S3776

/// <summary>
/// Match Rating Approach: a phonetic codex, and the rule for deciding whether two
/// codices name a match (Western Airlines, 1977).
/// </summary>
/// <remarks>
/// Reference behavior: <c>jellyfish.match_rating_codex</c> and
/// <c>jellyfish.match_rating_comparison</c>. Unlike <see cref="Soundex"/>,
/// <see cref="Metaphone"/> and <see cref="Nysiis"/>, a non-letter, non-space
/// character is <b>refused</b>. English-oriented, not Unicode-aware; thread-safe.
/// </remarks>
public static class MatchRatingApproach
{
    /// <summary>Encodes a string to its Match Rating codex.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="value"/> holds a character that is
    /// neither a letter nor a space.</exception>
    public static string Codex(string value)
    {
        Guard.NotNull(value);
        return CodexCore(value.AsSpan(), nameof(value));
    }

    /// <summary>Encodes <paramref name="value"/> to its Match Rating codex (or empty).</summary>
    /// <exception cref="ArgumentException"><paramref name="value"/> holds a character that is
    /// neither a letter nor a space.</exception>
    public static string Codex(ReadOnlySpan<char> value) => CodexCore(value, nameof(value));

    /// <summary>
    /// Compares the Match Rating codices of two names. Returns <c>null</c>, rather than
    /// <c>false</c>, when the codices' lengths differ too much for a rating to mean anything.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="a"/> or <paramref name="b"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="a"/> or <paramref name="b"/> holds a
    /// character that is neither a letter nor a space.</exception>
    public static bool? Compare(string a, string b)
    {
        Guard.NotNull(a);
        Guard.NotNull(b);
        return Compare(a.AsSpan(), b.AsSpan());
    }

    /// <summary>
    /// Compares the Match Rating codices of <paramref name="a"/> and <paramref name="b"/>.
    /// Returns <c>null</c>, rather than <c>false</c>, when the codices' lengths differ too much
    /// for a rating to mean anything.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="a"/> or <paramref name="b"/> holds a
    /// character that is neither a letter nor a space.</exception>
    public static bool? Compare(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        string codexA = CodexCore(a, nameof(a));
        string codexB = CodexCore(b, nameof(b));

        // Lengths 3 or more apart cannot be rated at all — measured against jellyfish 1.2.1.
        if (Math.Abs(codexA.Length - codexB.Length) >= 3)
        {
            return null;
        }

        int minimumRating = MinimumRating(codexA.Length + codexB.Length);
        int similarityRating = SimilarityRating(codexA, codexB);
        return similarityRating >= minimumRating;
    }

    private static string CodexCore(ReadOnlySpan<char> value, string paramName)
    {
        foreach (char ch in value)
        {
            if (!char.IsLetter(ch) && ch != ' ')
            {
                throw new ArgumentException(
                    $"'{ch}' (U+{(int)ch:X4}) is neither a letter nor a space.", paramName);
            }
        }
        if (value.Length == 0)
        {
            return string.Empty;
        }

        // Compared against the *raw* previous character, not the previously kept one, so
        // doubles collapse even across a dropped vowel ("Mississippi" -> "MSSP").
        var kept = new StringBuilder(value.Length);
        char prevRaw = '\0';
        for (int i = 0; i < value.Length; i++)
        {
            char c = char.ToUpperInvariant(value[i]);
            bool isVowel = c is 'A' or 'E' or 'I' or 'O' or 'U';
            if (i == 0 || (!isVowel && c != prevRaw))
            {
                kept.Append(c);
            }
            prevRaw = c;
        }

        return kept.Length > 6
            ? kept.ToString(0, 3) + kept.ToString(kept.Length - 3, 3)
            : kept.ToString();
    }

    // Combined-codex-length bucket, coarser the longer the codices are — measured by
    // bisection against jellyfish 1.2.1, not a textbook table (see the reference page).
    private static int MinimumRating(int combinedLength) => combinedLength switch
    {
        <= 4 => 5,
        <= 7 => 4,
        <= 11 => 3,
        _ => 2,
    };

    // Cancel same-index characters from the start, then cancel again over what is left,
    // from the end. What survives both passes on the longer side is the unmatched count.
    private static int SimilarityRating(string codexA, string codexB)
    {
        (StringBuilder residualA, StringBuilder residualB) = CancelFromStart(codexA, codexB);
        (int unmatchedA, int unmatchedB) = CancelFromEnd(residualA, residualB);
        return 6 - Math.Max(unmatchedA, unmatchedB);
    }

    private static (StringBuilder ResidualA, StringBuilder ResidualB) CancelFromStart(string codexA, string codexB)
    {
        var residualA = new StringBuilder();
        var residualB = new StringBuilder();
        int n = Math.Max(codexA.Length, codexB.Length);
        for (int i = 0; i < n; i++)
        {
            char? ca = i < codexA.Length ? codexA[i] : null;
            char? cb = i < codexB.Length ? codexB[i] : null;
            if (ca == cb)
            {
                continue;
            }
            if (ca.HasValue)
            {
                residualA.Append(ca.Value);
            }
            if (cb.HasValue)
            {
                residualB.Append(cb.Value);
            }
        }
        return (residualA, residualB);
    }

    private static (int UnmatchedA, int UnmatchedB) CancelFromEnd(StringBuilder residualA, StringBuilder residualB)
    {
        int unmatchedA = 0;
        int unmatchedB = 0;
        int n = Math.Max(residualA.Length, residualB.Length);
        for (int i = 0; i < n; i++)
        {
            int ia = residualA.Length - 1 - i;
            int ib = residualB.Length - 1 - i;
            char? ca = ia >= 0 ? residualA[ia] : null;
            char? cb = ib >= 0 ? residualB[ib] : null;
            if (ca == cb)
            {
                continue;
            }
            if (ca.HasValue)
            {
                unmatchedA++;
            }
            if (cb.HasValue)
            {
                unmatchedB++;
            }
        }
        return (unmatchedA, unmatchedB);
    }
}
