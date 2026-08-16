namespace Lodestar.Text.Internal;

/// <summary>
/// Helpers for decoding UTF-16 text into Unicode code points (scalar values).
/// </summary>
internal static class CodePoints
{
    /// <summary>
    /// Decodes <paramref name="source"/> into Unicode scalar values written to
    /// <paramref name="destination"/> and returns the number of code points.
    /// </summary>
    /// <remarks>
    /// A well-formed surrogate pair becomes a single scalar value; a lone
    /// surrogate is passed through as its own code-unit value, matching how a
    /// Python <c>str</c> preserves unpaired surrogates rather than throwing.
    /// <paramref name="destination"/> must be at least
    /// <c>source.Length</c> elements long (one code point can never expand).
    /// </remarks>
    public static int Decode(ReadOnlySpan<char> source, Span<int> destination)
    {
        int n = 0;
        int i = 0;
        while (i < source.Length)
        {
            char c = source[i];
            if (char.IsHighSurrogate(c)
                && i + 1 < source.Length
                && char.IsLowSurrogate(source[i + 1]))
            {
                destination[n++] = char.ConvertToUtf32(c, source[i + 1]);
                i += 2;
            }
            else
            {
                destination[n++] = c;
                i++;
            }
        }
        return n;
    }

    /// <summary>Counts the number of Unicode code points in <paramref name="source"/>.</summary>
    public static int Count(ReadOnlySpan<char> source)
    {
        int n = 0;
        int i = 0;
        while (i < source.Length)
        {
            char c = source[i];
            i += (char.IsHighSurrogate(c)
                  && i + 1 < source.Length
                  && char.IsLowSurrogate(source[i + 1])) ? 2 : 1;
            n++;
        }
        return n;
    }
}
