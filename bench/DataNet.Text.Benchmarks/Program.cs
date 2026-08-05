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

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

/// <summary>Benchmark entry point marker.</summary>
public partial class Program
{
    private Program()
    {
    }
}
