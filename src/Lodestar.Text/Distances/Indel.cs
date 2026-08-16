using System.Buffers;
using Lodestar.Text.Internal;

namespace Lodestar.Text.Distances;

// SonarLint S4136: the overloads are grouped by concern, with the generic core deliberately last.
#pragma warning disable S4136

/// <summary>
/// Indel distance: the number of insertions and deletions (no substitutions) to
/// transform one sequence into another — equivalently <c>len(a) + len(b) - 2·LCS</c>.
/// </summary>
/// <remarks>
/// Reference behavior: <c>rapidfuzz.distance.Indel</c>; <see cref="NormalizedSimilarity"/>
/// ×100 is <c>rapidfuzz.fuzz.ratio</c> — not Levenshtein, the most common
/// confusion on this topic (see the brief §5). See <see cref="TextElement"/> for
/// the UTF-16 vs code-point choice. All members are stateless and thread-safe.
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
        return a.Length + b.Length - 2 * Lcs.SubsequenceLength(a, b);
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
