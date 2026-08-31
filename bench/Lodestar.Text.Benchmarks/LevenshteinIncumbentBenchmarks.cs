using BenchmarkDotNet.Attributes;
using Lodestar.Text.Distances;

namespace Lodestar.Text.Benchmarks;

// SonarLint S2245: a seeded Random builds a reproducible benchmark corpus; no security use.
#pragma warning disable S2245, CA5394

// CA1822 (mark members static): BenchmarkDotNet rejects static benchmarks, and the
// build succeeds either way -- so following the rule breaks the run, not the compile.
#pragma warning disable CA1822

/// <summary>
/// <see cref="Levenshtein"/> against the .NET libraries a reader would otherwise
/// reach for, as issue #438 requires of every package.
/// </summary>
/// <remarks>
/// The default overload is the baseline on purpose: it compares UTF-16 code units,
/// which is what all three incumbents compare, so this is like for like. The
/// <see cref="TextElement.CodePoint"/> overload answers a different question and
/// <see cref="LevenshteinBenchmarks"/> already times it.
/// </remarks>
[MemoryDiagnoser]
public class LevenshteinIncumbentBenchmarks
{
    private readonly F23.StringSimilarity.Levenshtein _f23 = new();
    private string _a = string.Empty;
    private string _b = string.Empty;

    /// <summary>Length of the generated operands.</summary>
    [Params(8, 64, 512)]
    public int Length { get; set; }

    [GlobalSetup]
    public void Setup() => (_a, _b) = ScatteredPair.Build(Length);

    [Benchmark(Baseline = true)]
    public int Lodestar() => Levenshtein.Distance(_a, _b);

    [Benchmark]
    public int Fastenshtein() => global::Fastenshtein.Levenshtein.Distance(_a, _b);

    [Benchmark]
    public int Quickenshtein() => global::Quickenshtein.Levenshtein.GetDistance(_a, _b);

    // Allocating, and deliberately not hoisted out of the measurement: F23 returns a
    // double from an instance method, and the instance is the part a caller reuses.
    [Benchmark]
    public double F23_StringSimilarity() => _f23.Distance(_a, _b);
}
