using System.Buffers;
using Lodestar.Text.Internal;

namespace Lodestar.Text.Distances;

// SonarLint S4136: the overloads are grouped by concern, with the generic core deliberately last.
#pragma warning disable S4136

/// <summary>
/// Hamming distance: the number of positions at which two sequences differ,
/// plus the absolute length difference when they are unequal.
/// </summary>
/// <remarks>
/// Matches <c>jellyfish.hamming_distance</c> except on combining-mark input; see
/// <c>docs/decisions/0005-hamming-jellyfish-divergence.md</c>. Pass
/// <see cref="TextElement.CodePoint"/> for code-point semantics on
/// supplementary-plane input. All members are stateless and thread-safe.
/// </remarks>
public static class Hamming
{
    /// <summary>Computes the Hamming distance between <paramref name="a"/> and <paramref name="b"/>.</summary>
    public static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
    {
        return element == TextElement.CodePoint
            ? DistanceCodePoints(a, b, out _, out _)
            : Distance<char>(a, b);
    }

    /// <summary>
    /// Length-normalized similarity in <c>[0, 1]</c>: <c>1 - distance / max(len(a), len(b))</c>.
    /// Two empty inputs are perfectly similar (<c>1</c>). This is a DataNet convenience;
    /// jellyfish itself exposes only the integer distance.
    /// </summary>
    public static double NormalizedSimilarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
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

        return maxLen == 0 ? 1.0 : 1.0 - (double)distance / maxLen;
    }

    /// <summary>Computes the Hamming distance over any sequence of equatable elements.</summary>
    public static int Distance<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b)
        where T : IEquatable<T>
    {
        int min = Math.Min(a.Length, b.Length);
        int distance = Math.Abs(a.Length - b.Length);
        for (int i = 0; i < min; i++)
        {
            if (!a[i].Equals(b[i]))
            {
                distance++;
            }
        }
        return distance;
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
