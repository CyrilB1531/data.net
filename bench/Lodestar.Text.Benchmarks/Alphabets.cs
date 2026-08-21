namespace Lodestar.Text.Benchmarks;

/// <summary>The two alphabets an operand builder draws from, held once so a band can change only its symbols.</summary>
/// <remarks>
/// Both are 27 symbols, so a band built from either differs in exactly one thing: where its
/// characters sit. <see cref="Latin"/> is indexed straight into the kernels' 256-entry equality
/// table; <see cref="Cjk"/> is above U+00FF, so it takes the side table #302 and #382 added.
/// Reading the two side by side is only an answer about the side table if that is the single
/// difference between them (#383).
/// </remarks>
internal static class Alphabets
{
    /// <summary>Latin, one byte a character: the alphabet every band used before #383.</summary>
    internal const string Latin = "abcdefghijklmnopqrstuvwxyz ";

    /// <summary>CJK, above Latin-1 and inside the BMP — one UTF-16 unit each, and off the dense table.</summary>
    internal const string Cjk = "一二三四五六七八九十百千万上下左右前後東西南北中大小山";
}
