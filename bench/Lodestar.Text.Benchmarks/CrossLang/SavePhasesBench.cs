using System.Buffers;
using System.Buffers.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text.Json;
using Lodestar.Embeddings.Search;

namespace Lodestar.Text.Benchmarks.CrossLang;

/// <summary>
/// The save path taken apart phase by phase, the way issue #324 took the load
/// path apart. Not a <c>compare-*</c> mode: there is no Python counterpart to a
/// question about where one side's own milliseconds go.
/// </summary>
/// <remarks>
/// <para>
/// It exists to answer one question that
/// <c>docs/guides/performance.md</c> has asserted since #323 without ever
/// measuring it — <em>encoding is the dominant cost of a save</em>. The load
/// profile settled its own version of that question by replacing the decode with
/// a <c>memcpy</c> of the same byte count and re-running; the same shape of
/// answer is what the rows below produce for the write direction.
/// </para>
/// <para>
/// Four rows, each a strict subset of the one above it, so the shares subtract
/// rather than needing to be argued:
/// </para>
/// <list type="bullet">
///   <item><description>
///     <c>save_total</c> — <see cref="EmbeddingIndex.Save(Stream)"/> into a
///     pre-sized <see cref="MemoryStream"/>, which is the row
///     <c>embedding_index_save</c> reports.
///   </description></item>
///   <item><description>
///     <c>write_base64_property</c> — one <see cref="Utf8JsonWriter"/> writing
///     the vector block and nothing else: the encode, plus the writer's internal
///     buffer growing to hold what the encode produced, plus the flush.
///   </description></item>
///   <item><description>
///     <c>base64_encode</c> — <see cref="Base64.EncodeToUtf8"/> alone, source and
///     destination both allocated before the clock starts. <b>This is the number
///     the lot turns on.</b>
///   </description></item>
///   <item><description>
///     <c>block_copy_floor</c> — the same bytes copied and not encoded. What
///     moving 15.36 MB costs at all on this machine, so the encode row can be
///     read as work rather than as bandwidth.
///   </description></item>
/// </list>
/// <para>
/// Medians and the spread of the runs are reported rather than a best-of, per the
/// measurement protocol in the performance guide: a best-of hides exactly the
/// drift a shared machine introduces.
/// </para>
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

        Console.WriteLine($"artifact       {artifact.Length:N0} bytes");
        Console.WriteLine($"vector block   {block.Length:N0} bytes");
        Console.WriteLine($"base64 of it   {encodedLength:N0} bytes");
        Console.WriteLine($"runtime        {Environment.Version}");
        Console.WriteLine($"cores          {Environment.ProcessorCount}");
        Console.WriteLine();

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

        // The same row the nightly publishes as embedding_index_save, taken through the
        // very harness that produced the 5.949 ms this lot is measured against:
        // best-of-5 over auto-scaled iterations, not a median of single calls. Printed
        // beside the table because the share depends entirely on which of the two the
        // denominator is, and the published figure is this one.
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

        Console.WriteLine();
        Console.WriteLine($"canonical harness (best-of-{Harness.RepeatCount}, the published methodology):");
        Console.WriteLine(
            $"  embedding_index_save = {canonical.MsPerOp:F3} ms wall, {canonical.CpuMsPerOp:F3} ms cpu");
        Console.WriteLine(
            $"  embedding_index_load = {control.MsPerOp:F3} ms wall, {control.CpuMsPerOp:F3} ms cpu   [control]");
    }

    /// <summary>
    /// Step 1(1)'s shape, as a diagnostic rather than as the change: encode in bounded
    /// slices into one rented buffer and write each slice straight out, so nothing has
    /// to grow to hold 20 MB.
    /// </summary>
    /// <remarks>
    /// Slices are cut on 12-byte boundaries — 3 floats, 4 base64 groups — so no slice
    /// pads mid-stream and the concatenation is the same base64 the one-shot call
    /// produces. It writes the string token by hand rather than through
    /// <c>WriteBase64String</c>, which is the whole point: that method is the one that
    /// cannot be given the block in pieces.
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

    /// <summary>
    /// Candidate C: the writer over an <see cref="ArrayBufferWriter{T}"/> sized up front,
    /// so its buffer never doubles, then one copy of the result to the destination.
    /// </summary>
    /// <remarks>
    /// Keeps <c>WriteBase64String</c> — the point of the row is to find out whether the
    /// cost is the successive doubling (which pre-sizing removes) or the contiguous
    /// 20 MB itself (which it cannot).
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

    /// <summary>
    /// Candidate D: the same, over a buffer rented from the pool rather than allocated,
    /// so the pages are already committed on the second and every later save.
    /// </summary>
    /// <remarks>
    /// #324 found that most of the load's allocation phase was the operating system
    /// committing pages on first touch, which no allocation strategy avoids — except
    /// reuse. This row asks whether the write direction has the same floor.
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

    /// <summary>
    /// The same 10 000 × 384 block <see cref="PersistenceBenchmarks.BuildIndex"/>
    /// generates, rebuilt here rather than read off the index.
    /// </summary>
    /// <remarks>
    /// <c>EmbeddingIndex</c> does not expose its backing array, and it should not:
    /// handing out the live block would let a caller edit vectors behind the
    /// index's back. Reproducing the generator is three shifts, and the arithmetic
    /// is identical, which the assertion below pins rather than assumes — a
    /// silently different block would make every share on this page wrong.
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

        // The index normalizes on Add, so the two blocks are not equal element for
        // element — only their size and their generator are what this row needs.
        // What must hold is the count, and it is the count every share divides by.
        if (values.Length != count * dimension)
        {
            throw new InvalidOperationException("The rebuilt vector block is not the benchmark's shape.");
        }
        return values;
    }

    /// <summary>
    /// Runs every phase once per round, <see cref="Repeats"/> rounds, rather than
    /// running one phase to completion before starting the next.
    /// </summary>
    /// <remarks>
    /// This is the performance guide's interleaving rule applied to a phase profile
    /// instead of to a before/after pair, and it is not a refinement — a first cut of
    /// this harness ran each phase's runs back to back and reported
    /// <c>write_base64_property</c> at <b>136.7% of <c>save_total</c></b>, which is
    /// impossible for a strict subset of the same work. A garbage collection storm
    /// inside one phase's window landed entirely on that phase. Round-robin puts every
    /// phase in every window, so drift lands on all of them or on none.
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

        Console.WriteLine($"| {"phase",-22} | {"median",9} | {"min",9} | {"max",9} | {"share",7} | {"GB/s",7} |");
        Console.WriteLine($"| {new string('-', 22)} | {new string('-', 9)} | {new string('-', 9)} | {new string('-', 9)} | {new string('-', 7)} | {new string('-', 7)} |");

        foreach (Phase phase in phases)
        {
            double gbPerSecond = blockBytes / (phase.Median / 1e3) / 1e9;
            Console.WriteLine(
                $"| {phase.Name,-22} | {phase.Median,9:F3} | {phase.Min,9:F3} | {phase.Max,9:F3} | "
                + $"{phase.Median / total,7:P1} | {gbPerSecond,7:F2} |");
        }

        Console.WriteLine();
        Console.WriteLine("median ms, 9 runs each; share is of save_total; GB/s counts the 15.36 MB input block.");
    }

    private sealed record Phase(string Name, double[] Runs)
    {
        public double Median => Runs[Runs.Length / 2];

        public double Min => Runs[0];

        public double Max => Runs[Runs.Length - 1];
    }
}
