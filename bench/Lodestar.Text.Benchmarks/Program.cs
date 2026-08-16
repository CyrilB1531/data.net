using BenchmarkDotNet.Running;
using Lodestar.Text.Benchmarks.CrossLang;

// Five entry points: default (BenchmarkDotNet), "compare", "compare-persistence",
// "compare-metrics" and "roc-parallel" — usage for each is in bench/README.md.
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
    MetricsCrossLang.Run(args);
    return;
}

if (args.Length > 0 && args[0] == "roc-parallel")
{
    RocParallelBench.Run();
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
