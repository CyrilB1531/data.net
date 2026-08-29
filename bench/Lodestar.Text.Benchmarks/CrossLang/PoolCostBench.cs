using System.Buffers;
using System.Linq;
using System.Diagnostics;

namespace Lodestar.Text.Benchmarks.CrossLang;

/// <summary>
/// What renting a payload-sized buffer is worth against allocating one, in time.
/// </summary>
/// <remarks>
/// ADR 0053 refused pooling on two memory columns and never filled in this one. The primitive
/// is measured rather than the load path, because it isolates exactly what pooling changes and
/// bounds the win: nothing downstream can return more than the allocation itself costs. Both
/// states touch every page, so neither is measuring a buffer it never wrote to. Issue #470.
/// </remarks>
internal static class PoolCostBench
{
    /// <summary>The artifact an index of the benchmark corpus writes, to the byte.</summary>
    private const int PayloadBytes = 20_589_007;

    /// <summary>Timed runs per row. Odd, so the median is a run rather than a mean of two.</summary>
    private const int Repeats = 15;

    /// <summary>Untimed runs first, to settle the JIT and to let the pool reach its size.</summary>
    private const int WarmupRuns = 3;

    public static void Run()
    {
        byte[] rented = ArrayPool<byte>.Shared.Rent(PayloadBytes);
        int bucket = rented.Length;
        ArrayPool<byte>.Shared.Return(rented);

        (string Name, Action Work)[] rows =
        [
            ("allocate", Allocate),
            ("rent    ", RentAndReturn),
        ];

        double[][] samples = Interleave(rows);

        string report =
            $"payload         {PayloadBytes,12:N0} bytes{Environment.NewLine}" +
            $"pool bucket     {bucket,12:N0} bytes  ({bucket / (double)PayloadBytes:F2}x the ask)" +
            Environment.NewLine + Environment.NewLine +
            string.Join(
                Environment.NewLine,
                rows.Select((row, i) =>
                    $"{row.Name}        median {Median(samples[i]),7:F3}  min {samples[i].Min(),7:F3}  max {samples[i].Max(),7:F3} ms")) +
            Environment.NewLine +
            $"{Environment.NewLine}rent is {Median(samples[0]) / Median(samples[1]):F2}x the allocation, " +
            $"saving {Median(samples[0]) - Median(samples[1]):F3} ms per load";

        // console-print: this subcommand's whole output, and one call so one marker covers it.
        Console.WriteLine(report);
    }

    /// <summary>Allocate and touch, which is what a load does with its payload.</summary>
    /// <remarks>
    /// Uninitialized, because that is what <c>Buffers.AllocateUninitialized</c> gives the real
    /// read path: comparing a zeroed <c>new byte[]</c> against a rent would charge the pool's
    /// rival for work the code does not do, and inflate the saving by the whole memset.
    /// </remarks>
    private static void Allocate()
    {
        byte[] buffer = GC.AllocateUninitializedArray<byte>(PayloadBytes);
        Touch(buffer, PayloadBytes);
    }

    /// <summary>Rent, touch the same count, return.</summary>
    private static void RentAndReturn()
    {
        byte[] buffer = ArrayPool<byte>.Shared.Rent(PayloadBytes);
        Touch(buffer, PayloadBytes);
        ArrayPool<byte>.Shared.Return(buffer);
    }

    /// <summary>Writes one byte per page, which is what commits them.</summary>
    private static void Touch(byte[] buffer, int count)
    {
        for (int offset = 0; offset < count; offset += 4096)
        {
            buffer[offset] = 1;
        }
    }

    private static double[][] Interleave((string Name, Action Work)[] rows)
    {
        foreach ((_, Action work) in rows)
        {
            for (int warmup = 0; warmup < WarmupRuns; warmup++)
            {
                work();
            }
        }

        double[][] samples = [.. rows.Select(_ => new double[Repeats])];
        for (int run = 0; run < Repeats; run++)
        {
            for (int row = 0; row < rows.Length; row++)
            {
                long start = Stopwatch.GetTimestamp();
                rows[row].Work();
                samples[row][run] = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
            }
        }

        return samples;
    }

    private static double Median(double[] samples)
    {
        var sorted = (double[])samples.Clone();
        Array.Sort(sorted);
        return sorted[sorted.Length / 2];
    }
}
