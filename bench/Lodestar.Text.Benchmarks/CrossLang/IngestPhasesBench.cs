using System.Diagnostics;
using System.Runtime.InteropServices;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Search;

namespace Lodestar.Text.Benchmarks.CrossLang;

/// <summary>The <c>.npy</c> ingest apart phase by phase, as <see cref="SavePhasesBench"/> took the save.</summary>
/// <remarks>
/// Issue #480: #466 removed a whole copy of the 15.36 MB block from the read and the row did not
/// move, while removing the copy into the index moved it by more than a copy is worth. Both
/// readings subtracted whole rows, which cannot say where the time went. Every phase carries the
/// collections it provoked beside its milliseconds, because on the artifact buffer ADR 0054 found
/// those two telling different stories.
/// </remarks>
internal static class IngestPhasesBench
{
    /// <summary>How many timed runs each phase gets. Odd, so the median is a run rather than a mean of two.</summary>
    private const int Repeats = 9;

    /// <summary>Untimed runs before the first timed one, to settle the JIT and the allocator.</summary>
    private const int WarmupRuns = 3;

    /// <summary>One float per 4 KB page, so an uninitialized array is actually committed.</summary>
    /// <remarks>
    /// Without it the allocation rows measure reserving address space rather than taking
    /// memory, which is the mistake that would make the allocation look free.
    /// </remarks>
    private const int FloatsPerPage = 1024;

    /// <summary>The bound the .npy rows load under, matching <see cref="PersistenceCrossLang"/>.</summary>
    private static ArtifactLoadOptions NpyLimits => new() { MaxTotalBytes = 1L << 31 };

    public static void Run()
    {
        EmbeddingIndex index = PersistenceBenchmarks.BuildIndex();
        float[] block = PersistenceBenchmarks.BuildBlock();
        int dimension = index.Dimension;
        int elements = block.Length;
        int blockBytes = elements * sizeof(float);

        byte[] npy;
        using (var stream = new MemoryStream())
        {
            NpyFile.Write(stream, block, index.Count, dimension);
            npy = stream.ToArray();
        }

        // A .npy of one float: everything but the payload, so what the header costs is a row
        // rather than an assumption. Its own payload is 4 bytes, below this table's resolution.
        byte[] tinyNpy;
        using (var stream = new MemoryStream())
        {
            NpyFile.Write(stream, [1f], 1);
            tinyNpy = stream.ToArray();
        }

        int dataStart = npy.Length - blockBytes;
        float[] reused = GC.AllocateUninitializedArray<float>(elements);
        float[] copyTarget = GC.AllocateUninitializedArray<float>(elements);

        // console-print: the block every GB/s column divides by, and the header above it.
        Console.WriteLine($"npy file       {npy.Length:N0} bytes ({dataStart} of header)");
        // console-print: the machine, without which no absolute in this table is readable.
        Console.WriteLine($"runtime        {Environment.Version}, {Environment.ProcessorCount} cores");
        // console-print: server GC changes what a large-object allocation costs, so it is stated.
        Console.WriteLine($"server GC      {System.Runtime.GCSettings.IsServerGC}");
        Console.WriteLine(); // console-print: separates the conditions from the table.

        var phases = new List<(string Name, Func<long> Action)>
        {
            ("ingest_total", () =>
            {
                NpyBlock read = NpyFile.Read(new MemoryStream(npy), NpyLimits);
                return EmbeddingIndex.FromOwnedBlock(
                    read.OwnedArray!, dimension, BlockNormalization.AlreadyNormalized).Count;
            }),
            ("read_stream_owned", () =>
                NpyFile.Read(new MemoryStream(npy), NpyLimits).Values.Length),
            ("read_memory_view", () =>
                NpyFile.Read(npy.AsMemory(), NpyLimits).Values.Length),
            ("stream_copy_floor", () =>
            {
                var stream = new MemoryStream(npy, dataStart, blockBytes, writable: false);
                return stream.Read(MemoryMarshal.AsBytes(reused.AsSpan()));
            }),
            ("allocate_cold", () => Touch(GC.AllocateUninitializedArray<float>(elements))),
            ("allocate_reused", () => Touch(reused)),
            ("parse_header_only", () => NpyFile.Read(tinyNpy.AsMemory(), NpyLimits).Values.Length),
            ("from_block_copy", () => EmbeddingIndex.FromBlock(
                block.AsSpan(), dimension, BlockNormalization.AlreadyNormalized).Count),
            ("from_owned_adopt", () => EmbeddingIndex.FromOwnedBlock(
                GC.AllocateUninitializedArray<float>(elements),
                dimension,
                BlockNormalization.AlreadyNormalized).Count),
            ("block_copy_floor", () =>
            {
                block.AsSpan().CopyTo(copyTarget);
                return copyTarget.Length;
            }),
        };

        Report(RunInterleaved(phases), blockBytes);

        // Through the harness that produced the published figures, which is best-of over scaled
        // iterations rather than a median of single calls: the share depends on which one it is.
        Harness.OperationResult canonical = Harness.Measure("embedding_index_ingest_npy", () =>
        {
            NpyBlock read = NpyFile.Read(new MemoryStream(npy), NpyLimits);
            return EmbeddingIndex.FromOwnedBlock(
                read.OwnedArray!, dimension, BlockNormalization.AlreadyNormalized);
        }, npy.Length);

        // The control: the row this diagnostic does not touch, and the neighbour every ratio
        // published for #466 was read against.
        byte[] artifact;
        using (var stream = new MemoryStream())
        {
            index.Save(stream);
            artifact = stream.ToArray();
        }

        Harness.OperationResult control = Harness.Measure("embedding_index_load_memory", () =>
            EmbeddingIndex.Load(artifact.AsMemory()), artifact.Length);

        Console.WriteLine(); // console-print: separates the phase table from the canonical rows.
        // console-print: names the methodology; a share is meaningless without its denominator.
        Console.WriteLine($"canonical harness (best-of-{Harness.RepeatCount}, the published methodology):");
        // console-print: the row the comparison publishes, so this page can be read against it.
        Console.WriteLine(
            $"  embedding_index_ingest_npy  = {canonical.MsPerOp:F3} ms wall, {canonical.CpuMsPerOp:F3} ms cpu");
        // console-print: the control; a window with no noise floor states nothing.
        Console.WriteLine(
            $"  embedding_index_load_memory = {control.MsPerOp:F3} ms wall, {control.CpuMsPerOp:F3} ms cpu   [control]");
    }

