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
    // Cached so the code-point path allocates no delegate per call.
    private static readonly CodePointPair.Measure MeasureCodePoints = Distance<int>;

    /// <summary>Computes the Indel distance between <paramref name="a"/> and <paramref name="b"/>.</summary>
    public static int Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
    {
        return element == TextElement.CodePoint
            ? DistanceCodePoints(a, b, out _, out _)
            : DistanceChars(a, b);
    }

    /// <summary>The character path, which reaches the bit-parallel LCS kernel (#273).</summary>
    /// <remarks>
    /// <see cref="Distance{T}"/> stays on the dynamic program: a generic sequence has no
    /// dense alphabet to build an equality table over. Routing the character overload here
    /// rather than through it is what puts <c>fuzz.ratio</c> on the fast path.
    /// </remarks>
    private static int DistanceChars(ReadOnlySpan<char> a, ReadOnlySpan<char> b) =>
        a.Length + b.Length - (2 * Lcs.SubsequenceLengthChars(a, b));

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
            distance = DistanceChars(a, b);
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

    private static int DistanceCodePoints(ReadOnlySpan<char> a, ReadOnlySpan<char> b, out int lenA, out int lenB) =>
        CodePointPair.Distance(a, b, MeasureCodePoints, out lenA, out lenB);
}
