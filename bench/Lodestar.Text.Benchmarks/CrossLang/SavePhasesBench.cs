using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Lodestar.Embeddings.Search;

namespace Lodestar.Text.Benchmarks.CrossLang;

/// <summary>
/// The save path taken apart phase by phase, the way issue #324 took the load path apart.
/// </summary>
/// <remarks>
/// Each row is a strict subset of the one above, so the shares subtract rather than needing to
/// be argued, and <c>base64_encode</c> against <c>block_copy_floor</c> is the comparison the
/// whole lot turned on. Medians and the spread of every run, not a best-of: a best-of hides the
/// drift a shared machine introduces. ADR 0051 is what this measured.
/// </remarks>
internal static class SavePhasesBench
{
    /// <summary>How many timed runs each phase gets. Odd, so the median is a run rather than a mean of two.</summary>
    private const int Repeats = 9;

    /// <summary>Untimed runs before the first timed one, to settle the JIT and the allocator.</summary>
    private const int WarmupRuns = 3;

    public static void Run()
    {
        EmbeddingIndex index = PersistenceBenchmarks.BuildIndex();

        byte[] artifact;
        using (var stream = new MemoryStream())
        {
            index.Save(stream);
            artifact = stream.ToArray();
        }

        // The exact span WriteSingles hands to WriteBase64String on a little-endian
        // machine: the index's own floats, viewed as bytes and not copied.
        float[] vectors = Rebuild();
        byte[] block = new byte[vectors.Length * sizeof(float)];
        MemoryMarshal.AsBytes(vectors.AsSpan()).CopyTo(block);

        int encodedLength = Base64.GetMaxEncodedToUtf8Length(block.Length);
        byte[] encoded = new byte[encodedLength];
        byte[] copyTarget = new byte[block.Length];
        string[] ids = new string[10_000];
        for (int i = 0; i < ids.Length; i++)
        {
            ids[i] = $"doc-{i}";
        }

        // console-print: the denominator every share below is a fraction of.
        Console.WriteLine($"artifact       {artifact.Length:N0} bytes");
        // console-print: 15.36 MB is the block the GB/s column divides by.
        Console.WriteLine($"vector block   {block.Length:N0} bytes");
        // console-print: the 1.34x expansion, stated rather than inferred.
        Console.WriteLine($"base64 of it   {encodedLength:N0} bytes");
        // console-print: the machine, without which no absolute here is readable.
        Console.WriteLine($"runtime        {Environment.Version}");
        // console-print: core count, which decides whether a parallel reading is even possible.
        Console.WriteLine($"cores          {Environment.ProcessorCount}");
        Console.WriteLine(); // console-print: separates the conditions from the table.

        var phases = new List<(string Name, Func<long> Action)>
        {
            ("save_total", () =>
            {
                using var stream = new MemoryStream(artifact.Length);
                index.Save(stream);
                return stream.Length;
            }),
            ("write_base64_property", () =>
            {
                using var stream = new MemoryStream(encodedLength + 64);
                using var writer = new Utf8JsonWriter(stream, JsonWriterOptions);
                writer.WriteStartObject();
                writer.WriteBase64String("vectors", block);
                writer.WriteEndObject();
                writer.Flush();
                return stream.Length;
            }),
            ("ensure_finite_simd", () =>
            {
                var ceiling = new System.Numerics.Vector<float>(float.MaxValue);
                int width = System.Numerics.Vector<float>.Count;
                int i = 0;
                for (; i <= vectors.Length - width; i += width)
                {
                    if (!System.Numerics.Vector.LessThanOrEqualAll(
                        System.Numerics.Vector.Abs(new System.Numerics.Vector<float>(vectors.AsSpan(i, width))), ceiling))
                    {
                        return -1;
                    }
                }
                return i;
            }),
            ("write_ids_only", () =>
            {
                using var stream = new MemoryStream(200_000);
                using var writer = new Utf8JsonWriter(stream, JsonWriterOptions);
                writer.WriteStartObject();
                writer.WriteStartArray("ids");
                for (int i = 0; i < 10_000; i++)
                {
                    writer.WriteStringValue(ids[i]);
                }
                writer.WriteEndArray();
                writer.WriteEndObject();
                writer.Flush();
                return stream.Length;
            }),
            ("base64_encode", () =>
            {
                OperationStatus status = Base64.EncodeToUtf8(block, encoded, out int consumed, out int written);
                return status == OperationStatus.Done && consumed == block.Length ? written : -1;
            }),
            ("write_base64_chunked", () => WriteChunked(block, encodedLength)),
            ("write_b64_presized_abw", () => WritePresized(block, encodedLength)),
            ("write_b64_presized_pooled", () => WritePooled(block, encodedLength)),
            ("block_copy_floor", () =>
            {
                block.AsSpan().CopyTo(copyTarget);
                return copyTarget.Length;
            }),
        };

        Report(RunInterleaved(phases), block.Length);

        // Through the harness that produced the published 5.949 ms — best-of-5 over scaled
        // iterations, not a median of single calls. The share depends on which one it is.
        Harness.OperationResult canonical = Harness.Measure("embedding_index_save", () =>
        {
            using var stream = new MemoryStream(artifact.Length);
            index.Save(stream);
            return stream.Length;
        }, artifact.Length);

        // The control: it reads the artifact this change does not touch, through the same
        // harness, which is the row #323 used as its own control for the same reason.
        Harness.OperationResult control = Harness.Measure("embedding_index_load", () =>
        {
            using var stream = new MemoryStream(artifact);
            return EmbeddingIndex.Load(stream);
        }, artifact.Length);

        Console.WriteLine(); // console-print: separates the phase table from the canonical rows.
        // console-print: names the methodology; the share depends on which denominator it is.
        Console.WriteLine($"canonical harness (best-of-{Harness.RepeatCount}, the published methodology):");
        // console-print: the row the nightly publishes, so this page can be compared to it.
        Console.WriteLine(
            $"  embedding_index_save = {canonical.MsPerOp:F3} ms wall, {canonical.CpuMsPerOp:F3} ms cpu");
        // console-print: the control; a window with no noise floor states nothing.
        Console.WriteLine(
            $"  embedding_index_load = {control.MsPerOp:F3} ms wall, {control.CpuMsPerOp:F3} ms cpu   [control]");
    }

