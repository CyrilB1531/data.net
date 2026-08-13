using System.Text.Json;
using System.Text.Json.Serialization;
using DataNet.Metrics;

namespace DataNet.Text.Benchmarks.CrossLang;

/// <summary>
/// Cross-language throughput harness for the #61 metrics work, mirroring
/// <c>bench/python/bench_metrics.py</c> exactly: same corpus files
/// (<c>bench/corpus/metrics/</c>, from <c>generate_metrics.py</c>), same
/// operations in the same order, same auto-scaling best-of-N methodology via
/// the shared <see cref="Harness"/>.
/// </summary>
/// <remarks>
/// <para>
/// Thirteen operations are named in total: the six from issue #61 —
/// <c>confusion_matrix</c>, <c>accuracy</c>, <c>precision_recall_f1_macro</c>,
/// <c>classification_report</c>, <c>roc_auc_binary</c> and
/// <c>roc_auc_ovr_macro</c> — plus <c>balanced_accuracy</c>, <c>matthews</c> and
/// <c>cohen_kappa</c> from issue #93, plus <c>mse</c>, <c>mae</c>,
/// <c>median_ae</c> and <c>r2</c> from issue #92, which (unlike the two ROC-AUC
/// rows) run over every shape. None takes <c>sample_weight</c> — the Python
/// calls this mirrors do not either — so the weight column the corpus carries
/// is unused here, on both sides.
/// </para>
/// <para>
/// <c>roc_auc_binary</c> only runs over the two-class files, and
/// <c>roc_auc_ovr_macro</c> only over the ten-class files whose <c>scores</c>
/// matrix the generator actually wrote (it stops at 100 000 rows — see the
/// generator's own comment on why). Every operation name carries its shape
/// (<c>_n{samples}_k{classes}</c>) so the two sides can be paired unambiguously
/// without a second field.
/// </para>
/// <para>
/// <c>precision_recall_f1_macro</c> matches scikit-learn's single
/// <c>precision_recall_fscore_support</c> call, which builds the confusion
/// matrix once and reads three averages off it. The DataNet side does the same:
/// one <see cref="ConfusionMatrix.Compute(ReadOnlySpan{int},ReadOnlySpan{int},ReadOnlySpan{int},ReadOnlySpan{double})"/>
/// call, then <see cref="Precision"/>, <see cref="Recall"/> and <see cref="F1"/>
/// each read that same matrix rather than recomputing it.
/// </para>
/// <para>
/// <c>mse</c>, <c>mae</c>, <c>median_ae</c> and <c>r2</c> cover the four
/// distinct cost shapes among the eleven regression metrics landed for issue
/// #92 — a squared mean, an absolute mean, a sort, and a two-pass centred sum —
/// so the other seven are one of those four with a different arithmetic kernel
/// and are not separately timed. They run over <c>y_true_real</c>/
/// <c>y_pred_real</c>, continuous targets the generator draws from a
/// separate seeded random instance and attaches to each shape's corpus file,
/// independent of the classification columns above.
/// </para>
/// </remarks>
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
    /// The process arguments. Two optional filters, both comma-separated:
    /// <c>--only mse,median_ae</c> keeps the operations whose name starts with
    /// one of the given prefixes, and <c>--shapes 1000000x2</c> keeps the corpus
    /// files of those <c>samples</c>×<c>classes</c> shapes. A filtered run writes
    /// the same file in the same format, holding fewer rows — which is what makes
    /// it a before/after instrument rather than a comparison against Python: the
    /// full matrix takes over eight minutes a campaign on a four-core desktop, and
    /// an interleaved before/after needs four of them. Ask for the operation under
    /// test and a control, and it takes about one.
    /// </param>
    public static void Run(string[] args)
    {
        ArgumentNullException.ThrowIfNull(args);

        string[] operations = Filter(args, "--only");
        string[] shapes = Filter(args, "--shapes");

        string root = BenchCorpus.RepoRoot();
        string corpusDir = Path.Combine(root, "bench", "corpus", "metrics");
        string outPath = Path.Combine(root, "bench", "results", "csharp-metrics.json");

        Console.WriteLine("C# metrics cross-lang bench");
        if (operations.Length > 0 || shapes.Length > 0)
        {
            Console.WriteLine($"  filtered: operations=[{string.Join(",", operations)}] shapes=[{string.Join(",", shapes)}]");
        }

        var results = new List<Harness.OperationResult>();

        foreach ((int n, int k) in Shapes)
        {
            // Skipped before the corpus file is read, not after: the largest is
            // 40 MB of JSON, and deserializing a shape nobody asked for would
            // cost more than the measurement it is not making.
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
                Library = "DataNet",
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
            // Flattened outside the lambda, as before: the reshape is corpus
            // preparation, and timing it would make this row measure the wrong
            // thing. It costs a copy when the filter drops the row, which is
            // cheaper than getting the one number this file exists to report wrong.
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

    // Boxed as a tuple so the shared Harness.Measure(Func<object>) signature — the
    // same one every other operation here and in PersistenceCrossLang uses — does
    // not need a second overload just for this one call that returns three
    // numbers instead of one.
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