    /// <summary>Writes one float per page, so the array is committed rather than merely reserved.</summary>
    private static long Touch(float[] buffer)
    {
        for (int i = 0; i < buffer.Length; i += FloatsPerPage)
        {
            buffer[i] = 1f;
        }
        return buffer.Length;
    }

    /// <summary>Runs every phase once per round, so a drift in the machine lands on all of them.</summary>
    /// <remarks>
    /// Collections are counted across the timed runs rather than per run: a 15.36 MB allocation
    /// provokes at most one gen2 per run, and a column of zeroes and ones says less than a total.
    /// </remarks>
    private static List<Phase> RunInterleaved(List<(string Name, Func<long> Action)> phases)
    {
        for (int warm = 0; warm < WarmupRuns; warm++)
        {
            for (int p = 0; p < phases.Count; p++)
            {
                GC.KeepAlive(phases[p].Action());
            }
        }

        var runs = new double[phases.Count][];
        var collections = new int[phases.Count][];
        for (int p = 0; p < phases.Count; p++)
        {
            runs[p] = new double[Repeats];
            collections[p] = new int[3];
        }

        for (int round = 0; round < Repeats; round++)
        {
            for (int p = 0; p < phases.Count; p++)
            {
                int gen0 = GC.CollectionCount(0);
                int gen1 = GC.CollectionCount(1);
                int gen2 = GC.CollectionCount(2);

                long start = Stopwatch.GetTimestamp();
                long result = phases[p].Action();
                runs[p][round] = (double)(Stopwatch.GetTimestamp() - start) / Stopwatch.Frequency * 1e3;

                collections[p][0] += GC.CollectionCount(0) - gen0;
                collections[p][1] += GC.CollectionCount(1) - gen1;
                collections[p][2] += GC.CollectionCount(2) - gen2;
                if (result < 0)
                {
                    throw new InvalidOperationException($"Phase '{phases[p].Name}' did not complete.");
                }
            }
        }

        var measured = new List<Phase>(phases.Count);
        for (int p = 0; p < phases.Count; p++)
        {
            double[] sorted = (double[])runs[p].Clone();
            Array.Sort(sorted);
            measured.Add(new Phase(phases[p].Name, sorted, collections[p]));
        }
        return measured;
    }

    private static void Report(IReadOnlyList<Phase> phases, int blockBytes)
    {
        double total = phases[0].Median;

        // console-print: this table is the mode's entire output; it writes no artifact.
        Console.WriteLine(
            $"| {"phase",-18} | {"median",9} | {"min",9} | {"max",9} | {"share",7} | {"GB/s",7} "
            + $"| {"gen0",5} | {"gen1",5} | {"gen2",5} |");
        // console-print: the separator that makes the rows a table a reader can paste.
        Console.WriteLine(
            $"| {new string('-', 18)} | {new string('-', 9)} | {new string('-', 9)} | {new string('-', 9)} "
            + $"| {new string('-', 7)} | {new string('-', 7)} | {new string('-', 5)} | {new string('-', 5)} "
            + $"| {new string('-', 5)} |");

        foreach (Phase phase in phases)
        {
            double gbPerSecond = blockBytes / (phase.Median / 1e3) / 1e9;
            // console-print: one measured phase; these rows are the measurement itself.
            Console.WriteLine(
                $"| {phase.Name,-18} | {phase.Median,9:F3} | {phase.Min,9:F3} | {phase.Max,9:F3} "
                + $"| {phase.Median / total,7:P1} | {gbPerSecond,7:F2} | {phase.Gen0,5} | {phase.Gen1,5} "
                + $"| {phase.Gen2,5} |");
        }

        Console.WriteLine(); // console-print: separates the table from the note under it.
        // console-print: the units, which the guide's protocol asks a table to carry.
        Console.WriteLine(
            $"median ms over {Repeats} runs; share is of ingest_total; GB/s counts the 15.36 MB block; "
            + $"gen columns are collections summed over the {Repeats} runs, not per run.");
        Console.WriteLine(); // console-print: separates the units from the reading they enable.
        // console-print: the subtraction the mode exists to make, named so a reader can check it.
        Console.WriteLine(
            "read_stream_owned - stream_copy_floor is the read's allocation; allocate_cold - "
            + "allocate_reused prices the same thing independently. They should agree.");
    }

    private sealed record Phase(string Name, double[] Runs, int[] Collections)
    {
        public double Median => Runs[Runs.Length / 2];

        public double Min => Runs[0];

        public double Max => Runs[Runs.Length - 1];

        public int Gen0 => Collections[0];

        public int Gen1 => Collections[1];

        public int Gen2 => Collections[2];
    }
}