    /// <summary>The sliced write, as a diagnostic rather than as the change.</summary>
    /// <remarks>
    /// Slices cut on 12-byte boundaries into one rented buffer, so nothing grows to hold 20 MB.
    /// Writes the string token by hand, which is the point: <c>WriteBase64String</c> is the
    /// method that cannot be given the block in pieces.
    /// </remarks>
    private static long WriteChunked(byte[] block, int encodedLength)
    {
        const int sliceBytes = 240 * 1024;
        using var stream = new MemoryStream(encodedLength + 64);
        byte[] scratch = ArrayPool<byte>.Shared.Rent(Base64.GetMaxEncodedToUtf8Length(sliceBytes));
        try
        {
            stream.WriteByte((byte)'"');
            for (int offset = 0; offset < block.Length; offset += sliceBytes)
            {
                int take = Math.Min(sliceBytes, block.Length - offset);
                OperationStatus status = Base64.EncodeToUtf8(
                    block.AsSpan(offset, take), scratch, out _, out int written);
                if (status != OperationStatus.Done)
                {
                    return -1;
                }
                stream.Write(scratch, 0, written);
            }
            stream.WriteByte((byte)'"');
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(scratch);
        }
        return stream.Length;
    }

    /// <summary>Candidate C: the writer over a pre-sized <see cref="ArrayBufferWriter{T}"/>.</summary>
    /// <remarks>
    /// Keeps <c>WriteBase64String</c>. Asks whether the cost is the successive doubling, which
    /// pre-sizing removes, or the contiguous 20 MB itself, which it cannot.
    /// </remarks>
    private static long WritePresized(byte[] block, int encodedLength)
    {
        using var stream = new MemoryStream(encodedLength + 64);
        var buffer = new ArrayBufferWriter<byte>(encodedLength + 64);
        using (var writer = new Utf8JsonWriter(buffer, JsonWriterOptions))
        {
            writer.WriteStartObject();
            writer.WriteBase64String("vectors", block);
            writer.WriteEndObject();
            writer.Flush();
        }
        stream.Write(buffer.WrittenSpan);
        return stream.Length;
    }

