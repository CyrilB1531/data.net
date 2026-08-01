namespace DataNet.Text;

/// <summary>
/// Selects the unit of comparison used by character-based distance algorithms.
/// </summary>
/// <remarks>
/// <para>
/// This choice is the single most important source of divergence from Python
/// reference libraries. A Python <c>str</c> iterates over Unicode code points,
/// whereas a .NET <see cref="string"/> iterates over UTF-16 code units: a
/// character outside the Basic Multilingual Plane (emoji, rare ideographs) is a
/// surrogate pair and therefore occupies <em>two</em> code units. A naive
/// <see cref="char"/>-based edit distance will thus disagree with Python on such
/// inputs.
/// </para>
/// <para>
/// Grapheme-cluster comparison (user-perceived characters, e.g. an emoji with a
/// skin-tone modifier) is intentionally not yet offered; see
/// <c>docs/decisions/0002-unicode-comparison-unit.md</c>.
/// </para>
/// </remarks>
public enum TextElement
{
    /// <summary>
    /// Compare individual UTF-16 code units (<see cref="char"/>). This is the
    /// default: it is allocation-free and fastest, and it agrees with Python for
    /// any input confined to the Basic Multilingual Plane.
    /// </summary>
    Utf16Unit = 0,

    /// <summary>
    /// Compare Unicode scalar values (code points). This matches the iteration
    /// semantics of a Python <c>str</c> and is required for exact parity with
    /// rapidfuzz / jellyfish on strings containing supplementary-plane
    /// characters. Costs one pooled decode pass per operand.
    /// </summary>
    CodePoint = 1,
}
