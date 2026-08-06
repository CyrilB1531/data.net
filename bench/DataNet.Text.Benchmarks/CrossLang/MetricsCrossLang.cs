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
/// Six operations are named in the design brief: <c>confusion_matrix</c>,
/// <c>accuracy</c>, <c>precision_recall_f1_macro</c>, <c>classification_report</c>,
/// <c>roc_auc_binary</c> and <c>roc_auc_ovr_macro</c>. None takes
/// <c>sample_weight</c> — the Python calls this mirrors do not either — so the
/// weight column the corpus carries is unused here, on both sides.
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
/// </remarks>
public static class MetricsCrossLang
{
    private static readonly (int Samples, int Classes)[] Shapes =
    [
        (1_000, 2), (1_000, 10), (100_000, 2), (100_000, 10), (1_000_000, 2), (1_000_000, 10),
    ];

    public static void Run()
    {
        string root = BenchCorpus.RepoRoot();
        string corpusDir = Path.Combine(root, "bench", "corpus", "metrics");
        string outPath = Path.Combine(root, "bench", "results", "csharp-metrics.json");

        Console.WriteLine("C# metrics cross-lang bench");
        var results = new List<Harness.OperationResult>();

        foreach ((int n, int k) in Shapes)
        {
            results.AddRange(MeasureShape(corpusDir, n, k));
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
            },
            Results = results,
        };

        Harness.Write(outPath, payload);
    }

    private static List<Harness.OperationResult> MeasureShape(string corpusDir, int n, int k)
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
        string suffix = $"n{n}_k{k}";

        var results = new List<Harness.OperationResult>
        {
            Harness.Measure($"confusion_matrix_{suffix}", () => ConfusionMatrix.Compute(yTrue, yPred)),
            Harness.Measure($"accuracy_{suffix}", () => Accuracy.Score(yTrue, yPred)),
            Harness.Measure($"precision_recall_f1_macro_{suffix}", () => PrecisionRecallF1Macro(yTrue, yPred)),
            Harness.Measure($"classification_report_{suffix}", () =>
                ClassificationReport.Compute(yTrue, yPred, zeroDivision: ZeroDivision.Zero).ToText()),
        };

        if (k == 2 && file.BinaryScores is { } binaryScores)
        {
            results.Add(Harness.Measure($"roc_auc_binary_{suffix}", () => RocAuc.Score(yTrue, binaryScores)));
        }

        if (k > 2 && file.Scores is { } scores)
        {
            double[] flat = Flatten(scores, k);
            results.Add(Harness.Measure($"roc_auc_ovr_macro_{suffix}", () => RocAuc.MultiClass(yTrue, flat, k)));
        }

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
    }
}
