using BenchmarkDotNet.Running;

// Runs every *Benchmarks class in the assembly. Filter from the CLI, e.g.:
//   dotnet run -c Release --project bench/DataNet.Text.Benchmarks -- --filter *Levenshtein*
BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

/// <summary>Benchmark entry point marker.</summary>
public partial class Program
{
    private Program()
    {
    }
}
