using Accord.Statistics.Analysis;
using Accord.Statistics.Testing;
using BenchmarkDotNet.Attributes;
using Lodestar.Stats;

namespace Lodestar.Stats.Benchmarks;

// SonarLint S2245: a seeded Random builds a reproducible benchmark corpus; no security use.
#pragma warning disable S2245, CA5394

// CA1822 (mark members static): BenchmarkDotNet rejects static benchmarks, and the
// build succeeds either way -- so following the rule breaks the run, not the compile.
#pragma warning disable CA1822

/// <summary>
/// Three families against Accord.Statistics.Testing, the one .NET library that
/// carried them.
/// </summary>
/// <remarks>
/// The timing is the smaller half. The larger half is that a second
/// implementation is a second opinion: where Accord and scipy disagree, the
/// corpus says which one this package follows -- see bench/README.md.
/// </remarks>
[MemoryDiagnoser]
public class StatsBenchmarks
{
    private double[] _a = [];
    private double[] _b = [];
    private double[][] _table = [];

    [Params(100, 10_000)]
    public int SampleSize { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        Random random = new(442);
        _a = [.. Enumerable.Range(0, SampleSize).Select(_ => random.NextDouble())];
        _b = [.. Enumerable.Range(0, SampleSize).Select(_ => random.NextDouble() + 0.1)];
        _table = [[30.0, 20.0], [15.0, 35.0]];
    }

    [Benchmark(Baseline = true)]
    public double LodestarWelchT() => TTest.Independent(_a, _b).PValue;

    [Benchmark]
    public double AccordWelchT() => new TwoSampleTTest(_a, _b, assumeEqualVariances: false).PValue;

    [Benchmark]
    public double LodestarMannWhitney() => MannWhitney.Test(_a, _b).PValue;

    [Benchmark]
    public double AccordMannWhitney() => new MannWhitneyWilcoxonTest(_a, _b).PValue;

    [Benchmark]
    public double LodestarChiSquare() => ChiSquare.Contingency(_table).PValue;

    // yatesCorrection: true matches Lodestar.Stats' own default (Continuity.Applied) --
    // Accord's own default (false) would time two different statistics under one name.
    [Benchmark]
    public double AccordChiSquare() =>
        new ChiSquareTest(new ConfusionMatrix(30, 20, 15, 35), yatesCorrection: true).PValue;
}
