using System.Buffers;
using Lodestar.Text.Internal;

namespace Lodestar.Text.Distances;

// SonarLint S3776: cognitive complexity: a faithful implementation of a published rule-engine; decomposing it would break the 1:1 mapping with the reference that makes divergences auditable.
// SonarLint S4136: the overloads are grouped by concern, with the generic core deliberately last.
#pragma warning disable S3776, S4136

/// <summary>
/// Optimal String Alignment distance (restricted Damerau-Levenshtein): adjacent
/// transpositions allowed, but no substring edited more than once. Not a metric.
/// </summary>
/// <remarks>
/// Reference behavior: <c>rapidfuzz.distance.OSA</c>; see
/// <c>docs/equivalence.md</c> for the divergence from
/// <see cref="DamerauLevenshtein"/>. See <see cref="TextElement"/> for the
/// UTF-16 vs code-point choice. All members are stateless and thread-safe.
/// </remarks>
public static class Osa
{
    // Cached so the code-point path allocates no delegate per call.
    private static readonly CodePointPair.Measure MeasureCodePoints = Distance<int>;

    /// <summary>Computes the OSA distance between <paramref name="a"/> and <paramref name="b"/>.</summary>
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

    /// <summary>Computes the OSA distance over any sequence of equatable elements.</summary>
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

        // Three rolling rows of width n+1: the transposition term needs D[i-2][j-2].
        int width = n + 1;
        int[] rented = ArrayPool<int>.Shared.Rent(width * 3);
        try
        {
            Span<int> buffer = rented.AsSpan(0, width * 3);
            Span<int> prev2 = buffer.Slice(0, width);
            Span<int> prev1 = buffer.Slice(width, width);
            Span<int> current = buffer.Slice(width * 2, width);

            for (int j = 0; j <= n; j++)
            {
                prev1[j] = j; // D[0][j]
            }

            for (int i = 1; i <= m; i++)
            {
                current[0] = i;
                T ai = a[i - 1];
                for (int j = 1; j <= n; j++)
                {
                    int cost = ai.Equals(b[j - 1]) ? 0 : 1;
                    int value = prev1[j - 1] + cost;      // substitution / match
                    int deletion = prev1[j] + 1;
                    int insertion = current[j - 1] + 1;
                    if (deletion < value)
                    {
                        value = deletion;
                    }
                    if (insertion < value)
                    {
                        value = insertion;
                    }
                    if (i > 1 && j > 1
                        && ai.Equals(b[j - 2])
                        && a[i - 2].Equals(b[j - 1]))
                    {
                        int transposition = prev2[j - 2] + 1;
                        if (transposition < value)
                        {
                            value = transposition;
                        }
                    }
                    current[j] = value;
                }

                // Rotate rows: prev2 <- prev1 <- current <- (reuse old prev2).
                Span<int> recycled = prev2;
                prev2 = prev1;
                prev1 = current;
                current = recycled;
            }

            return prev1[n];
        }
        finally
        {
            ArrayPool<int>.Shared.Return(rented);
        }
    }

    private static int DistanceCodePoints(ReadOnlySpan<char> a, ReadOnlySpan<char> b, out int lenA, out int lenB) =>
        CodePointPair.Distance(a, b, MeasureCodePoints, out lenA, out lenB);
}
