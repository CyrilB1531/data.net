using BenchmarkDotNet.Running;
using Lodestar.Text.Benchmarks.CrossLang;

// long-comment: the entry points, and why the chain of ifs became a switch.
// Nine entry points, the default being BenchmarkDotNet; usage for each of the eight
// "compare*"/"roc-parallel"/"save-phases"/"heap-warmth"/"sidecar" is in bench/README.md.
// A switch rather than a chain of ifs: the ninth took the chain past the cognitive-complexity
// bar, and tools/check_bench_map.py reads these cases by name.
switch (args.Length > 0 ? args[0] : string.Empty)
{
    case "compare":
        LevenshteinCrossLang.Run(args);
        return;
    case "compare-indel":
        IndelCrossLang.Run(args);
        return;
    case "compare-persistence":
        PersistenceCrossLang.Run();
        return;
    case "compare-metrics":
        MetricsCrossLang.Run(args);
        return;
    case "roc-parallel":
        RocParallelBench.Run();
        return;
    case "save-phases":
        SavePhasesBench.Run();
        return;
    case "sidecar":
        SidecarBench.Run();
        return;
    case "heap-warmth":
        HeapWarmthBench.Run(args);
        return;
    default:
        break;
}

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

/// <summary>Benchmark entry point marker.</summary>
public partial class Program
{
    private Program()
    {
    }
}
