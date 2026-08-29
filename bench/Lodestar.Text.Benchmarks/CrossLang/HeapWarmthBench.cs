using System.Diagnostics;
using Lodestar.Embeddings.Search;

namespace Lodestar.Text.Benchmarks.CrossLang;

/// <summary>
/// <c>EmbeddingIndex.Load</c> in a process that has saved, against one that has not.
/// </summary>
/// <remarks>
/// The variable is the process, so the two states cannot be two rows of one run: a save warms
/// the heap for everything after it, and nothing inside one process undoes that honestly.
/// Hence three subcommands — <c>prepare</c> writes the artifact once, then <c>cold</c> loads it
/// having built and saved nothing, and <c>warm</c> does its saves <b>first</b> and then loads.
/// Issue #433, first seen in ADR 0051's consequences.
/// </remarks>
internal static class HeapWarmthBench
{
    /// <summary>Timed runs. Odd, so the median is a run rather than a mean of two.</summary>
    private const int Repeats = 9;

    /// <summary>Untimed runs first, to settle the JIT.</summary>
    private const int WarmupRuns = 2;

    /// <summary>Saves the warm state makes before it loads anything, as the harness does.</summary>
    private const int WarmingSaves = 12;

    public static void Run(string[] args)
    {
        string state = args.Length > 1 ? args[1] : "cold";
        string path = args.Length > 2
            ? args[2]
            : Path.Combine(Path.GetTempPath(), "lodestar-heap-warmth.json");

        if (state == "prepare")
        {
            PersistenceBenchmarks.BuildIndex().Save(path);

            // console-print: the path the two measuring processes are then pointed at.
            Console.WriteLine($"prepared        {path} ({new FileInfo(path).Length:N0} bytes)");
            return;
        }

        Measure(state == "warm", path);
    }

    private static void Measure(bool warm, string path)
    {
        // Read, never save: this is the only allocation the cold process makes before the loop,
        // and the warm one makes it too, so it cancels.
        byte[] artifact = File.ReadAllBytes(path);
        EmbeddingIndex? source = warm ? PersistenceBenchmarks.BuildIndex() : null;

        // The whole experiment, and it happens BEFORE the loop rather than inside it.
        // compare-persistence measures every save, then every load; a save between two loads
        // would add garbage competing with the load instead of leaving a heap behind it.
        for (int warming = 0; warming < WarmingSaves && source is not null; warming++)
        {
            using var scratch = new MemoryStream(artifact.Length);
            source.Save(scratch);
        }

        var samples = new List<double>(Repeats);
        long loadAllocated = 0;
        int[] before = [GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2)];

        for (int run = 0; run < WarmupRuns + Repeats; run++)
        {
            long allocatedBefore = GC.GetTotalAllocatedBytes(precise: true);
            long start = Stopwatch.GetTimestamp();
            using var stream = new MemoryStream(artifact);
            EmbeddingIndex loaded = EmbeddingIndex.Load(stream);
            double ms = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;

            GC.KeepAlive(loaded);
            if (run >= WarmupRuns)
            {
                samples.Add(ms);

                // The two states must agree here, or the comparison is of two workloads
                // rather than of one workload on two heaps.
                loadAllocated += GC.GetTotalAllocatedBytes(precise: true) - allocatedBefore;
            }
        }

        samples.Sort();

        // console-print: this subcommand's whole output is the four lines below.
        Console.WriteLine($"state           {(warm ? "warm" : "cold")}");
        Console.WriteLine($"load ms         median {samples[Repeats / 2]:F3}  min {samples[0]:F3}  p25 {samples[Repeats / 4]:F3}  p75 {samples[Repeats * 3 / 4]:F3}  max {samples[^1]:F3}");
        Console.WriteLine($"load allocated  {loadAllocated / Repeats:N0} bytes per load");
        Console.WriteLine($"collections     {GC.CollectionCount(0) - before[0]}/{GC.CollectionCount(1) - before[1]}/{GC.CollectionCount(2) - before[2]}");
    }
}
