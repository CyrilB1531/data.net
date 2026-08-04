using System.Buffers;
using DataNet.Text.Internal;

namespace DataNet.Text.Distances;

// SonarLint S4136: the overloads are grouped by concern, with the generic core deliberately last.
#pragma warning disable S4136

/// <summary>
/// The Levenshtein edit distance and its normalized forms.
/// </summary>
/// <remarks>
/// <para>
/// The distance is the minimum number of single-element insertions, deletions
/// and substitutions (each of unit cost) required to transform one sequence into
/// another. It is a true metric.
/// </para>
/// <para>
/// Reference behavior: <c>rapidfuzz.distance.Levenshtein</c> with default weights
/// <c>(1, 1, 1)</c>. rapidfuzz operates on Python <c>str</c>, i.e. code points, so
/// pass <see cref="TextElement.CodePoint"/> for exact parity on
/// supplementary-plane input; the default <see cref="TextElement.Utf16Unit"/>
/// agrees for all Basic-Multilingual-Plane text and avoids a decode pass.
/// </para>
/// <para>
/// All members are stateless and thread-safe.
/// </para>
/// </remarks>
public static class Levenshtein
{
    // Below this pattern length the DP beats Myers: the equality-table setup
    // dominates. There is no upper bound any more — patterns longer than one
    // machine word take the blocked variant.
    private const int MyersMinPatternLength = 16;

    /// <summary>
    /// Computes the Levenshtein distance between <paramref name="a"/> and
    /// <paramref name="b"/>.
    /// </summary>
    /// <param name="a">First operand.</param>
    /// <param name="b">Second operand.</param>
    /// <param name="element">The unit of comparison. See <see cref="TextElement"/>.</param>
    /// <returns>The edit distance, a non-negative integer.</returns>
    public static int Distance(
        ReadOnlySpan<char> a,
        ReadOnlySpan<char> b,
        TextElement element = TextElement.Utf16Unit)
    {
        return element == TextElement.CodePoint
            ? DistanceCodePoints(a, b, out _, out _)
            : DistanceChars(a, b);
    }

    /// <summary>
    /// Computes the length-normalized distance in <c>[0, 1]</c>:
    /// <c>distance / max(len(a), len(b))</c>, or <c>0</c> when both are empty.
    /// </summary>
    /// <remarks>Matches <c>Levenshtein.normalized_distance</c> in rapidfuzz.</remarks>
    public static double NormalizedDistance(
        ReadOnlySpan<char> a,
        ReadOnlySpan<char> b,
        TextElement element = TextElement.Utf16Unit)
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
            distance = DistanceChars(a, b);
            maxLen = Math.Max(a.Length, b.Length);
        }

        return maxLen == 0 ? 0.0 : (double)distance / maxLen;
    }

    /// <summary>
    /// Computes the length-normalized similarity in <c>[0, 1]</c>:
    /// <c>1 - NormalizedDistance</c>. Two empty inputs are perfectly similar (<c>1</c>).
    /// </summary>
    /// <remarks>Matches <c>Levenshtein.normalized_similarity</c> in rapidfuzz.</remarks>
    public static double NormalizedSimilarity(
        ReadOnlySpan<char> a,
        ReadOnlySpan<char> b,
        TextElement element = TextElement.Utf16Unit)
    {
        return 1.0 - NormalizedDistance(a, b, element);
    }

    /// <summary>
    /// Computes the Levenshtein distance over any sequence of equatable elements.
    /// This is the allocation-lean core used by the character overloads.
    /// </summary>
    /// <typeparam name="T">The element type (e.g. <see cref="char"/> or code-point <see cref="int"/>).</typeparam>
    public static int Distance<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b)
        where T : IEquatable<T>
    {
        Trim(ref a, ref b);
        if (a.Length == 0)
        {
            return b.Length;
        }
        if (b.Length == 0)
        {
            return a.Length;
        }
        // Keep the shorter operand as the DP row: O(min(n, m)) memory.
        if (b.Length > a.Length)
        {
            ReadOnlySpan<T> tmp = a;
            a = b;
            b = tmp;
        }
        return Dp(a, b);
    }

    /// <summary>
    /// Character fast path: identical result to <see cref="Distance{T}"/> over
    /// <see cref="char"/>, but takes the bit-parallel Myers route when it applies.
    /// </summary>
    private static int DistanceChars(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        Trim(ref a, ref b);
        if (a.Length == 0)
        {
            return b.Length;
        }
        if (b.Length == 0)
        {
            return a.Length;
        }
        if (b.Length > a.Length)
        {
            ReadOnlySpan<char> tmp = a;
            a = b;
            b = tmp;
        }

        // b is the shorter operand (the Myers "pattern"); a is the text. Myers
        // wins from a modest pattern length up: below it, building the equality
        // table costs more than the tiny DP, so we stay on the DP. (Measured
        // crossover ~16; see docs/guides/performance.md.)
        if (b.Length >= MyersMinPatternLength && Myers.TryDistance(b, a, out int d))
        {
            return d;
        }
        return Dp(a, b);
    }

    /// <summary>Strips the common prefix and suffix in place (result-preserving).</summary>
    /// <remarks>
    /// Collapses the DP band on near-equal inputs — the common case in record
    /// matching — to near nothing.
    /// </remarks>
    private static void Trim<T>(ref ReadOnlySpan<T> a, ref ReadOnlySpan<T> b)
        where T : IEquatable<T>
    {
        int start = 0;
        int endA = a.Length;
        int endB = b.Length;
        while (start < endA && start < endB && a[start].Equals(b[start]))
        {
            start++;
        }
        while (endA > start && endB > start && a[endA - 1].Equals(b[endB - 1]))
        {
            endA--;
            endB--;
        }

        a = a[start..endA];
        b = b[start..endB];
    }

    /// <summary>Rolling-row DP over trimmed operands where <paramref name="b"/> is the shorter.</summary>
    private static int Dp<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b)
        where T : IEquatable<T>
    {
        int width = b.Length + 1;
        int[] rented = ArrayPool<int>.Shared.Rent(width);
        try
        {
            Span<int> row = rented.AsSpan(0, width);
            for (int j = 0; j < width; j++)
            {
                row[j] = j;
            }

            for (int i = 1; i <= a.Length; i++)
            {
                int diagonal = row[0]; // D[i-1][j-1]
                row[0] = i;            // D[i][0]
                T ai = a[i - 1];

                for (int j = 1; j < width; j++)
                {
                    int above = row[j];                     // D[i-1][j]
                    int cost = ai.Equals(b[j - 1]) ? 0 : 1;
                    int value = diagonal + cost;            // substitution / match
                    int deletion = above + 1;
                    int insertion = row[j - 1] + 1;         // D[i][j-1], already updated
                    if (deletion < value)
                    {
                        value = deletion;
                    }
                    if (insertion < value)
                    {
                        value = insertion;
                    }

                    row[j] = value;
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

    /// <summary>
    /// Decodes both operands to code points into pooled buffers and computes the
    /// distance, also reporting the code-point lengths for normalization.
    /// </summary>
    private static int DistanceCodePoints(
        ReadOnlySpan<char> a,
        ReadOnlySpan<char> b,
        out int lenA,
        out int lenB)
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
