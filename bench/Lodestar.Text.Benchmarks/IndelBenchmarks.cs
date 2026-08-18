using BenchmarkDotNet.Attributes;
using Lodestar.Text.Distances;

namespace Lodestar.Text.Benchmarks;

// SonarLint S2245: a seeded Random builds a reproducible benchmark corpus; no security use.
#pragma warning disable S2245, CA5394

// CA1822 (mark members static): BenchmarkDotNet rejects static benchmarks, so
// following this rule breaks the benchmarks at run time rather than compile time.
#pragma warning disable CA1822

/// <summary>
/// Micro-benchmarks for <see cref="Indel"/> and the LCS kernel underneath it.
/// </summary>
/// <remarks>
/// <c>FuzzBenchmarks.Ratio</c> runs this path on one fixed 43-character pair — a
/// point, not a curve. This is <see cref="LevenshteinBenchmarks"/>' sweep over the
/// same operands, so the two distances can be read side by side (#273).
/// </remarks>
[MemoryDiagnoser]
public class IndelBenchmarks
{
    private string _a = string.Empty;
    private string _b = string.Empty;

    /// <summary>The cross-language corpus buckets, plus the gate's own neighbourhood.</summary>
    /// <remarks>
    /// 8, 32, 128 and 512 mirror <c>bench/corpus/pairs.json</c> so the two harnesses can be
    /// read together. 12, 16, 20 and 24 exist because neither of them measures the gate:
    /// <c>BitParallelMinPatternLength</c> is 16, inherited from Myers where it was calibrated
    /// for edit distance, and LCS has a lighter inner loop — so the constant may be wrong in
    /// either direction and nothing here could have said so (#273).
    /// </remarks>
    [Params(8, 12, 16, 20, 24, 32, 128, 512)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // The operands LevenshteinBenchmarks builds, seed included: comparing the
        // two distances is only meaningful over identical inputs.
        (_a, _b) = ScatteredPair.Build(Length);
    }

    [Benchmark(Baseline = true)]
    public int Distance_Utf16() => Indel.Distance(_a, _b);

    [Benchmark]
    public int Distance_CodePoint() => Indel.Distance(_a, _b, TextElement.CodePoint);

    /// <summary>What <c>fuzz.ratio</c> is, ×100 — the caller this lot exists for.</summary>
    [Benchmark]
    public double NormalizedSimilarity_Utf16() => Indel.NormalizedSimilarity(_a, _b);

    /// <summary>The kernel itself, so a change to it is readable without the Indel arithmetic.</summary>
    [Benchmark]
    public int SubsequenceLength_Utf16() => Lcs.SubsequenceLength(_a, _b);
}
