using System.Diagnostics;
using System.Numerics.Tensors;
using Lodestar.Embeddings.Search;

namespace Lodestar.Text.Benchmarks.CrossLang;

/// <summary>Our kNN kernel against <c>TensorPrimitives</c>, on the access pattern the index has.</summary>
/// <remarks>
/// V6 of issue #437, and the open risk it answers is "TensorPrimitives makes the kNN redundant".
/// The index normalizes on insertion, so <c>Search</c> is a dot product over a contiguous block
/// rather than a cosine: comparing our dot against <c>CosineSimilarity</c> would charge the BCL
/// for two norms we never compute. Both shapes are measured for that reason. Every row calls the
/// shipped <see cref="VectorMath.Dot"/> rather than a copy of it, because a kernel reproduced for
/// a benchmark is a kernel nobody shipped.
/// </remarks>
internal static class TensorPrimitivesBench
{
    /// <summary>How many timed runs each row gets. Odd, so the median is a run rather than a mean of two.</summary>
    private const int Repeats = 9;

    /// <summary>Untimed runs before the first timed one, to settle the JIT.</summary>
    private const int WarmupRuns = 3;

    public static void Run()
    {
        EmbeddingIndex index = PersistenceBenchmarks.BuildIndex();
        float[] raw = PersistenceBenchmarks.BuildBlock();
        int dimension = index.Dimension;
        int count = raw.Length / dimension;

        // The index stores normalized rows, so a like-for-like dot needs a normalized corpus.
        // Built here rather than read off the index, which does not hand its store out.
        float[] unit = Normalized(raw, dimension);
        float[] query = raw[..dimension];
        float[] unitQuery = Normalized(query, dimension);
        var scores = new float[count];

        // console-print: the shape every row below sweeps, and the size the ratios belong to.
        Console.WriteLine($"corpus         {count:N0} x {dimension} = {raw.Length:N0} floats");
        // console-print: the width of the SIMD path, without which our side is not interpretable.
        Console.WriteLine(
            $"Vector<float>  {System.Numerics.Vector<float>.Count} wide, "
            + $"hardware accelerated = {System.Numerics.Vector.IsHardwareAccelerated}");
        // console-print: the machine, without which no absolute here is readable.
        Console.WriteLine($"runtime        {Environment.Version}, {Environment.ProcessorCount} cores");
        Console.WriteLine(); // console-print: separates the conditions from the agreement.

        // Agreement first, and before any timing: a speed comparison between two routes that
        // disagree is meaningless, so it is a precondition here rather than a footnote.

        // console-print: the worst absolute difference on each shape, over every row.
        Console.WriteLine($"agreement dot      {WorstDotDifference(unit, unitQuery, dimension, count):E3}");
        // console-print: the cosine shape, where the BCL computes norms we do not.
        Console.WriteLine($"agreement cosine   {WorstCosineDifference(raw, query, dimension, count):E3}");
        Console.WriteLine(); // console-print: separates the agreement from the table.

        var rows = new List<(string Name, Func<long> Action)>
        {
            ("ours_dot_knn", () => OursDot(unit, unitQuery, dimension, count, scores)),
            ("tp_dot_knn", () => TensorDot(unit, unitQuery, dimension, count, scores)),
            ("ours_cosine_knn", () => OursCosine(raw, query, dimension, count, scores)),
            ("tp_cosine_knn", () => TensorCosine(raw, query, dimension, count, scores)),
            // TensorPrimitives is built for long spans, and the four rows above call it once per
            // row of 384. One call over the whole block shows what its design is actually for.
            ("ours_one_sweep", () => (long)(VectorMath.Dot(unit, unit) * 0) + unit.Length),
            ("tp_one_sweep", () => (long)(TensorPrimitives.Dot<float>(unit, unit) * 0) + unit.Length),
            // The row that decides the risk: the shipped method, not a kernel in isolation.
            ("index_search", () => index.Search(unitQuery, k: 10).Count),
        };

        Report(RunInterleaved(rows), raw.Length);
    }

    /// <summary>Our kernel over the corpus, one row at a time — the index's own access pattern.</summary>
    private static long OursDot(float[] block, float[] q, int dimension, int count, float[] into)
    {
        for (int item = 0; item < count; item++)
        {
            into[item] = VectorMath.Dot(block.AsSpan(item * dimension, dimension), q);
        }
        return count;
    }

    private static long TensorDot(float[] block, float[] q, int dimension, int count, float[] into)
    {
        for (int item = 0; item < count; item++)
        {
            into[item] = TensorPrimitives.Dot<float>(block.AsSpan(item * dimension, dimension), q);
        }
        return count;
    }

    /// <summary>What the index would cost without normalizing on insertion: norms every call.</summary>
    private static long OursCosine(float[] block, float[] q, int dimension, int count, float[] into)
    {
        float queryNorm = VectorMath.L2Norm(q);
        for (int item = 0; item < count; item++)
        {
            ReadOnlySpan<float> row = block.AsSpan(item * dimension, dimension);
            into[item] = VectorMath.Dot(row, q) / (VectorMath.L2Norm(row) * queryNorm);
        }
        return count;
    }

    private static long TensorCosine(float[] block, float[] q, int dimension, int count, float[] into)
    {
        for (int item = 0; item < count; item++)
        {
            into[item] = TensorPrimitives.CosineSimilarity<float>(
                block.AsSpan(item * dimension, dimension), q);
        }
        return count;
    }

