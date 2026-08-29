using System.Diagnostics;
using System.Linq;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Search;

namespace Lodestar.Text.Benchmarks.CrossLang;

/// <summary>
/// What a binary sidecar would cost and save, in bytes first and milliseconds second.
/// </summary>
/// <remarks>
/// ADR 0011 says a sidecar is to be argued on size, and ADR 0051 says why: the encode costs
/// nothing over a memcpy, so removing it buys no write time. This prices the size exactly and
/// the read approximately, on the corpus the persistence rows use. Issue #436.
/// </remarks>
internal static class SidecarBench
{
    /// <summary>Timed runs per row. Odd, so the median is a run rather than a mean of two.</summary>
    private const int Repeats = 9;

    /// <summary>Untimed runs first, to settle the JIT and the allocator.</summary>
    private const int WarmupRuns = 3;

    public static void Run()
    {
        EmbeddingIndex index = PersistenceBenchmarks.BuildIndex();
        int dimension = index.Dimension;
        int count = index.Count;

        byte[] artifact;
        using (var stream = new MemoryStream())
        {
            index.Save(stream);
            artifact = stream.ToArray();
        }

        float[] block = Block(count, dimension);
        byte[] npy;
        using (var stream = new MemoryStream())
        {
            NpyFile.Write(stream, block, count, dimension);
            npy = stream.ToArray();
        }

        // The head is what a sidecar still has to write: schema, version, dimension, the
        // normalize flag, the count and every id. Measured by subtraction, exactly.
        long base64 = 4L * ((block.Length * sizeof(float) + 2) / 3);
        long head = artifact.Length - base64;

        // Interleaved one round each, not one row to completion: save-phases learned that a
        // collection storm lands inside whichever row is unlucky and makes it meaningless.
        (string Name, Action Work)[] rows =
        [
            ("load artifact ", () => GC.KeepAlive(EmbeddingIndex.Load(new MemoryStream(artifact)))),
            ("read npy block", () => GC.KeepAlive(NpyFile.Read(new MemoryStream(npy), Unbounded))),
            ("rebuild index ", () => Rebuild(block, count, dimension)),
            ("sidecar floor ", () => Floor(npy, count, dimension)),
            ("ingest copy   ", () => IngestCopy(npy, dimension)),
            ("ingest only   ", () => IngestOnly(block, dimension)),
        ];

        double[][] samples = Rounds.Interleave(rows, Repeats, WarmupRuns);
        double[] medians = [.. samples.Select(Rounds.Median)];

        string report =
            $"artifact        {artifact.Length,12:N0} bytes{Environment.NewLine}" +
            $"  block base64  {base64,12:N0} bytes{Environment.NewLine}" +
            $"  head          {head,12:N0} bytes  (schema, flags, {count:N0} ids){Environment.NewLine}" +
            $"npy block       {npy.Length,12:N0} bytes{Environment.NewLine}" +
            $"sidecar total   {head + npy.Length,12:N0} bytes  " +
            $"({artifact.Length / (double)(head + npy.Length):F3}x smaller){Environment.NewLine}" +
            Environment.NewLine +
            string.Join(
                Environment.NewLine,
                rows.Select((row, i) =>
                    $"{row.Name}  median {medians[i],8:F3}  min {samples[i].Min(),8:F3}  max {samples[i].Max(),8:F3} ms")) +
            Environment.NewLine +
            $"{Environment.NewLine}load / floor    {medians[0] / medians[3]:F2}x   " +
            $"load / rebuild  {medians[0] / medians[2]:F2}x   " +
            $"load / ingest   {medians[0] / medians[4]:F2}x   " +
            $"ingest / floor  {medians[4] / medians[3]:F2}x";

        // console-print: this subcommand's whole output, and one call so one marker covers it.
        Console.WriteLine(report);
    }

    /// <summary>A .npy of the benchmark corpus needs no artifact limit; it is our own bytes.</summary>
    private static ArtifactLoadOptions Unbounded => new() { MaxTotalBytes = 1L << 31 };

    /// <summary>The same corpus BuildIndex draws, as one flat block.</summary>
    /// <remarks>
    /// Generated rather than read back out of the index, which exposes no vector accessor. The
    /// index normalizes on insertion so these are not its stored values, and for what this
    /// measures — a byte count and the cost of moving it — that does not matter.
    /// </remarks>
    private static float[] Block(int count, int dimension)
    {
        var all = new float[count * dimension];
        uint state = 12_345;
        for (int i = 0; i < all.Length; i++)
        {
            state ^= state << 13;
            state ^= state >> 17;
            state ^= state << 5;
            all[i] = (state & 0xFFFFFF) / (float)0xFFFFFF - 0.5f;
        }
        return all;
    }

    private static void Rebuild(float[] block, int count, int dimension)
    {
        var rebuilt = new EmbeddingIndex(dimension);
        for (int item = 0; item < count; item++)
        {
            rebuilt.Add(block.AsSpan(item * dimension, dimension));
        }
        GC.KeepAlive(rebuilt);
    }

    /// <summary>What a sidecar load could cost, not what one costs today.</summary>
    /// <remarks>
    /// The read plus a single copy into a backing store, which is what a bulk ingest would do.
    /// EmbeddingIndex has no such path — Add is per vector — so this is a floor in the sense
    /// ADR 0051's <c>block_copy_floor</c> is one: a bound the real thing cannot beat, measured
    /// rather than argued. The gap between it and <c>rebuild index</c> is what a bulk ingest
    /// would be worth, and the gap to <c>load artifact</c> is what the format would be worth.
    /// </remarks>
    private static void Floor(byte[] npy, int count, int dimension)
    {
        NpyBlock read = NpyFile.Read(new MemoryStream(npy), Unbounded);
        var backing = new float[count * dimension];
        read.Values.Span.CopyTo(backing);
        GC.KeepAlive(backing);
    }

    /// <summary>The sidecar route as it will exist: the block read, then the bulk ingest.</summary>
    /// <remarks>
    /// This is the row issue #474's gate is read off. It is what <c>sidecar floor</c> bounds —
    /// the floor pays a read and one copy, and this pays a read and one copy into an index —
    /// so the two landing together is the finding, and this landing near
    /// <c>rebuild index</c> instead is the refusal.
    /// </remarks>
    private static void IngestCopy(byte[] npy, int dimension)
    {
        NpyBlock read = NpyFile.Read(new MemoryStream(npy), Unbounded);
        GC.KeepAlive(EmbeddingIndex.FromBlock(
            read.Values.Span, dimension, BlockNormalization.AlreadyNormalized));
    }

    /// <summary>The ingest alone, on a block already in hand.</summary>
    /// <remarks>
    /// Separates the ingest's cost from the read's, so a later regression in either can be
    /// attributed. There is deliberately no row for <c>FromOwnedBlock</c>: it assigns four
    /// fields and is constant time whatever the block's size, so its ceiling is the
    /// <c>read npy block</c> row and a row of its own would publish noise.
    /// </remarks>
    private static void IngestOnly(float[] block, int dimension)
    {
        GC.KeepAlive(EmbeddingIndex.FromBlock(
            block, dimension, BlockNormalization.AlreadyNormalized));
    }
}
