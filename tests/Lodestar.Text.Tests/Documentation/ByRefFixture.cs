namespace Lodestar.Text.Tests.Documentation;

/// <summary>The three by-ref keywords, which metadata does not keep apart on its own.</summary>
/// <remarks>
/// Reflected over by <c>A_by_ref_parameter_keeps_the_keyword_it_was_declared_with</c>.
/// No member of Lodestar.Text.Distances takes a by-ref parameter yet, so the renderer's
/// handling of one stays unmeasured against the real surface until a later lot needs it
/// — and would then be documented as <c>ref</c> whatever it was declared.
/// </remarks>
internal static class ByRefFixture
{
    public static bool TryMeasure(in ReadOnlySpan<char> text, ref int budget, out int length)
    {
        length = text.Length;
        budget -= length;
        return budget >= 0;
    }
}
