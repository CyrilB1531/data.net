using System.Buffers;
using DataNet.Text.Internal;

namespace DataNet.Text.Distances;

// SonarLint S3776: cognitive complexity: a faithful implementation of a published rule-engine; decomposing it would break the 1:1 mapping with the reference that makes divergences auditable.
#pragma warning disable S3776

/// <summary>
/// Jaro similarity: a value in <c>[0, 1]</c> based on matching characters within a
/// sliding window and the number of transpositions among them.
/// </summary>
/// <remarks>
/// Reference behavior: <c>jellyfish.jaro_similarity</c>, empty ⇒ <c>0</c>; see
/// <c>docs/decisions/0005-hamming-jellyfish-divergence.md</c> for the divergence.
/// Pass <see cref="TextElement.CodePoint"/> for supplementary-plane parity
/// (jellyfish operates on code points). All members are stateless and thread-safe.
/// </remarks>
public static class Jaro
{
    /// <summary>Computes the Jaro similarity of <paramref name="a"/> and <paramref name="b"/>.</summary>
    public static double Similarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
    {
        if (element != TextElement.CodePoint)
        {
            return SimilarityCore<char>(a, b);
        }

        int[] bufA = ArrayPool<int>.Shared.Rent(Math.Max(1, a.Length));
        int[] bufB = ArrayPool<int>.Shared.Rent(Math.Max(1, b.Length));
        try
        {
            int lenA = CodePoints.Decode(a, bufA);
            int lenB = CodePoints.Decode(b, bufB);
            return SimilarityCore<int>(bufA.AsSpan(0, lenA), bufB.AsSpan(0, lenB));
        }
        finally
        {
            ArrayPool<int>.Shared.Return(bufA);
            ArrayPool<int>.Shared.Return(bufB);
        }
    }

    /// <summary>Jaro distance: <c>1 - Similarity</c>.</summary>
    public static double Distance(ReadOnlySpan<char> a, ReadOnlySpan<char> b, TextElement element = TextElement.Utf16Unit)
    {
        return 1.0 - Similarity(a, b, element);
    }

    /// <summary>The shared Jaro core over any sequence of equatable elements.</summary>
    internal static double SimilarityCore<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2)
        where T : IEquatable<T>
    {
        int len1 = s1.Length;
        int len2 = s2.Length;
        if (len1 == 0 || len2 == 0)
        {
            return 0.0;
        }

        int window = Math.Max(0, Math.Max(len1, len2) / 2 - 1);

        bool[] matched1 = ArrayPool<bool>.Shared.Rent(len1);
        bool[] matched2 = ArrayPool<bool>.Shared.Rent(len2);
        try
        {
            Array.Clear(matched1, 0, len1);
            Array.Clear(matched2, 0, len2);

            int matches = 0;
            for (int i = 0; i < len1; i++)
            {
                int lo = Math.Max(0, i - window);
                int hi = Math.Min(i + window + 1, len2);
                for (int j = lo; j < hi; j++)
                {
                    if (!matched2[j] && s1[i].Equals(s2[j]))
                    {
                        matched1[i] = true;
                        matched2[j] = true;
                        matches++;
                        break;
                    }
                }
            }

            if (matches == 0)
            {
                return 0.0;
            }

            int transpositions = 0;
            int k = 0;
            for (int i = 0; i < len1; i++)
            {
                if (!matched1[i])
                {
                    continue;
                }
                while (!matched2[k])
                {
                    k++;
                }
                if (!s1[i].Equals(s2[k]))
                {
                    transpositions++;
                }
                k++;
            }

            // Matched-character mismatches always come in pairs (published Jaro
            // property), so halving with integer division is exact, matching jellyfish and rapidfuzz.
            int half = transpositions / 2;
            double m = matches;
            return (m / len1 + m / len2 + (m - half) / m) / 3.0;
        }
        finally
        {
            ArrayPool<bool>.Shared.Return(matched1);
            ArrayPool<bool>.Shared.Return(matched2);
        }
    }
}