    /// <summary>Candidate D: the same, over a pooled buffer whose pages are already committed.</summary>
    /// <remarks>
    /// #324 found most of the load's allocation phase was the OS committing pages on first
    /// touch, which no allocation strategy avoids except reuse. This asks the write direction.
    /// </remarks>
    private static long WritePooled(byte[] block, int encodedLength)
    {
        using var stream = new MemoryStream(encodedLength + 64);
        byte[] rented = ArrayPool<byte>.Shared.Rent(encodedLength + 64);
        try
        {
            var buffer = new PooledBufferWriter(rented);
            using var writer = new Utf8JsonWriter(buffer, JsonWriterOptions);
            writer.WriteStartObject();
            writer.WriteBase64String("vectors", block);
            writer.WriteEndObject();
            writer.Flush();
            stream.Write(rented, 0, buffer.WrittenCount);
            return stream.Length;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rented);
        }
    }

    /// <summary>An <see cref="IBufferWriter{T}"/> over one array that is already big enough.</summary>
    private sealed class PooledBufferWriter(byte[] buffer) : IBufferWriter<byte>
    {
        public int WrittenCount { get; private set; }

        public void Advance(int count) => WrittenCount += count;

        public Memory<byte> GetMemory(int sizeHint = 0) => buffer.AsMemory(WrittenCount);

        public Span<byte> GetSpan(int sizeHint = 0) => buffer.AsSpan(WrittenCount);
    }

    /// <summary>The writer options the artifacts are written with, so the row is the real one.</summary>
    private static JsonWriterOptions JsonWriterOptions => new()
    {
        Indented = false,
        SkipValidation = false,
    };

    /// <summary>The same 10 000 × 384 block <see cref="PersistenceBenchmarks.BuildIndex"/> generates.</summary>
    /// <remarks>
    /// Rebuilt rather than read off the index, which does not expose its backing array and should
    /// not. The index normalizes on <c>Add</c>, so the two blocks are not equal element for
    /// element — only the size and the generator matter here, and the encode's cost does not
    /// depend on the values.
    /// </remarks>
    private static float[] Rebuild()
    {
        const int count = 10_000;
        const int dimension = 384;
        var values = new float[count * dimension];
        uint state = 12_345;
        for (int i = 0; i < values.Length; i++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            values[i] = (state & 0xFFFFFF) / (float)0xFFFFFF - 0.5f;
        }

        // The count is what every share divides by, so it is what must hold.
        if (values.Length != count * dimension)
        {
            throw new InvalidOperationException("The rebuilt vector block is not the benchmark's shape.");
        }
        return values;
    }

    /// <summary>Runs every phase once per round, rather than one phase to completion.</summary>
    /// <remarks>
    /// The guide's interleaving rule, and not a refinement: a first cut ran each phase's runs
    /// back to back and reported <c>write_base64_property</c> at <b>136.7% of
    /// <c>save_total</c></b> — impossible for a strict subset, because a collection storm
    /// landed entirely inside one phase's window. Round-robin puts every phase in every one.
    /// </remarks>
    private static List<Phase> RunInterleaved(List<(string Name, Func<long> Action)> phases)
    {
        var runs = new double[phases.Count][];
        for (int p = 0; p < phases.Count; p++)
        {
            runs[p] = new double[Repeats];
            for (int w = 0; w < WarmupRuns; w++)
            {
                GC.KeepAlive(phases[p].Action());
            }
        }

        for (int round = 0; round < Repeats; round++)
        {
            for (int p = 0; p < phases.Count; p++)
            {
                long start = Stopwatch.GetTimestamp();
                long result = phases[p].Action();
                runs[p][round] = (double)(Stopwatch.GetTimestamp() - start) / Stopwatch.Frequency * 1e3;
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
            measured.Add(new Phase(phases[p].Name, sorted));
        }
        return measured;
    }

    private static void Report(IReadOnlyList<Phase> phases, int blockBytes)
    {
        double total = phases[0].Median;

        // console-print: this table is the mode's entire output; it writes no artifact.
        Console.WriteLine($"| {"phase",-22} | {"median",9} | {"min",9} | {"max",9} | {"share",7} | {"GB/s",7} |");
        // console-print: the separator that makes the rows a table a reader can paste.
        Console.WriteLine($"| {new string('-', 22)} | {new string('-', 9)} | {new string('-', 9)} | {new string('-', 9)} | {new string('-', 7)} | {new string('-', 7)} |");

        foreach (Phase phase in phases)
        {
            double gbPerSecond = blockBytes / (phase.Median / 1e3) / 1e9;
            // console-print: one measured phase; these rows are the measurement itself.
            Console.WriteLine(
                $"| {phase.Name,-22} | {phase.Median,9:F3} | {phase.Min,9:F3} | {phase.Max,9:F3} | "
                + $"{phase.Median / total,7:P1} | {gbPerSecond,7:F2} |");
        }

        Console.WriteLine(); // console-print: separates the table from the note under it.
        // console-print: the units, which the guide's protocol asks a table to carry.
        Console.WriteLine("median ms, 9 runs each; share is of save_total; GB/s counts the 15.36 MB input block.");
    }

    private sealed record Phase(string Name, double[] Runs)
    {
        public double Median => Runs[Runs.Length / 2];

        public double Min => Runs[0];

        public double Max => Runs[Runs.Length - 1];
    }
}
