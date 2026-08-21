using System.Text.Json;
using System.Text.Json.Serialization;
using Lodestar.Metrics;

namespace Lodestar.Text.Benchmarks.CrossLang;

/// <summary>
/// Cross-language throughput harness for the #61 metrics work, mirroring
/// <c>bench/python/bench_metrics.py</c>: same corpus files
/// (<c>bench/corpus/metrics/</c>), same operations in the same order, same
/// auto-scaling best-of-N methodology via <see cref="Harness"/>. Thirteen
/// operations from #61, #93 and #92; none takes <c>sample_weight</c>, so the
/// corpus's weight column is unused on both sides. <c>roc_auc_binary</c> and
/// <c>roc_auc_ovr_macro</c> only run on the matching two-/ten-class shapes, and
/// every name carries its shape (<c>_n{samples}_k{classes}</c>) to pair unambiguously.
/// </summary>
public static class MetricsCrossLang
{
    private static readonly (int Samples, int Classes)[] Shapes =
    [
        (1_000, 2), (1_000, 10), (100_000, 2), (100_000, 10), (1_000_000, 2), (1_000_000, 10),
    ];

    /// <summary>
    /// Runs the whole matrix, or the part of it named by <c>--only</c> and
    /// <c>--shapes</c>.
    /// </summary>
    /// <param name="args">
    /// Two optional comma-separated filters: <c>--only mse,median_ae</c> keeps
    /// operations whose name starts with one of the given prefixes; <c>--shapes
    /// 1000000x2</c> keeps only those corpus shapes. A filtered run writes the
    /// same file with fewer rows — a before/after instrument, not a comparison
    /// against Python, since the full matrix takes over eight minutes a campaign.
    /// </param>
    public static void Run(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        string[] operations = Filter(args, "--only");
        string[] shapes = Filter(args, "--shapes");

        string root = BenchCorpus.RepoRoot();
        string corpusDir = Path.Combine(root, "bench", "corpus", "metrics");
        string outPath = Path.Combine(root, "bench", "results", "csharp-metrics.json");

        var results = new List<Harness.OperationResult>();

        foreach ((int n, int k) in Shapes)
        {
            // Skipped before the corpus file is read: the largest is 40 MB of
            // JSON, more than the skipped measurement is worth deserializing.
            if (shapes.Length > 0 && Array.IndexOf(shapes, $"{n}x{k}") < 0)
            {
                continue;
            }

            results.AddRange(MeasureShape(corpusDir, n, k, operations));
        }

        var payload = new Harness.Output
        {
            Metadata = new Harness.OutputMetadata
            {
                Side = "csharp",
                Library = "Lodestar",
                Runtime = Environment.Version.ToString(),
                Os = Environment.OSVersion.ToString(),
                MinTimeS = Harness.MinTimeSeconds,
                Repeats = Harness.RepeatCount,
                Filtered = operations.Length == 0 && shapes.Length == 0
                    ? null
                    : $"only=[{string.Join(",", operations)}] shapes=[{string.Join(",", shapes)}]",
            },
            Results = results,
        };

        Harness.Write(outPath, payload);
    }

    /// <summary>Reads the comma-separated values of <paramref name="option"/>, or an empty array if it is absent.</summary>
    private static string[] Filter(string[] args, string option)
    {
        int at = Array.IndexOf(args, option);
        return at >= 0 && at + 1 < args.Length
            ? args[at + 1].Split(',', StringSplitOptions.RemoveEmptyEntries)
            : [];
    }

