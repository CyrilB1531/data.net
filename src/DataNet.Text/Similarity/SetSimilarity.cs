using System.Buffers;
using System.Text;
using DataNet.Text.Internal;

namespace DataNet.Text.Similarity;

/// <summary>
/// Shared q-gram multiset machinery for the token-set similarity measures.
/// </summary>
/// <remarks>
/// Matches <c>textdistance</c> semantics: q-grams are counted as a <em>multiset</em>
/// (a bag), so repeated grams count with multiplicity. The default <c>qval = 1</c>
/// makes the grams individual characters / code points.
/// </remarks>
internal static class QgramCounts
{
    /// <summary>Computes the multiset intersection size and each side's total gram count.</summary>
    public static (int Intersection, int SizeA, int SizeB) Compute(
        ReadOnlySpan<char> a, ReadOnlySpan<char> b, int qval, TextElement element)
    {
        if (qval < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(qval), qval, "qval must be >= 1.");
        }

        Dictionary<string, int> ca = Build(a, qval, element);
        Dictionary<string, int> cb = Build(b, qval, element);

        int sizeA = 0;
        foreach (int v in ca.Values)
        {
            sizeA += v;
        }
        int sizeB = 0;
        foreach (int v in cb.Values)
        {
            sizeB += v;
        }

        int intersection = 0;
        // Iterate the smaller dictionary for the shared grams.
        Dictionary<string, int> small = ca.Count <= cb.Count ? ca : cb;
        Dictionary<string, int> large = ReferenceEquals(small, ca) ? cb : ca;
        foreach (KeyValuePair<string, int> kv in small)
        {
            if (large.TryGetValue(kv.Key, out int other))
            {
                intersection += Math.Min(kv.Value, other);
            }
        }

        return (intersection, sizeA, sizeB);
    }

    private static Dictionary<string, int> Build(ReadOnlySpan<char> s, int qval, TextElement element)
    {
        var counts = new Dictionary<string, int>();

        if (element == TextElement.CodePoint)
        {
            int[] buf = ArrayPool<int>.Shared.Rent(Math.Max(1, s.Length));
            try
            {
                int n = CodePoints.Decode(s, buf);
                for (int i = 0; i + qval <= n; i++)
                {
                    var sb = new StringBuilder(qval * 2);
                    for (int k = 0; k < qval; k++)
                    {
                        sb.Append(char.ConvertFromUtf32(buf[i + k]));
                    }
                    Add(counts, sb.ToString());
                }
            }
            finally
            {
                ArrayPool<int>.Shared.Return(buf);
            }
        }
        else
        {
            for (int i = 0; i + qval <= s.Length; i++)
            {
                Add(counts, new string(s.Slice(i, qval)));
            }
        }

        return counts;
    }

    private static void Add(Dictionary<string, int> counts, string gram)
    {
        counts[gram] = counts.TryGetValue(gram, out int c) ? c + 1 : 1;
    }
}

/// <summary>Jaccard similarity on character q-gram multisets: <c>|A∩B| / |A∪B|</c>.</summary>
/// <remarks>Reference: <c>textdistance.Jaccard</c> (default <c>qval = 1</c>). Two empty inputs give 1.</remarks>
public static class Jaccard
{
    /// <summary>Computes the Jaccard similarity of <paramref name="a"/> and <paramref name="b"/>.</summary>
    public static double Similarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, int qval = 1, TextElement element = TextElement.Utf16Unit)
    {
        (int inter, int sizeA, int sizeB) = QgramCounts.Compute(a, b, qval, element);
        int union = sizeA + sizeB - inter;
        return union == 0 ? 1.0 : (double)inter / union;
    }
}

/// <summary>Sørensen-Dice similarity: <c>2·|A∩B| / (|A| + |B|)</c>.</summary>
/// <remarks>Reference: <c>textdistance.Sorensen</c> (default <c>qval = 1</c>). Two empty inputs give 1.</remarks>
public static class SorensenDice
{
    /// <summary>Computes the Sørensen-Dice similarity of <paramref name="a"/> and <paramref name="b"/>.</summary>
    public static double Similarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, int qval = 1, TextElement element = TextElement.Utf16Unit)
    {
        (int inter, int sizeA, int sizeB) = QgramCounts.Compute(a, b, qval, element);
        int denom = sizeA + sizeB;
        return denom == 0 ? 1.0 : 2.0 * inter / denom;
    }
}

/// <summary>Overlap (Szymkiewicz-Simpson) coefficient: <c>|A∩B| / min(|A|, |B|)</c>.</summary>
/// <remarks>Reference: <c>textdistance.Overlap</c> (default <c>qval = 1</c>). Two empty inputs give 1; one empty gives 0.</remarks>
public static class Overlap
{
    /// <summary>Computes the overlap coefficient of <paramref name="a"/> and <paramref name="b"/>.</summary>
    public static double Similarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, int qval = 1, TextElement element = TextElement.Utf16Unit)
    {
        (int inter, int sizeA, int sizeB) = QgramCounts.Compute(a, b, qval, element);
        int denom = Math.Min(sizeA, sizeB);
        if (denom == 0)
        {
            return sizeA == 0 && sizeB == 0 ? 1.0 : 0.0;
        }
        return (double)inter / denom;
    }
}

/// <summary>Tversky index: <c>|A∩B| / (|A∩B| + α·|A\B| + β·|B\A|)</c>.</summary>
/// <remarks>Reference: <c>textdistance.Tversky</c> (default <c>qval = 1</c>, <c>α = β = 1</c>, which equals Jaccard).</remarks>
public static class Tversky
{
    /// <summary>Computes the Tversky index of <paramref name="a"/> and <paramref name="b"/>.</summary>
    public static double Similarity(
        ReadOnlySpan<char> a,
        ReadOnlySpan<char> b,
        double alpha = 1.0,
        double beta = 1.0,
        int qval = 1,
        TextElement element = TextElement.Utf16Unit)
    {
        (int inter, int sizeA, int sizeB) = QgramCounts.Compute(a, b, qval, element);
        double denom = inter + alpha * (sizeA - inter) + beta * (sizeB - inter);
        return denom == 0.0 ? 1.0 : inter / denom;
    }
}

/// <summary>Cosine (Ochiai) similarity on q-gram multisets: <c>|A∩B| / √(|A|·|B|)</c>.</summary>
/// <remarks>Reference: <c>textdistance.Cosine</c> (default <c>qval = 1</c>). Two empty inputs give 1; one empty gives 0.</remarks>
public static class Cosine
{
    /// <summary>Computes the cosine similarity of <paramref name="a"/> and <paramref name="b"/> over q-gram multisets.</summary>
    public static double Similarity(ReadOnlySpan<char> a, ReadOnlySpan<char> b, int qval = 1, TextElement element = TextElement.Utf16Unit)
    {
        (int inter, int sizeA, int sizeB) = QgramCounts.Compute(a, b, qval, element);
        if (sizeA == 0 || sizeB == 0)
        {
            return sizeA == 0 && sizeB == 0 ? 1.0 : 0.0;
        }
        return inter / Math.Sqrt((double)sizeA * sizeB);
    }
}
