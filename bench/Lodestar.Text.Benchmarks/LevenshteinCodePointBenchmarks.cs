using BenchmarkDotNet.Attributes;
using Lodestar.Text.Distances;

namespace Lodestar.Text.Benchmarks;

// SonarLint S2245: a seeded Random builds a reproducible benchmark corpus; no security use.
#pragma warning disable S2245, CA5394

/// <summary>
/// <see cref="Levenshtein"/> in code-point mode, over operands that actually
/// leave the Basic Multilingual Plane.
/// </summary>
/// <remarks>
/// <see cref="LevenshteinBenchmarks"/> draws both operands from
/// <c>"abcdefghijklmnopqrstuvwxyz "</c>, so its <c>Distance_CodePoint</c> row
/// decodes ASCII into a sequence identical to the UTF-16 one and measures the
/// decode over a 27-symbol alphabet. That is not the mode's case: decision 0002
/// points a caller at it precisely for supplementary characters, where the two
/// readings differ (#208).
/// <para>
/// <see cref="Distinct"/> is a parameter rather than a constant because the fast
/// path has a ceiling: a pattern is renamed into a dense alphabet of 255 symbols,
/// and one holding more than that falls back to the DP. 32 is the side that takes
/// the fast path at every length here, 512 the side that stops taking it once the
/// pattern outgrows the alphabet -- so the pair measures the win and the ceiling
/// in the same run rather than reporting only the flattering half.
/// </para>
/// </remarks>
[MemoryDiagnoser]
public class LevenshteinCodePointBenchmarks
{
    // U+1F300..U+1FAFF, the emoji block the oracle corpus draws its
    // long_supplementary family from. Every one of them is a surrogate pair.
    private const int SupplementaryBase = 0x1F300;
    private const int SupplementaryCount = 0x1FAFF - 0x1F300 + 1;

    private string _a = string.Empty;
    private string _b = string.Empty;

    /// <summary>Length of the generated operands, in code points.</summary>
    [Params(32, 128, 512)]
    public int Length { get; set; }

    /// <summary>How many distinct code points the operands are drawn from.</summary>
    [Params(32, 512)]
    public int Distinct { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        var rng = new Random(42);
        int[] alphabet = new int[Distinct];
        for (int i = 0; i < Distinct; i++)
        {
            alphabet[i] = SupplementaryBase + (i * SupplementaryCount / Distinct);
        }

        int[] a = new int[Length];
        for (int i = 0; i < Length; i++)
        {
            a[i] = alphabet[rng.Next(Distinct)];
        }

        int[] b = (int[])a.Clone();
        for (int i = 0; i < Math.Max(1, Length / 10); i++)
        {
            b[rng.Next(Length)] = alphabet[rng.Next(Distinct)];
        }

        _a = Compose(a);
        _b = Compose(b);
    }

    private static string Compose(int[] codePoints)
    {
        var text = new System.Text.StringBuilder(codePoints.Length * 2);
        foreach (int codePoint in codePoints)
        {
            text.Append(char.ConvertFromUtf32(codePoint));
        }
        return text.ToString();
    }

    [Benchmark(Baseline = true)]
    public int Distance_CodePoint() => Levenshtein.Distance(_a, _b, TextElement.CodePoint);

    // Context rather than a comparison: the same pair read as UTF-16 units is a
    // different question with a different answer, and its own fast path already
    // refuses these operands -- every one of them is a surrogate.
    [Benchmark]
    public int Distance_Utf16() => Levenshtein.Distance(_a, _b);
}
