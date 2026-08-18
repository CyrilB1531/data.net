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
/// <c>FuzzBenchmarks.Ratio</c> already runs this path, on one fixed pair of
/// 43-character sentences — a point, not a curve, and one that sits near the
/// bottom of where a bit-parallel kernel would begin to pay. This class is the
/// same length sweep <see cref="LevenshteinBenchmarks"/> carries, over the same
/// operands, so the two distances can be read side by side and the gate constant
/// for #273 is chosen from a measurement rather than inherited.
/// </remarks>
[MemoryDiagnoser]
public class IndelBenchmarks
{
    private string _a = string.Empty;
    private string _b = string.Empty;

    /// <summary>Length of the generated operands, matching the cross-language corpus buckets.</summary>
    [Params(8, 32, 128, 512)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        // The operands LevenshteinBenchmarks builds, seed included: comparing the
        // two distances is only meaningful over identical inputs.
        var rng = new Random(42);
        const string alphabet = "abcdefghijklmnopqrstuvwxyz ";
        char[] a = new char[Length];
        for (int i = 0; i < Length; i++)
        {
            a[i] = alphabet[rng.Next(alphabet.Length)];
        }

        char[] b = (char[])a.Clone();
        for (int i = 0; i < Math.Max(1, Length / 10); i++)
        {
            b[rng.Next(Length)] = alphabet[rng.Next(alphabet.Length)];
        }

        _a = new string(a);
        _b = new string(b);
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
