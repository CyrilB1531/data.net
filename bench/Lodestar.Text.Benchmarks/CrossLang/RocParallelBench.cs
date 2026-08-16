using System.Diagnostics;
using Lodestar.Metrics;

namespace Lodestar.Text.Benchmarks.CrossLang;

// SonarLint S2245: a seeded Random builds a reproducible benchmark corpus; no security use.
#pragma warning disable S2245, CA5394

/// <summary>
/// The before-and-after for issue #86: multiclass ROC-AUC at several worker
/// counts, wall and processor time from the same run. Not a <c>compare-*</c>
/// mode — this is C# against itself with more threads, so there is no Python
/// side to keep in step. Inputs are generated here from a fixed seed rather
/// than read from <c>bench/corpus/metrics/</c> (which stops at 100 000 rows and
/// only holds k=2/k=10): same data on both sides is all a C#-vs-C# comparison
/// needs, and a seeded generator guarantees that without touching #61's
/// published corpus. Processor time rising with worker count is expected, not a failure.
/// </summary>
internal static class RocParallelBench
{
    private static readonly (int Samples, int Classes)[] Shapes =
    [
        (1_000, 10),      // the small-input path: dispatch must not eat it
        (100_000, 5),
        (100_000, 10),
    ];

    private static readonly int[] WorkerCounts = [1, 2, 4, 8];

    // The heaviest committed shape measures about 127 ms, per docs/guides/performance.md.
    // This guards a future, quadratic-in-k Shapes entry that runs long instead.
    private static readonly TimeSpan OneVsOnePatience = TimeSpan.FromSeconds(60);

    public static void Run()
    {
        string outPath = Path.Combine(BenchCorpus.RepoRoot(), "bench", "results", "csharp-roc-parallel.json");

        Console.WriteLine($"C# multiclass ROC-AUC, sequential vs parallel — {Environment.ProcessorCount} logical cores");
        var results = new List<Harness.OperationResult>();

        foreach ((int n, int k) in Shapes)
        {
            (int[] yTrue, double[] scores) = Generate(n, k);

            foreach (MultiClassStrategy strategy in new[] { MultiClassStrategy.OneVsRest, MultiClassStrategy.OneVsOne })
            {
                bool heaviest = strategy == MultiClassStrategy.OneVsOne && n >= 100_000 && k == 10;

                foreach (int workers in WorkerCounts)
                {
                    string name = $"{(strategy == MultiClassStrategy.OneVsRest ? "ovr" : "ovo")}_n{n}_k{k}_dop{workers}";

                    if (heaviest)
                    {
                        // Timed outside Harness.Measure's own repeats, so the patience
                        // budget lands on one call; harmless today, and warms the path.
                        var single = Stopwatch.StartNew();
                        double auc = RocAuc.MultiClass(yTrue, scores, k, new MultiClassRocOptions
                        {
                            Strategy = strategy,
                            Average = Averaging.Macro,
                            MaxDegreeOfParallelism = workers,
                        });
                        single.Stop();
                        GC.KeepAlive(auc);

                        if (single.Elapsed > OneVsOnePatience)
                        {
                            Console.WriteLine(
                                $"  {name} skipped: one call already took {single.Elapsed.TotalSeconds:F1}s, " +
                                $"over the {OneVsOnePatience.TotalSeconds:F0}s patience for this cell");
                            continue;
                        }
                    }

                    results.Add(Harness.Measure(name, () => RocAuc.MultiClass(yTrue, scores, k, new MultiClassRocOptions
                    {
                        Strategy = strategy,
                        Average = Averaging.Macro,
                        MaxDegreeOfParallelism = workers,
                    })));
                }
            }
        }

        Harness.Write(outPath, new Harness.Output
        {
            Metadata = new Harness.OutputMetadata
            {
                Side = "csharp",
                Library = $"Lodestar.Metrics ({Environment.ProcessorCount} logical cores)",
                Runtime = Environment.Version.ToString(),
                Os = Environment.OSVersion.ToString(),
                MinTimeS = Harness.MinTimeSeconds,
                Repeats = Harness.RepeatCount,
            },
            Results = results,
        });
    }

    /// <summary>
    /// A separable multiclass problem: the true class gets a bonus draw, then the
    /// row is normalised, because <c>MultiClass</c> requires rows summing to 1.
    /// Seeded, so two runs of this harness score the same numbers.
    /// </summary>
    private static (int[] YTrue, double[] Scores) Generate(int n, int k)
    {
        int[] yTrue = new int[n];
        double[] scores = new double[n * k];
        var random = new Random(86);

        for (int i = 0; i < n; i++)
        {
            yTrue[i] = i % k;
            int row = i * k;
            double total = 0.0;
            for (int c = 0; c < k; c++)
            {
                double draw = random.NextDouble() + (c == yTrue[i] ? 0.75 : 0.0);
                scores[row + c] = draw;
                total += draw;
            }
            for (int c = 0; c < k; c++)
            {
                scores[row + c] /= total;
            }
        }

        return (yTrue, scores);
    }
}
