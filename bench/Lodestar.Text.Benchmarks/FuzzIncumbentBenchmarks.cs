using BenchmarkDotNet.Attributes;
using Lodestar.Fuzzy;

namespace Lodestar.Text.Benchmarks;

// CA1822: see LevenshteinIncumbentBenchmarks.
#pragma warning disable CA1822

/// <summary>Which <see cref="Fuzz"/> ratio a row of the incumbent table measures.</summary>
public enum FuzzOperation
{
    /// <summary><see cref="Fuzz.Ratio"/>.</summary>
    Ratio,

    /// <summary><see cref="Fuzz.PartialRatio"/>.</summary>
    PartialRatio,

    /// <summary><see cref="Fuzz.TokenSetRatio"/>.</summary>
    TokenSetRatio,

    /// <summary><see cref="Fuzz.WRatio"/>.</summary>
    WRatio,
}

/// <summary>
/// <see cref="Fuzz"/> against Raffinert.FuzzySharp, the maintained .NET port of
/// rapidfuzz's `fuzz` module and the incumbent issue #438 names for this package.
/// </summary>
/// <remarks>
/// The operation is a parameter rather than four pairs of methods so that each pair
/// gets its own baseline: with one baseline for the whole class, the ratio column
/// compares `PartialRatio` against `Ratio` instead of against its counterpart, which
/// is not the question. The switch costs both sides the same jump.
/// </remarks>
[MemoryDiagnoser]
public class FuzzIncumbentBenchmarks
{
    private const string A = "the quick brown fox jumps over the lazy dog";
    private const string B = "the lazy dog jumps over the quick brown fox";

    /// <summary>The ratio this row measures on both libraries.</summary>
    [Params(
        FuzzOperation.Ratio,
        FuzzOperation.PartialRatio,
        FuzzOperation.TokenSetRatio,
        FuzzOperation.WRatio)]
    public FuzzOperation Operation { get; set; }

    [Benchmark(Baseline = true)]
    public double Lodestar() => Operation switch
    {
        FuzzOperation.Ratio => Fuzz.Ratio(A, B),
        FuzzOperation.PartialRatio => Fuzz.PartialRatio(A, B),
        FuzzOperation.TokenSetRatio => Fuzz.TokenSetRatio(A, B),
        _ => Fuzz.WRatio(A, B),
    };

    [Benchmark]
    public double FuzzySharp() => Operation switch
    {
        FuzzOperation.Ratio => Raffinert.FuzzySharp.Fuzz.Ratio(A, B),
        FuzzOperation.PartialRatio => Raffinert.FuzzySharp.Fuzz.PartialRatio(A, B),
        FuzzOperation.TokenSetRatio => Raffinert.FuzzySharp.Fuzz.TokenSetRatio(A, B),
        _ => Raffinert.FuzzySharp.Fuzz.WeightedRatio(A, B),
    };
}
