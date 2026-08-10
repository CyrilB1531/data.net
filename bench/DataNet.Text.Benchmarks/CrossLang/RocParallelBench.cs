using System.Diagnostics;
using DataNet.Metrics;

namespace DataNet.Text.Benchmarks.CrossLang;

/// <summary>
/// The before-and-after for issue #86: multiclass ROC-AUC at several worker
/// counts, wall and processor time from the same run.
/// </summary>
/// <remarks>
/// <para>
/// Not a <c>compare-*</c> mode. Those are the matched face-offs against Python;
/// this is C# against C#, the sequential path against itself with more threads,
/// so there is no Python side and no shared corpus file to keep in step.
/// </para>
/// <para>
/// The inputs are generated here from a fixed seed rather than read from
/// <c>bench/corpus/metrics/</c>, which holds k=2 and k=10 only and stops its
/// score matrix at 100 000 rows. For a C#-against-C# comparison the only property
/// that matters is the same data on both sides, and a seeded generator guarantees
/// that more firmly than a committed file — while leaving #61's published corpus
/// and its table untouched.
/// </para>
/// <para>
/// Processor time is expected to rise with the worker count. That is not the
/// measurement failing; elapsed time is what this issue is about, and the ratio
/// of the two is printed so the cost is visible rather than implied.
/// </para>
/// </remarks>
internal static class RocParallelBench
{
    private static readonly (int Samples, int Classes)[] Shapes =
    [
        (1_000, 10),      // the small-input path: dispatch must not eat it
        (100_000, 5),
        (100_000, 10),
    ];

    private static readonly int[] WorkerCounts = [1, 2, 4, 8];

    // One-vs-one at n=100 000, k=10 is 45 pairs and 90 curves — the heaviest cell
    // in the whole matrix. It is attempted rather than skipped by default; a
    // single cell running past this is named and skipped rather than left to
    // stall the rest of the run.
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
                        // Timed outside Harness.Measure's own auto-scaling repeats: a
                        // single call here already costs minutes, so patience is
                        // spent on one call rather than Measure's five best-of-N
                        // repeats, and a slow cell is named rather than left to stall
                        // every worker count and shape after it.
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
                Library = $"DataNet.Metrics ({Environment.ProcessorCount} logical cores)",
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
