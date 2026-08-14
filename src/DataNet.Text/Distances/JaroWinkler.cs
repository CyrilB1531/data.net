using System.Buffers;
using DataNet.Text.Internal;

namespace DataNet.Text.Distances;

/// <summary>
/// Jaro-Winkler similarity: <see cref="Jaro"/> boosted for a shared prefix.
/// </summary>
/// <remarks>
/// Reference behavior: <c>jellyfish.jaro_winkler_similarity</c>; see
/// <c>docs/equivalence.md</c> for the threshold, weight and cap. Long-tolerance
/// is not applied (jellyfish's default). jellyfish operates on code points —
/// pass <see cref="TextElement.CodePoint"/> for supplementary-plane parity. All
/// members are stateless and thread-safe.
/// </remarks>
public static class JaroWinkler
{
    /// <summary>Default prefix weight <c>p</c> (jellyfish default).</summary>
    public const double DefaultPrefixWeight = 0.1;

    private const int MaxPrefix = 4;

    // The prefix boost applies only above this base-Jaro threshold (jellyfish uses 0.7).
    private const double BoostThreshold = 0.7;

    /// <summary>Computes the Jaro-Winkler similarity of <paramref name="a"/> and <paramref name="b"/>.</summary>
    public static double Similarity(
        ReadOnlySpan<char> a,
        ReadOnlySpan<char> b,
        double prefixWeight = DefaultPrefixWeight,
        TextElement element = TextElement.Utf16Unit)
    {
        if (element != TextElement.CodePoint)
        {
            return WinklerCore<char>(a, b, prefixWeight);
        }

        int[] bufA = ArrayPool<int>.Shared.Rent(Math.Max(1, a.Length));
        int[] bufB = ArrayPool<int>.Shared.Rent(Math.Max(1, b.Length));
        try
        {
            int lenA = CodePoints.Decode(a, bufA);
            int lenB = CodePoints.Decode(b, bufB);
            return WinklerCore<int>(bufA.AsSpan(0, lenA), bufB.AsSpan(0, lenB), prefixWeight);
        }
        finally
        {
            ArrayPool<int>.Shared.Return(bufA);
            ArrayPool<int>.Shared.Return(bufB);
        }
    }

    /// <summary>Jaro-Winkler distance: <c>1 - Similarity</c>.</summary>
    public static double Distance(
        ReadOnlySpan<char> a,
        ReadOnlySpan<char> b,
        double prefixWeight = DefaultPrefixWeight,
        TextElement element = TextElement.Utf16Unit)
    {
        return 1.0 - Similarity(a, b, prefixWeight, element);
    }

    private static double WinklerCore<T>(ReadOnlySpan<T> s1, ReadOnlySpan<T> s2, double prefixWeight)
        where T : IEquatable<T>
    {
        double jaro = Jaro.SimilarityCore(s1, s2);
        if (jaro <= BoostThreshold)
        {
            return jaro;
        }

        int limit = Math.Min(Math.Min(s1.Length, s2.Length), MaxPrefix);
        int prefix = 0;
        while (prefix < limit && s1[prefix].Equals(s2[prefix]))
        {
            prefix++;
        }

        return jaro + prefix * prefixWeight * (1.0 - jaro);
    }
}
