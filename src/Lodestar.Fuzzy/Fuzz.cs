using Lodestar.Text.Distances;

namespace Lodestar.Fuzzy;

/// <summary>
/// Applied fuzzy-matching ratios, reproducing <c>rapidfuzz.fuzz</c>.
/// </summary>
/// <remarks>
/// Scores are in <c>[0, 100]</c>, with no preprocessing: comparisons are
/// case-sensitive and punctuation is kept, as in rapidfuzz and unlike the old
/// fuzzywuzzy defaults. <see cref="Ratio"/> is the Indel similarity ×100 —
/// <em>not</em> Levenshtein, the commonest confusion here. Tokenization splits
/// on runs of whitespace, as Python's <c>str.split()</c>. Thread-safe.
/// </remarks>
public static class Fuzz
{
    /// <summary>Indel similarity ×100 — the base ratio.</summary>
    public static double Ratio(string a, string b)
    {
        Guard.NotNull(a);
        Guard.NotNull(b);
        return 100.0 * Indel.NormalizedSimilarity(a, b);
    }

    /// <summary>Best <see cref="Ratio"/> between the shorter string and any substring of the longer.</summary>
    public static double PartialRatio(string a, string b)
    {
        Guard.NotNull(a);
        Guard.NotNull(b);
        if (a.Length == 0 && b.Length == 0)
        {
            return 100.0;
        }
        if (a.Length == 0 || b.Length == 0)
        {
            return 0.0;
        }

        // Slide the shorter string over the longer. When lengths are equal, neither
        // is strictly shorter, so try both orientations (matches rapidfuzz).
        if (a.Length < b.Length)
        {
            return SlideMax(a, b);
        }
        if (b.Length < a.Length)
        {
            return SlideMax(b, a);
        }
        return Math.Max(SlideMax(a, b), SlideMax(b, a));
    }

    /// <summary>
    /// Slides <paramref name="pattern"/> across <paramref name="text"/> and returns the
    /// best ratio over the aligned windows (full length in the interior, truncated at edges).
    /// </summary>
    private static double SlideMax(string pattern, string text)
    {
        int m = pattern.Length;
        int n = text.Length;
        if (m == 0 || n == 0)
        {
            return 0.0;
        }

        double best = 0;
        for (int offset = -(m - 1); offset < n; offset++)
        {
            int start = Math.Max(0, offset);
            int end = Math.Min(n, offset + m);
            if (start >= end)
            {
                continue;
            }
            double r = 100.0 * Indel.NormalizedSimilarity(pattern, text.AsSpan(start, end - start));
            if (r > best)
            {
                best = r;
                if (best >= 100.0)
                {
                    return 100.0;
                }
            }
        }
        return best;
    }

    /// <summary><see cref="Ratio"/> after splitting, sorting and rejoining the tokens of each string.</summary>
    public static double TokenSortRatio(string a, string b)
    {
        Guard.NotNull(a);
        Guard.NotNull(b);
        return Ratio(SortedJoin(Tokenize(a)), SortedJoin(Tokenize(b)));
    }

    /// <summary>Token-set ratio: compares the shared tokens against each string's full sorted token set.</summary>
    public static double TokenSetRatio(string a, string b)
    {
        return TokenSet(a, b, partial: false);
    }

    /// <summary><see cref="PartialRatio"/> on sorted-token strings.</summary>
    public static double PartialTokenSortRatio(string a, string b)
    {
        Guard.NotNull(a);
        Guard.NotNull(b);
        return PartialRatio(SortedJoin(Tokenize(a)), SortedJoin(Tokenize(b)));
    }

    /// <summary>Token-set ratio using <see cref="PartialRatio"/> for the comparisons.</summary>
    public static double PartialTokenSetRatio(string a, string b)
    {
        return TokenSet(a, b, partial: true);
    }

