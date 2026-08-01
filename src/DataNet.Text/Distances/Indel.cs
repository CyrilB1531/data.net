using System.Buffers;
using DataNet.Text.Internal;

namespace DataNet.Text.Distances;

/// <summary>
/// Indel distance: the number of insertions and deletions (no substitutions) to
/// transform one sequence into another — equivalently <c>len(a) + len(b) - 2·LCS</c>.
/// </summary>
/// <remarks>
/// <para>
/// This is the basis of <c>rapidfuzz.fuzz.ratio</c>: that ratio is the Indel
/// <see cref="NormalizedSimilarity"/> multiplied by 100. Confusing it with
/// Levenshtein is the single most common error on this topic (see the brief §5).
/// </para>
/// <para>
/// Reference behavior: <c>rapidfuzz.distance.Indel</c>. See <see cref="TextElement"/>
/// for the UTF-16 vs code-point choice. All members are stateless and thread-safe.
/// </para>
/// </remarks>
public static class Indel
{
    /// <summary>Computes the Indel distance between <paramref name="a"/> and <paramref name="b"/>.</summary>
    public static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
    {
        return element == TextElement.CodePoint
            ? DistanceCodePoints(a, b, out _, out _)
            : Distance<char>(a, b);
    }

    /// <summary>Normalized distance in <c>[0, 1]</c>: <c>distance / (len(a) + len(b))</c>, or <c>0</c> if both empty.</summary>
    public static double NormalizedDistance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
    {
        int distance;
        int total;
        if (element == TextElement.CodePoint)
        {
            distance = DistanceCodePoints(a, b, out int lenA, out int lenB);
            total = lenA + lenB;
        }
        else
        {
            distance = Distance<char>(a, b);
            total = a.Length + b.Length;
        }

        return total == 0 ? 0.0 : (double)distance / total;
    }

    /// <summary>Normalized similarity in <c>[0, 1]</c>: <c>1 - NormalizedDistance</c>. This ×100 is <c>fuzz.ratio</c>.</summary>
    public static double NormalizedSimilarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
    {
        return 1.0 - NormalizedDistance(a, b, element);
    }

    /// <summary>Computes the Indel distance over any sequence of equatable elements.</summary>
    public static int Distance<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b)
        where T : IEquatable<T>
    {
        return a.Length + b.Length - 2 * LongestCommonSubsequenceLength(a, b);
    }

    /// <summary>Length of the longest common subsequence, via rolling-row DP.</summary>
    internal static int LongestCommonSubsequenceLength<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b)
        where T : IEquatable<T>
    {
        if (a.Length == 0 || b.Length == 0)
        {
            return 0;
        }

        // Keep the DP row on the shorter operand.
        if (b.Length > a.Length)
        {
            ReadOnlySpan<T> tmp = a;
            a = b;
            b = tmp;
        }

        int width = b.Length + 1;
        int[] rented = ArrayPool<int>.Shared.Rent(width);
        try
        {
            Span<int> row = rented.AsSpan(0, width);
            row.Clear();

            for (int i = 1; i <= a.Length; i++)
            {
                int diagonal = row[0]; // L[i-1][j-1]
                T ai = a[i - 1];
                for (int j = 1; j < width; j++)
                {
                    int above = row[j]; // L[i-1][j]
                    row[j] = ai.Equals(b[j - 1])
                        ? diagonal + 1
                        : Math.Max(above, row[j - 1]);
                    diagonal = above;
                }
            }

            return row[b.Length];
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
