namespace DataNet.Text;

/// <summary>
/// Selects the unit of comparison used by character-based distance algorithms.
/// </summary>
/// <remarks>
/// The single most important source of divergence from Python reference
/// libraries: a Python <c>str</c> iterates code points, a .NET
/// <see cref="string"/> iterates UTF-16 code units. See
/// <c>docs/decisions/0002-unicode-comparison-unit.md</c> for why, and for the
/// deferred grapheme-cluster option.
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
