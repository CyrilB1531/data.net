using System.Buffers;
using Lodestar.Text.Internal;

namespace Lodestar.Text.Distances;

// SonarLint S4136: the overloads are grouped by concern, with the generic core deliberately last.
#pragma warning disable S4136

/// <summary>
/// The Levenshtein edit distance and its normalized forms: the minimum number
/// of single-element insertions, deletions and substitutions (unit cost) to
/// transform one sequence into another. A true metric.
/// </summary>
/// <remarks>
/// Reference behavior: <c>rapidfuzz.distance.Levenshtein</c>, weights
/// <c>(1, 1, 1)</c>; see <c>docs/equivalence.md</c> for the UTF-16 vs
/// code-point choice. All members are stateless and thread-safe.
/// </remarks>
public static class Levenshtein
{
    // Below this length the DP beats Myers (equality-table setup dominates); no
    // upper bound remains since longer patterns take the blocked path.
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

        // b is the shorter operand (Myers' "pattern"); below MyersMinPatternLength the
        // DP wins because building the equality table costs more than it saves.
        if (b.Length >= MyersMinPatternLength && Myers.TryDistance(b, a, out int d))
        {
            return d;
        }
        return Dp(a, b);
    }

    /// <summary>Code-point mirror of <see cref="DistanceChars"/>: the Myers route when it applies.</summary>
    /// <remarks>
    /// <c>decisions/0002</c> sends a caller wanting Python's answer on
    /// supplementary characters here, so DP-only meant recommending the slow path
    /// to whoever needed the correct one (#208). <see cref="Distance{T}"/> over an
    /// arbitrary <c>int</c> sequence still takes the DP; the fast path is reached
    /// through the <see cref="TextElement"/> overloads, which document the mode.
    /// </remarks>
    private static int DistanceCodePoints(ReadOnlySpan<int> a, ReadOnlySpan<int> b)
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
            ReadOnlySpan<int> tmp = a;
            a = b;
            b = tmp;
        }

        if (b.Length >= MyersMinPatternLength && Myers.TryDistance(b, a, out int d))
        {
            return d;
        }
        return Dp(a, b);
    }

    /// <summary>Strips the common prefix and suffix in place (result-preserving).</summary>
    /// <remarks>
    /// Collapses the DP band on near-equal inputs — the common case in record
    /// matching — to near nothing. The count <see cref="Affixes.Trim{T}"/>
    /// returns is what a subsequence length adds back and an edit distance does
    /// not, so this discards it.
    /// </remarks>
    private static void Trim<T>(ref ReadOnlySpan<T> a, ref ReadOnlySpan<T> b)
        where T : IEquatable<T> => Affixes.Trim(ref a, ref b);

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
            return DistanceCodePoints(bufA.AsSpan(0, lenA), bufB.AsSpan(0, lenB));
        }
        finally
        {
            ArrayPool<int>.Shared.Return(bufA);
            ArrayPool<int>.Shared.Return(bufB);
        }
    }
}