    /// <summary>The worst absolute disagreement on the dot shape, over every row of the corpus.</summary>
    private static double WorstDotDifference(float[] block, float[] q, int dimension, int count)
    {
        double worst = 0;
        for (int item = 0; item < count; item++)
        {
            ReadOnlySpan<float> row = block.AsSpan(item * dimension, dimension);
            worst = Math.Max(worst, Math.Abs(VectorMath.Dot(row, q) - TensorPrimitives.Dot<float>(row, q)));
        }
        return worst;
    }

    /// <summary>The same, on the cosine shape, where the two compute the norms differently.</summary>
    private static double WorstCosineDifference(float[] block, float[] q, int dimension, int count)
    {
        double worst = 0;
        float queryNorm = VectorMath.L2Norm(q);
        for (int item = 0; item < count; item++)
        {
            ReadOnlySpan<float> row = block.AsSpan(item * dimension, dimension);
            float mine = VectorMath.Dot(row, q) / (VectorMath.L2Norm(row) * queryNorm);
            worst = Math.Max(worst, Math.Abs(mine - TensorPrimitives.CosineSimilarity<float>(row, q)));
        }
        return worst;
    }

    /// <summary>A copy of the corpus with every row scaled to unit length.</summary>
    private static float[] Normalized(float[] block, int dimension)
    {
        var copy = (float[])block.Clone();
        for (int offset = 0; offset < copy.Length; offset += dimension)
        {
            Span<float> row = copy.AsSpan(offset, dimension);
            // Positive rather than != 0: S1244 refuses the equality, and a zero row is left as
            // it is because scaling it is undefined rather than because zero is special here.
            float norm = VectorMath.L2Norm(row);
            if (norm > 0)
            {
                for (int i = 0; i < row.Length; i++)
                {
                    row[i] /= norm;
                }
            }
        }
        return copy;
    }

    /// <summary>Runs every row once per round, so a drift in the machine lands on all of them.</summary>
    private static List<Row> RunInterleaved(List<(string Name, Func<long> Action)> rows)
    {
        for (int warm = 0; warm < WarmupRuns; warm++)
        {
            for (int r = 0; r < rows.Count; r++)
            {
                GC.KeepAlive(rows[r].Action());
            }
        }

        var runs = new double[rows.Count][];
        for (int r = 0; r < rows.Count; r++)
        {
            runs[r] = new double[Repeats];
        }

        for (int round = 0; round < Repeats; round++)
        {
            for (int r = 0; r < rows.Count; r++)
            {
                long start = Stopwatch.GetTimestamp();
                long result = rows[r].Action();
                runs[r][round] = (double)(Stopwatch.GetTimestamp() - start) / Stopwatch.Frequency * 1e3;
                if (result <= 0)
                {
                    throw new InvalidOperationException($"Row '{rows[r].Name}' did not complete.");
                }
            }
        }

        var measured = new List<Row>(rows.Count);
        for (int r = 0; r < rows.Count; r++)
        {
            double[] sorted = (double[])runs[r].Clone();
            Array.Sort(sorted);
            measured.Add(new Row(rows[r].Name, sorted));
        }
        return measured;
    }

    private static void Report(List<Row> rows, int blockLength)
    {
        // console-print: this table is the mode's entire output; it writes no page.
        Console.WriteLine($"| {"row",-16} | {"median",9} | {"min",9} | {"max",9} | {"Mfloat/s",10} |");
        // console-print: the separator that makes the rows a table a reader can paste.
        Console.WriteLine(
            $"| {new string('-', 16)} | {new string('-', 9)} | {new string('-', 9)} | "
            + $"{new string('-', 9)} | {new string('-', 10)} |");

        foreach (Row row in rows)
        {
            double rate = blockLength / (row.Median / 1e3) / 1e6;
            // console-print: one measured row; these are the measurement itself.
            Console.WriteLine(
                $"| {row.Name,-16} | {row.Median,9:F3} | {row.Min,9:F3} | {row.Max,9:F3} | {rate,10:F1} |");
        }

        Console.WriteLine(); // console-print: separates the table from the ratios it supports.
        foreach ((string ours, string theirs) in
                 new[] { ("ours_dot_knn", "tp_dot_knn"), ("ours_cosine_knn", "tp_cosine_knn"), ("ours_one_sweep", "tp_one_sweep") })
        {
            double a = rows.First(r => r.Name == ours).Median;
            double b = rows.First(r => r.Name == theirs).Median;
            // console-print: the ratio V6 asks for, with its direction stated on every line.
            Console.WriteLine($"{ours} / {theirs} = {a / b:F2}x   (above 1 means TensorPrimitives is faster)");
        }

        Console.WriteLine(); // console-print: separates the ratios from what bounds them.
        // console-print: the units, which the guide's protocol asks a table to carry.
        Console.WriteLine(
            $"median ms over {Repeats} runs; Mfloat/s counts the whole block per call, so the "
            + "one_sweep rows and the knn rows sweep the same floats by different routes.");
    }

    private sealed record Row(string Name, double[] Runs)
    {
        public double Median => Runs[Runs.Length / 2];

        public double Min => Runs[0];

        public double Max => Runs[Runs.Length - 1];
    }
}
