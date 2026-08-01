using System.Buffers;
using DataNet.Text.Internal;

namespace DataNet.Text.Distances;

/// <summary>
/// Longest common subsequence and longest common substring lengths.
/// </summary>
/// <remarks>
/// <para>
/// A <em>subsequence</em> keeps relative order but need not be contiguous
/// (<c>"ace"</c> in <c>"abcde"</c>); a <em>substring</em> is contiguous
/// (<c>"Dupon"</c> in <c>"Dupont"</c>). The substring length matches the size of
/// <c>difflib.SequenceMatcher.find_longest_match</c>; the subsequence length is the
/// classic LCS that also underlies <see cref="Indel"/>.
/// </para>
/// <para>See <see cref="TextElement"/> for the UTF-16 vs code-point choice. All members are thread-safe.</para>
/// </remarks>
public static class Lcs
{
    /// <summary>Length of the longest common subsequence (order-preserving, not necessarily contiguous).</summary>
    public static int SubsequenceLength(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
    {
        return element == TextElement.CodePoint
            ? OverCodePoints(a, b, static (x, y) => SubsequenceLength<int>(x, y))
            : SubsequenceLength<char>(a, b);
    }

    /// <summary>Length of the longest common substring (contiguous).</summary>
    public static int SubstringLength(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
    {
        return element == TextElement.CodePoint
            ? OverCodePoints(a, b, static (x, y) => SubstringLength<int>(x, y))
            : SubstringLength<char>(a, b);
    }

    /// <summary>Longest common subsequence length over any sequence of equatable elements.</summary>
    public static int SubsequenceLength<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b)
        where T : IEquatable<T>
    {
        if (a.Length == 0 || b.Length == 0)
        {
            return 0;
        }
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
                int diagonal = row[0];
                T ai = a[i - 1];
                for (int j = 1; j < width; j++)
                {
                    int above = row[j];
                    row[j] = ai.Equals(b[j - 1]) ? diagonal + 1 : Math.Max(above, row[j - 1]);
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

    /// <summary>Longest common substring length over any sequence of equatable elements.</summary>
    public static int SubstringLength<T>(ReadOnlySpan<T> a, ReadOnlySpan<T> b)
        where T : IEquatable<T>
    {
        if (a.Length == 0 || b.Length == 0)
        {
            return 0;
        }
        if (b.Length > a.Length)
        {
            ReadOnlySpan<T> tmp = a;
            a = b;
            b = tmp;
        }

        int width = b.Length;
        int[] rented = ArrayPool<int>.Shared.Rent(width);
        try
        {
            Span<int> previous = rented.AsSpan(0, width);
            previous.Clear();
            int best = 0;
            for (int i = 0; i < a.Length; i++)
            {
                T ai = a[i];
                int diagonal = 0; // previous[j-1] before overwrite
                for (int j = 0; j < width; j++)
                {
                    int here = previous[j];
                    int run = ai.Equals(b[j]) ? diagonal + 1 : 0;
                    previous[j] = run;
                    if (run > best)
                    {
                        best = run;
                    }
                    diagonal = here;
                }
            }
            return best;
        }
        finally
        {
            ArrayPool<int>.Shared.Return(rented);
        }
    }

    private delegate int CodePointOp(ReadOnlySpan<int> a, ReadOnlySpan<int> b);

    private static int OverCodePoints(ReadOnlySpan<char> a, ReadOnlySpan<char> b, CodePointOp op)
    {
        int[] bufA = ArrayPool<int>.Shared.Rent(Math.Max(1, a.Length));
        int[] bufB = ArrayPool<int>.Shared.Rent(Math.Max(1, b.Length));
        try
        {
            int lenA = CodePoints.Decode(a, bufA);
            int lenB = CodePoints.Decode(b, bufB);
            return op(bufA.AsSpan(0, lenA), bufB.AsSpan(0, lenB));
        }
        finally
        {
            ArrayPool<int>.Shared.Return(bufA);
            ArrayPool<int>.Shared.Return(bufB);
        }
    }
}