    /// <summary>
    /// Weighted ratio: combines the base, partial and token ratios with rapidfuzz's
    /// length-dependent weighting and returns the maximum.
    /// </summary>
    public static double WRatio(string a, string b)
    {
        Guard.NotNull(a);
        Guard.NotNull(b);
        if (a.Length == 0 || b.Length == 0)
        {
            return 0.0;
        }

        const double unbaseScale = 0.95;
        double lenRatio = (double)Math.Max(a.Length, b.Length) / Math.Min(a.Length, b.Length);

        double best = Ratio(a, b);

        if (lenRatio < 1.5)
        {
            best = Math.Max(best, TokenSortRatio(a, b) * unbaseScale);
            best = Math.Max(best, TokenSetRatio(a, b) * unbaseScale);
            return best;
        }

        double partialScale = lenRatio > 8.0 ? 0.6 : 0.9;
        best = Math.Max(best, PartialRatio(a, b) * partialScale);
        best = Math.Max(best, PartialTokenSortRatio(a, b) * unbaseScale * partialScale);
        best = Math.Max(best, PartialTokenSetRatio(a, b) * unbaseScale * partialScale);
        return best;
    }

    private static string[] Tokenize(string s) =>
        s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);

    private static string SortedJoin(string[] tokens)
    {
        var copy = (string[])tokens.Clone();
        Array.Sort(copy, StringComparer.Ordinal);
        return string.Join(" ", copy);
    }

    private static double TokenSet(string a, string b, bool partial)
    {
        // Sorted, deduplicated arrays rather than five SortedSet<string>: the sets hold a
        // handful of tokens each, and a red-black node apiece was most of what #494 measured.
        string[] setA = Tokenize(a);
        string[] setB = Tokenize(b);
        int countA = SortDistinct(setA);
        int countB = SortDistinct(setB);
        if (countA == 0 && countB == 0)
        {
            return 0.0; // rapidfuzz returns 0 for token-set with no tokens
        }

        var intersection = new List<string>();
        var diffA = new List<string>();
        var diffB = new List<string>();
        Partition(setA, countA, setB, countB, intersection, diffA, diffB);

        string sect = string.Join(" ", intersection);
        string combinedA = Join(intersection, diffA);
        string combinedB = Join(intersection, diffB);

        Func<string, string, double> score = partial ? PartialRatio : Ratio;
        double r1 = score(sect, combinedA);
        double r2 = score(sect, combinedB);
        double r3 = score(combinedA, combinedB);
        return Math.Max(r1, Math.Max(r2, r3));
    }

    /// <summary>
    /// Sorts ordinally and moves the distinct tokens to the front, returning how many there
    /// are — the order a <c>SortedSet</c> enumerated, without a second array to hold it.
    /// </summary>
    private static int SortDistinct(string[] tokens)
    {
        if (tokens.Length <= 1)
        {
            return tokens.Length;
        }

        Array.Sort(tokens, StringComparer.Ordinal);
        int kept = 1;
        for (int i = 1; i < tokens.Length; i++)
        {
            if (!string.Equals(tokens[i], tokens[kept - 1], StringComparison.Ordinal))
            {
                tokens[kept++] = tokens[i];
            }
        }
        return kept;
    }

    /// <summary>Splits two sorted, distinct token lists into shared and either-side-only.</summary>
    /// <remarks>
    /// One merge over both, where the set version walked each tree three times. Order is the
    /// sorted one either way, which is what the joins below depend on.
    /// </remarks>
    private static void Partition(
        string[] first,
        int firstCount,
        string[] second,
        int secondCount,
        List<string> shared,
        List<string> onlyFirst,
        List<string> onlySecond)
    {
        int i = 0;
        int j = 0;
        while (i < firstCount && j < secondCount)
        {
            int order = string.CompareOrdinal(first[i], second[j]);
            if (order == 0)
            {
                shared.Add(first[i++]);
                j++;
            }
            else if (order < 0)
            {
                onlyFirst.Add(first[i++]);
            }
            else
            {
                onlySecond.Add(second[j++]);
            }
        }

        for (; i < firstCount; i++)
        {
            onlyFirst.Add(first[i]);
        }
        for (; j < secondCount; j++)
        {
            onlySecond.Add(second[j]);
        }
    }

    private static string Join(List<string> first, List<string> second)
    {
        if (second.Count == 0)
        {
            return string.Join(" ", first);
        }

        var all = new List<string>(first.Count + second.Count);
        all.AddRange(first);
        all.AddRange(second);
        return string.Join(" ", all).Trim();
    }
}
