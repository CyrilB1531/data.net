using System.Reflection;
using System.Runtime.Versioning;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using DataNet.Embeddings.Search;
using DataNet.Metrics;
using DataNet.Text.Distances;

// Runs the shared benchmark bodies against the netstandard2.0 build.
//
// Two things make that claim true rather than merely intended.
//
// First, the in-process toolchain. BenchmarkDotNet's default toolchain generates
// and builds its own project per run, which re-resolves the ProjectReference and
// silently restores the net10.0 build — both suites then measure the same
// assemblies while looking correct. Running in-process removes the generated
// project, so the benchmarks execute against exactly what this process loaded.
//
// Second, the assertion below. An isolation failure is invisible in the numbers
// unless you already know what to expect, so it is checked rather than eyeballed.

if (!AssertLoaded(typeof(Levenshtein), ".NETStandard,Version=v2.0") ||
    !AssertLoaded(typeof(VectorMath), ".NETStandard,Version=v2.0") ||
    !AssertLoaded(typeof(ConfusionMatrix), ".NETStandard,Version=v2.0"))
{
    return 1;
}

// Also disables OptimizationValidator for the non-optimized Microsoft.ML.OnnxRuntime
// assembly this project links transitively (BatchEmbeddingBenchmarks.cs) — see the
// longer explanation in DataNet.Text.Benchmarks/Program.cs. The validator checks
// every assembly the process loads, so it fails for every benchmark class here,
// not only the one that references ONNX.
var config = DefaultConfig.Instance
    .AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance))
    .WithOptions(ConfigOptions.DisableOptimizationsValidator);

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
return 0;

static bool AssertLoaded(Type probe, string expectedFramework)
{
    Assembly assembly = probe.Assembly;
    string? actual = assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
    string name = assembly.GetName().Name ?? "<unknown>";

    if (string.Equals(actual, expectedFramework, StringComparison.Ordinal))
    {
        Console.WriteLine($"// {name}: {actual}");
        return true;
    }

    Console.Error.WriteLine(
        $"ERROR: {name} was built for '{actual}', expected '{expectedFramework}'. " +
        "The benchmark is measuring the wrong build; results would be meaningless.");
    return false;
}

/// <summary>Benchmark entry point marker.</summary>
public partial class Program
{
    private Program()
    {
    }
}
