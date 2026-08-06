using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using DataNet.Text.Benchmarks.CrossLang;

// Two entry points:
//   * default        -> BenchmarkDotNet (rigorous intra-C# micro-benchmarks), e.g.
//       dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter *Levenshtein*
//   * "compare"      -> matched cross-language throughput harness vs the Python
//                       side (bench/python/bench_levenshtein.py), e.g.
//       dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- compare
//       dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- compare --codepoint
//   * "compare-persistence" -> the same, for the #58 loaders and the TF-IDF
//                       round trip (bench/python/bench_persistence.py)
//   * "compare-metrics" -> the same, for the #61 classification metrics
//                       (bench/python/bench_metrics.py)
if (args.Length > 0 && args[0] == "compare")
{
    LevenshteinCrossLang.Run(args);
    return;
}

if (args.Length > 0 && args[0] == "compare-persistence")
{
    PersistenceCrossLang.Run();
    return;
}

if (args.Length > 0 && args[0] == "compare-metrics")
{
    MetricsCrossLang.Run();
    return;
}

// Microsoft.ML.OnnxRuntime (pulled in transitively for BatchEmbeddingBenchmarks)
// ships a managed assembly that is not marked as optimized. BenchmarkDotNet's
// OptimizationValidator checks every assembly the *whole process* loads, not
// only the one the filtered benchmark class lives in, so as soon as this
// project references it, every benchmark class in this assembly — Levenshtein,
// StopWord, Metrics, all of them — fails validation, not only the batch one.
// BatchEmbeddingBenchmarks already disables this validator on itself
// (NonOptimizedOnnxRuntime) for the reason given there: it is a managed shim
// over native code, consumed exactly as a user consumes it from nuget.org.
// That reasoning is not specific to one benchmark class, so the disable is
// applied once here instead of copied onto every other class that would
// otherwise fail for a dependency it never touches.
var config = ManualConfig.Create(DefaultConfig.Instance);
config.Options |= ConfigOptions.DisableOptimizationsValidator;

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);

/// <summary>Benchmark entry point marker.</summary>
public partial class Program
{
    private Program()
    {
    }
}
