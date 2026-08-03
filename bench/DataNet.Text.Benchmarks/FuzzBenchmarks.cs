using BenchmarkDotNet.Attributes;
using DataNet.Fuzzy;

namespace DataNet.Text.Benchmarks;

/// <summary>Per-call cost of the fuzzy ratios on representative sentence pairs.</summary>
[MemoryDiagnoser]
public class FuzzBenchmarks
{
    private const string A = "the quick brown fox jumps over the lazy dog";
    private const string B = "the lazy dog jumps over the quick brown fox";

    [Benchmark(Baseline = true)]
    public double Ratio() => Fuzz.Ratio(A, B);

    [Benchmark]
    public double PartialRatio() => Fuzz.PartialRatio(A, B);

    [Benchmark]
    public double TokenSortRatio() => Fuzz.TokenSortRatio(A, B);

    [Benchmark]
    public double TokenSetRatio() => Fuzz.TokenSetRatio(A, B);

    [Benchmark]
    public double WRatio() => Fuzz.WRatio(A, B);
}
