using BenchmarkDotNet.Attributes;
using DataNet.Fuzzy;

namespace DataNet.Text.Benchmarks;


// CA1822 (mark members static): BenchmarkDotNet rejects static benchmarks —
// "Benchmarks MUST be instance methods, static methods are not supported."
// The build succeeds either way, so following this rule breaks the benchmarks
// at run time rather than compile time.
#pragma warning disable CA1822
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