    private static List<Harness.OperationResult> MeasureShape(string corpusDir, int n, int k, string[] operations)
    {
        string path = Path.Combine(corpusDir, $"metrics_n{n}_k{k}.json");
        if (!File.Exists(path))
        {
            throw new InvalidOperationException(
                $"The benchmark corpus is missing '{path}'. Generate it first: python bench/corpus/generate_metrics.py");
        }

        CorpusFile file = JsonSerializer.Deserialize<CorpusFile>(File.ReadAllBytes(path))!;
        int[] yTrue = file.YTrue;
        int[] yPred = file.YPred;
        double[] yTrueReal = file.YTrueReal;
        double[] yPredReal = file.YPredReal;
        string suffix = $"n{n}_k{k}";

        var results = new List<Harness.OperationResult>();

        void Add(string name, Func<object> operation)
        {
            // A prefix, not the whole name: `--only median_ae` takes that
            // operation across every shape, `--only median_ae_n1000000` takes one.
            if (operations.Length == 0 || Array.Exists(operations, only => name.StartsWith(only, StringComparison.Ordinal)))
            {
                results.Add(Harness.Measure(name, operation));
            }
        }

        Add($"confusion_matrix_{suffix}", () => ConfusionMatrix.Compute(yTrue, yPred));
        Add($"accuracy_{suffix}", () => Accuracy.Score(yTrue, yPred));
        Add($"precision_recall_f1_macro_{suffix}", () => PrecisionRecallF1Macro(yTrue, yPred));
        Add($"classification_report_{suffix}", () =>
            ClassificationReport.Compute(yTrue, yPred, zeroDivision: ZeroDivision.Zero).ToText());

        if (k == 2 && file.BinaryScores is { } binaryScores)
        {
            Add($"roc_auc_binary_{suffix}", () => RocAuc.Score(yTrue, binaryScores));
        }

        if (k > 2 && file.Scores is { } scores)
        {
            // Flattened outside the lambda: timing the reshape would measure
            // corpus prep, not the metric, at the cost of a wasted copy if filtered out.
            double[] flat = Flatten(scores, k);
            Add($"roc_auc_ovr_macro_{suffix}", () => RocAuc.MultiClass(yTrue, flat, k));
        }

        Add($"balanced_accuracy_{suffix}", () => BalancedAccuracy.Score(yTrue, yPred));
        Add($"matthews_{suffix}", () => MatthewsCorrelation.Score(yTrue, yPred));
        Add($"cohen_kappa_{suffix}", () => CohenKappa.Score(yTrue, yPred));

        Add($"mse_{suffix}", () => MeanSquaredError.Score(yTrueReal, yPredReal));
        Add($"mae_{suffix}", () => MeanAbsoluteError.Score(yTrueReal, yPredReal));
        Add($"median_ae_{suffix}", () => MedianAbsoluteError.Score(yTrueReal, yPredReal));
        Add($"r2_{suffix}", () => R2.Score(yTrueReal, yPredReal));

        return results;
    }

    // Boxed as a tuple so Harness.Measure(Func<object>) — shared with every
    // other operation here and in PersistenceCrossLang — needs no second overload.
    private static object PrecisionRecallF1Macro(int[] yTrue, int[] yPred)
    {
        ConfusionMatrix cm = ConfusionMatrix.Compute(yTrue, yPred);
        double precision = Precision.Score(cm, Averaging.Macro);
        double recall = Recall.Score(cm, Averaging.Macro);
        double f1 = F1.Score(cm, Averaging.Macro);
        return (precision, recall, f1);
    }

    private static double[] Flatten(double[][] rows, int classCount)
    {
        double[] flat = new double[rows.Length * classCount];
        for (int i = 0; i < rows.Length; i++)
        {
            Array.Copy(rows[i], 0, flat, i * classCount, classCount);
        }
        return flat;
    }

    private sealed record CorpusFile
    {
        [JsonPropertyName("samples")] public int Samples { get; init; }
        [JsonPropertyName("classes")] public int Classes { get; init; }
        [JsonPropertyName("y_true")] public int[] YTrue { get; init; } = [];
        [JsonPropertyName("y_pred")] public int[] YPred { get; init; } = [];
        [JsonPropertyName("sample_weight")] public double[] SampleWeight { get; init; } = [];
        [JsonPropertyName("binary_scores")] public double[]? BinaryScores { get; init; }
        [JsonPropertyName("scores")] public double[][]? Scores { get; init; }
        [JsonPropertyName("y_true_real")] public double[] YTrueReal { get; init; } = [];
        [JsonPropertyName("y_pred_real")] public double[] YPredReal { get; init; } = [];
    }
}
