using System.Buffers;
using DataNet.Text.Internal;

namespace DataNet.Text.Distances;

/// <summary>
/// Ratcliff-Obershelp similarity (Gestalt pattern matching): <c>2·M / T</c> over
/// the longest common substring, recursively paired.
/// </summary>
/// <remarks>
/// Reference behavior: <c>difflib.SequenceMatcher(None, a, b).ratio()</c>;
/// <c>autojunk</c> is not replicated — see
/// <c>docs/decisions/0006-ratcliff-autojunk.md</c>. See <see cref="TextElement"/>
/// for the UTF-16 vs code-point choice.
/// </remarks>
public static class RatcliffObershelp
{
    /// <summary>Computes the Ratcliff-Obershelp similarity of <paramref name="a"/> and <paramref name="b"/>.</summary>
    public static double Similarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
    {
        if (element == TextElement.CodePoint)
        {
            return SimilarityCodePoints(a, b);
        }

        int total = a.Length + b.Length;
        return total == 0 ? 1.0 : 2.0 * MatchLength<char>(a, b) / total;
    }

    /// <summary>Ratcliff-Obershelp distance: <c>1 - Similarity</c>.</summary>
    public static double Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
    {
        return 1.0 - Similarity(a, b, element);
    }

    /// <summary>The total matched length (M) over any sequence of equatable elements.</summary>
    internal static int MatchLength<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b)
        where T : IEquatable<T>
    {
        LongestMatch(a, b, out int i, out int j, out int size);
        if (size == 0)
        {
            return 0;
        }
        return size
            + MatchLength(a[..i], b[..j])
            + MatchLength(a[(i + size)..], b[(j + size)..]);
    }

    /// <summary>
    /// Finds the longest contiguous matching block, breaking ties like difflib:
    /// earliest start in <paramref name="a"/>, then earliest start in <paramref name="b"/>.
    /// </summary>
    private static void LongestMatch<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b, out int bestI, out int bestJ, out int bestSize)
        where T : IEquatable<T>
    {
        bestI = 0;
        bestJ = 0;
        bestSize = 0;
        if (a.Length == 0 || b.Length == 0)
        {
            return;
        }

        int width = b.Length;
        int[] rented = ArrayPool<int>.Shared.Rent(width);
        try
        {
            Span<int> previous = rented.AsSpan(0, width);
            previous.Clear();
            for (int i = 0; i < a.Length; i++)
            {
                T ai = a[i];
                int diagonal = 0; // previous[j-1] value from before this row overwrote it
                for (int j = 0; j < width; j++)
                {
                    int here = previous[j];
                    int run = ai.Equals(b[j]) ? diagonal + 1 : 0;
                    previous[j] = run;
                    // Strictly-greater keeps the earliest end (hence earliest start in a,
                    // then earliest in b) — difflib's tie-break.
                    if (run > bestSize)
                    {
                        bestSize = run;
                        bestI = i - run + 1;
                        bestJ = j - run + 1;
                    }
                    diagonal = here;
                }
            }
        }
        finally
        {
            ArrayPool<int>.Shared.Return(rented);
        }
    }

    private static double SimilarityCodePoints(ReadOnlySpan<char> a, ReadOnlySpan<char> b)
    {
        int[] bufA = ArrayPool<int>.Shared.Rent(Math.Max(1, a.Length));
        int[] bufB = ArrayPool<int>.Shared.Rent(Math.Max(1, b.Length));
        try
        {
            int lenA = CodePoints.Decode(a, bufA);
            int lenB = CodePoints.Decode(b, bufB);
            int total = lenA + lenB;
            return total == 0
                ? 1.0
                : 2.0 * MatchLength<int>(bufA.AsSpan(0, lenA), bufB.AsSpan(0, lenB)) / total;
        }
        finally
        {
            ArrayPool<int>.Shared.Return(bufA);
            ArrayPool<int>.Shared.Return(bufB);
        }
    }
}
