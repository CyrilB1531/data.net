namespace Lodestar.Text.Internal;

/// <summary>Common prefix and suffix removal, shared by the distances that may drop them.</summary>
internal static class Affixes
{
    /// <summary>
    /// Strips the longest common prefix and suffix from both operands, and returns how many
    /// elements went.
    /// </summary>
    /// <remarks>
    /// Edit distance discards the count — a shared affix costs no edit — while the longest
    /// common subsequence adds it back, since every stripped element is in the subsequence.
    /// Both are safe because equal ends can always be matched to each other: an optimal
    /// alignment that does not already do so can be rewritten to, without getting worse.
    /// It is <b>not</b> safe for the longest common <i>substring</i>, which is why
    /// <c>Lcs.SubstringLength</c> does not call this — trimming <c>"abc"</c> against
    /// <c>"abd"</c> would report 0 where the answer is 2.
    /// </remarks>
    public static int Trim<T>(ref ReadOnlySpan<T> a, ref ReadOnlySpan<T> b)
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

        int trimmed = start + (a.Length - endA);
        a = a[start..endA];
        b = b[start..endB];
        return trimmed;
    }
}
