using System.Buffers;
using Lodestar.Text.Internal;

namespace Lodestar.Text.Distances;

/// <summary>
/// Decoding both operands to code points once, for the distances measured over them.
/// </summary>
/// <remarks>
/// Four distances need the same fifteen lines — rent, decode, measure, return — and
/// wrote them four times, which is what Sonar flagged when Indel became the fourth
/// (#273). The measurement itself is passed in, cached as a static delegate by each
/// caller so the shared path allocates nothing the copies did not.
/// </remarks>
internal static class CodePointPair
{
    /// <summary>A distance over two decoded code-point sequences.</summary>
    internal delegate int Measure(ReadOnlySpan<int> a, ReadOnlySpan<int> b);

    /// <summary>Decodes <paramref name="a"/> and <paramref name="b"/> into pooled buffers, then measures across them.</summary>
    internal static int Distance(
        ReadOnlySpan<char> a, ReadOnlySpan<char> b, Measure measure, out int lenA, out int lenB)
    {
        int[] bufA = ArrayPool<int>.Shared.Rent(Math.Max(1, a.Length));
        int[] bufB = ArrayPool<int>.Shared.Rent(Math.Max(1, b.Length));
        try
        {
            lenA = CodePoints.Decode(a, bufA);
            lenB = CodePoints.Decode(b, bufB);
            return measure(bufA.AsSpan(0, lenA), bufB.AsSpan(0, lenB));
        }
        finally
        {
            ArrayPool<int>.Shared.Return(bufA);
            ArrayPool<int>.Shared.Return(bufB);
        }
    }
}
