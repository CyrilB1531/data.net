using BenchmarkDotNet.Attributes;
using Lodestar.Text.Distances;

namespace Lodestar.Text.Benchmarks;

// SonarLint S2245: a seeded Random builds a reproducible benchmark corpus; no security use.
#pragma warning disable S2245, CA5394

/// <summary><see cref="Levenshtein"/> in code-point mode, on operands that leave the BMP.</summary>
/// <remarks>
/// <see cref="LevenshteinBenchmarks"/> is ASCII, so its <c>Distance_CodePoint</c>
/// row measures a decode over 27 symbols, not this mode's case (#208).
/// <see cref="Distinct"/> is a parameter because the fast path has a ceiling at
/// 255 distinct symbols: 32 stays under it, 512 outgrows it, so one run measures
/// the win and the ceiling. <c>bench/README.md</c> has the corpus.
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
    /// <remarks>
    /// 16 to 40 straddles <c>MyersMinPatternLength</c>, which was tuned for the
    /// character path and inherited rather than confirmed here (#255 review).
    /// </remarks>
    [Params(16, 24, 32, 40, 128, 512)]
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

        // Both ends differ, so Trim strips nothing and the pattern the gate sees is
        // Length -- scattered mutations alone trimmed 16 to a couple of symbols.
        b[0] = alphabet[(Array.IndexOf(alphabet, a[0]) + 1) % Distinct];
        b[Length - 1] = alphabet[(Array.IndexOf(alphabet, a[Length - 1]) + 1) % Distinct];

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

    // Context, not a comparison: a different question, whose own fast path
    // refuses these operands because every character here is a surrogate.
    [Benchmark]
    public int Distance_Utf16() => Levenshtein.Distance(_a, _b);
}
