using System.Diagnostics;

namespace Lodestar.Text.Benchmarks.CrossLang;

/// <summary>Timing several rows against each other, one round each rather than one row at a time.</summary>
/// <remarks>
/// Shared by the diagnostics that compare rows inside one process. Running a row to completion
/// lets a collection storm land inside whichever one is unlucky: <c>save-phases</c> did that once
/// and reported a strict subset at 136.7% of its parent. Interleaving spreads that across every
/// row instead of concentrating it in one, which is the whole reason these read as comparisons.
/// </remarks>
internal static class Rounds
{
    /// <summary>Runs each row once per round, returning one sample series per row.</summary>
    /// <param name="rows">The rows, in report order.</param>
    /// <param name="repeats">Timed rounds. Odd, so a median is a run rather than a mean of two.</param>
    /// <param name="warmups">Untimed runs of every row first, to settle the JIT and the allocator.</param>
    public static double[][] Interleave((string Name, Action Work)[] rows, int repeats, int warmups)
    {
        foreach ((_, Action work) in rows)
        {
            for (int warmup = 0; warmup < warmups; warmup++)
            {
                work();
            }
        }

        double[][] samples = [.. rows.Select(_ => new double[repeats])];
        for (int run = 0; run < repeats; run++)
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

    /// <summary>The median of a series, leaving the caller's array in the order it was filled.</summary>
    public static double Median(double[] samples)
    {
        var sorted = (double[])samples.Clone();
        Array.Sort(sorted);
        return sorted[sorted.Length / 2];
    }
}
