using System.Reflection;
using System.Runtime.Versioning;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Toolchains.InProcess.Emit;
using Lodestar.Embeddings.Search;
using Lodestar.Metrics;
using Lodestar.Text.Distances;

// In-process, so BenchmarkDotNet cannot regenerate a project that silently
// restores net10.0 instead; the assertion below checks that, rather than trusting it.

if (!AssertLoaded(typeof(Levenshtein), ".NETStandard,Version=v2.0") ||
    !AssertLoaded(typeof(VectorMath), ".NETStandard,Version=v2.0") ||
    !AssertLoaded(typeof(ConfusionMatrix), ".NETStandard,Version=v2.0"))
{
    return 1;
}

var config = DefaultConfig.Instance
    .AddJob(Job.Default.WithToolchain(InProcessEmitToolchain.Instance));

BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
return 0;

static bool AssertLoaded(Type probe, string expectedFramework)
{
    Assembly assembly = probe.Assembly;
    string? actual = assembly.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
    string name = assembly.GetName().Name ?? "<unknown>";

    if (string.Equals(actual, expectedFramework, StringComparison.Ordinal))
    {
        // console-print: which build was measured; the pair of the refusal below.
        Console.WriteLine($"// {name}: {actual}");
        return true;
    }

    // console-print: the wrong build was measured, so every number below is a lie.
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
