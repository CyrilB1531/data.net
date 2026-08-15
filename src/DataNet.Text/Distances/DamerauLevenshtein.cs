using System.Buffers;
using DataNet.Text.Internal;

namespace DataNet.Text.Distances;

// SonarLint S3776: cognitive complexity: a faithful implementation of a published rule-engine; decomposing it would break the 1:1 mapping with the reference that makes divergences auditable.
// SonarLint S4136: the overloads are grouped by concern, with the generic core deliberately last.
#pragma warning disable S3776, S4136

/// <summary>
/// Unrestricted Damerau-Levenshtein (Lowrance-Wagner: insertions, deletions,
/// substitutions and transpositions of adjacent characters, a substring editable
/// more than once, unlike <see cref="Osa"/>). A true metric at unit costs.
/// </summary>
/// <remarks>
/// Reference behavior: <c>rapidfuzz.distance.DamerauLevenshtein</c>; see
/// <c>docs/equivalence.md</c>. See <see cref="TextElement"/> for the UTF-16
/// vs code-point choice. All members are stateless and thread-safe.
/// </remarks>
public static class DamerauLevenshtein
{
    /// <summary>Computes the Damerau-Levenshtein distance between <paramref name="a"/> and <paramref name="b"/>.</summary>
    public static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
    {
        return element == TextElement.CodePoint
            ? DistanceCodePoints(a, b, out _, out _)
            : Distance<char>(a, b);
    }

    /// <summary>Length-normalized distance in <c>[0, 1]</c>: <c>distance / max(len(a), len(b))</c>.</summary>
    public static double NormalizedDistance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
    {
        int distance;
        int maxLen;
        if (element == TextElement.CodePoint)
        {
            distance = DistanceCodePoints(a, b, out int lenA, out int lenB);
            maxLen = Math.Max(lenA, lenB);
        }
        else
        {
            distance = Distance<char>(a, b);
            maxLen = Math.Max(a.Length, b.Length);
        }

        return maxLen == 0 ? 0.0 : (double)distance / maxLen;
    }

    /// <summary>Length-normalized similarity in <c>[0, 1]</c>: <c>1 - NormalizedDistance</c>.</summary>
    public static double NormalizedSimilarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
    {
        return 1.0 - NormalizedDistance(a, b, element);
    }

    /// <summary>Computes the Damerau-Levenshtein distance over any sequence of equatable elements.</summary>
    public static int Distance<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b)
        where T : IEquatable<T>
    {
        int m = a.Length;
        int n = b.Length;
        if (m == 0)
        {
            return n;
        }
        if (n == 0)
        {
            return m;
        }

        // Matrix indexed from -1..m and -1..n via a +1 offset; width = n + 2.
        int width = n + 2;
        int maxDistance = m + n;
        int[] rented = ArrayPool<int>.Shared.Rent((m + 2) * width);
        try
        {
            Span<int> d = rented.AsSpan(0, (m + 2) * width);
            int Idx(int i, int j) => (i + 1) * width + (j + 1);

            d[Idx(-1, -1)] = maxDistance;
            for (int i = 0; i <= m; i++)
            {
                d[Idx(i, -1)] = maxDistance;
                d[Idx(i, 0)] = i;
            }
            for (int j = 0; j <= n; j++)
            {
                d[Idx(-1, j)] = maxDistance;
                d[Idx(0, j)] = j;
            }

            var lastRow = new Dictionary<T, int>(); // symbol -> last row of a where it occurred
            for (int i = 1; i <= m; i++)
            {
                int lastMatchCol = 0; // db
                T ai = a[i - 1];
                for (int j = 1; j <= n; j++)
                {
                    T bj = b[j - 1];
                    int k = lastRow.TryGetValue(bj, out int kk) ? kk : 0;
                    int l = lastMatchCol;

                    int cost;
                    if (ai.Equals(bj))
                    {
                        cost = 0;
                        lastMatchCol = j;
                    }
                    else
                    {
                        cost = 1;
                    }

                    int substitution = d[Idx(i - 1, j - 1)] + cost;
                    int insertion = d[Idx(i, j - 1)] + 1;
                    int deletion = d[Idx(i - 1, j)] + 1;
                    int transposition = d[Idx(k - 1, l - 1)] + (i - k - 1) + 1 + (j - l - 1);

                    int value = substitution;
                    if (insertion < value)
                    {
                        value = insertion;
                    }
                    if (deletion < value)
                    {
                        value = deletion;
                    }
                    if (transposition < value)
                    {
                        value = transposition;
                    }
                    d[Idx(i, j)] = value;
                }

                lastRow[ai] = i;
            }

            return d[Idx(m, n)];
        }
        finally
        {
            ArrayPool<int>.Shared.Return(rented);
        }
    }

    private static int DistanceCodePoints(ReadOnlySpan<char> a, ReadOnlySpan<char> b, out int lenA, out int lenB)
    {
        int[] bufA = ArrayPool<int>.Shared.Rent(Math.Max(1, a.Length));
        int[] bufB = ArrayPool<int>.Shared.Rent(Math.Max(1, b.Length));
        try
        {
            lenA = CodePoints.Decode(a, bufA);
            lenB = CodePoints.Decode(b, bufB);
            return Distance<int>(bufA.AsSpan(0, lenA), bufB.AsSpan(0, lenB));
        }
        finally
        {
            ArrayPool<int>.Shared.Return(bufA);
            ArrayPool<int>.Shared.Return(bufB);
        }
    }
}
