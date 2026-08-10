using System.Globalization;
using DataNet.Metrics;

namespace DataNet.Sample;

/// <summary>
/// Lot 5 — DataNet.Metrics, the scikit-learn-compatible evaluation surface.
/// </summary>
internal static class Lot5Metrics
{
    // Ten samples over three classes, deliberately imbalanced: class 2 carries
    // half the support and class 0 a fifth of it. That is what makes the three
    // averages below disagree — on a balanced set they would print the same
    // number three times and the reader would learn nothing.
    private static readonly int[] YTrue = [0, 1, 2, 2, 1, 0, 1, 2, 2, 2];
    private static readonly int[] YPred = [0, 2, 2, 1, 1, 0, 1, 1, 2, 2];
    private static readonly string[] TargetNames = ["setosa", "versicolor", "virginica"];

    // Averaging.Binary is not an average — it reports one class against the
    // rest, and the library refuses it on a matrix with more than two classes.
    // It therefore needs a target of its own.
    private static readonly int[] SpamTruth = [0, 1, 1, 0, 1, 1, 0, 1];
    private static readonly int[] SpamPredicted = [0, 1, 0, 0, 1, 1, 1, 1];

    // A fourth label nothing was ever predicted into, which is the only way to
    // see ZeroDivision do anything at all.
    private static readonly int[] WithAbsentClass = [0, 1, 2, 3];

    public static void Run()
    {
        Console.WriteLine("lot 5 — classification metrics");

        ConfusionMatrix cm = ConfusionMatrix.Compute(YTrue, YPred);
        double[,] cells = cm.ToArray();
        Console.WriteLine($"  labels                = [{string.Join(", ", cm.Labels)}], total weight {cm.TotalWeight:F0}");
        for (int row = 0; row < cm.Labels.Count; row++)
        {
            Console.WriteLine($"    row {cm.Labels[row]}               = "
                + string.Join(" ", Enumerable.Range(0, cm.Labels.Count).Select(col => $"{cells[row, col]:F0}")));
        }

        // [0,0] read through the indexer, to show the matrix answers without a copy.
        Console.WriteLine($"  cm[0,0]               = {cm[0, 0]:F0}");
        Console.WriteLine($"  Accuracy              = {Accuracy.Score(cm):F3} normalized, "
            + $"{Accuracy.Score(cm, normalize: false):F0} correct");
        Console.WriteLine();

        AveragesDisagree(cm);
        PerClass(cm);
        Beta(cm);
        ZeroDivisionModes();
        Weighted();
        Report(cm);
        Roc();
        MatrixReaders();
    }

    /// <summary>The three multiclass averages, on one matrix, printed together.</summary>
    private static void AveragesDisagree(ConfusionMatrix cm)
    {
        Console.WriteLine("  precision / recall / F1, by averaging mode");
        foreach (Averaging average in new[] { Averaging.Micro, Averaging.Macro, Averaging.Weighted })
        {
            Console.WriteLine($"    {average,-8}            = "
                + $"{Precision.Score(cm, average):F3} / "
                + $"{Recall.Score(cm, average):F3} / "
                + $"{F1.Score(cm, average):F3}");
        }

        // Averaging.Binary is the default, and it is not an average: it scores
        // posLabel alone. On the three-class matrix above it throws rather than
        // guess, so it gets a two-class target of its own.
        Console.WriteLine($"    Binary, posLabel=1  = "
            + $"{Precision.Score(SpamTruth, SpamPredicted, Averaging.Binary, posLabel: 1):F3} / "
            + $"{Recall.Score(SpamTruth, SpamPredicted, Averaging.Binary, posLabel: 1):F3} / "
            + $"{F1.Score(SpamTruth, SpamPredicted, Averaging.Binary, posLabel: 1):F3} (spam/not-spam)");
        Console.WriteLine();
    }

    /// <summary>The unreduced per-class vectors the averages are computed from.</summary>
    private static void PerClass(ConfusionMatrix cm)
    {
        Console.WriteLine($"  Precision.PerClass    = {Format(Precision.PerClass(cm))}");
        Console.WriteLine($"  Recall.PerClass       = {Format(Recall.PerClass(cm))}");
        Console.WriteLine($"  F1.PerClass           = {Format(F1.PerClass(cm))}");
        Console.WriteLine();
    }

    /// <summary>F-beta either side of 1, where beta weights recall against precision.</summary>
    private static void Beta(ConfusionMatrix cm)
    {
        Console.WriteLine($"  FBeta β=0.5 (macro)   = {FBeta.Score(cm, beta: 0.5, Averaging.Macro):F3} — leans on precision");
        Console.WriteLine($"  FBeta β=2   (macro)   = {FBeta.Score(cm, beta: 2.0, Averaging.Macro):F3} — leans on recall");
        Console.WriteLine($"  FBeta.PerClass β=2    = {Format(FBeta.PerClass(cm, beta: 2.0))}");
        Console.WriteLine();
    }

    /// <summary>
    /// All four <see cref="ZeroDivision"/> values on a label nothing predicts,
    /// including the one that throws.
    /// </summary>
    private static void ZeroDivisionModes()
    {
        ConfusionMatrix cm = ConfusionMatrix.Compute(YTrue, YPred, WithAbsentClass);
        Console.WriteLine($"  label 3 occurs in neither column; precision for it is 0/0:");
        Console.WriteLine($"    ZeroDivision.Zero   = {Format(Precision.PerClass(cm, ZeroDivision.Zero))}");
        Console.WriteLine($"    ZeroDivision.One    = {Format(Precision.PerClass(cm, ZeroDivision.One))}");
        Console.WriteLine($"    ZeroDivision.NaN    = {Format(Precision.PerClass(cm, ZeroDivision.NaN))}");

        try
        {
            Precision.PerClass(cm, ZeroDivision.Throw);
            Console.WriteLine("    ZeroDivision.Throw  = <did not throw, which is a bug>");
        }
        catch (UndefinedMetricException ex)
        {
            Console.WriteLine($"    ZeroDivision.Throw  = {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>The same three numbers with a weight per sample.</summary>
    private static void Weighted()
    {
        // The five samples of class 2 count double, which moves the weighted
        // average and the support column with it.
        double[] weights = [1, 1, 2, 2, 1, 1, 1, 2, 2, 2];
        ConfusionMatrix weighted = ConfusionMatrix.Compute(YTrue, YPred, labels: default, sampleWeight: weights);
        Console.WriteLine($"  weighted total        = {weighted.TotalWeight:F0} (unweighted {YTrue.Length})");
        Console.WriteLine($"  weighted F1 (macro)   = {F1.Score(weighted, Averaging.Macro):F3} "
            + $"(unweighted {F1.Score(ConfusionMatrix.Compute(YTrue, YPred), Averaging.Macro):F3})");
        Console.WriteLine($"  weighted accuracy     = {Accuracy.Score(YTrue, YPred, sampleWeight: weights):F3}");
        Console.WriteLine();
    }

    /// <summary>The report as rows a program can read, then as the text sklearn prints.</summary>
    private static void Report(ConfusionMatrix cm)
    {
        ClassificationReport report = ClassificationReport.Compute(cm, TargetNames);

        Console.WriteLine("  ClassificationReport, structured");
        foreach (ClassRow row in report.Classes)
        {
            Console.WriteLine($"    {row.Name,-12} ({row.Label}) = "
                + $"{row.Precision:F3} / {row.Recall:F3} / {row.F1:F3} on {row.Support:F0} samples");
        }

        AverageRow macro = report.MacroAverage;
        AverageRow weighted = report.WeightedAverage;
        Console.WriteLine($"    {macro.Name,-18} = {macro.Precision:F3} / {macro.Recall:F3} / {macro.F1:F3} on {macro.Support:F0}");
        Console.WriteLine($"    {weighted.Name,-18} = {weighted.Precision:F3} / {weighted.Recall:F3} / {weighted.F1:F3}");

        // Present only when the report is not over the full label set, exactly
        // as scikit-learn prints "micro avg" in place of "accuracy".
        Console.WriteLine($"    micro avg          = {report.MicroAverage?.F1.ToString("F3", CultureInfo.InvariantCulture) ?? "<absent: every label is covered>"}");
        Console.WriteLine($"    accuracy           = {report.Accuracy:F3} on {report.TotalSupport:F0} samples");
        Console.WriteLine();

        Console.WriteLine("  ClassificationReport.ToText(), character for character what sklearn prints");
        foreach (string line in report.ToText().Split('\n'))
        {
            Console.WriteLine($"    |{line}");
        }

        Console.WriteLine();
    }

    /// <summary>ROC-AUC binary, then one-vs-rest and one-vs-one over three classes.</summary>
    private static void Roc()
    {
        int[] binaryTruth = [0, 0, 1, 1, 1, 0];
        double[] scores = [0.10, 0.40, 0.35, 0.80, 0.70, 0.20];
        Console.WriteLine($"  RocAuc.Score (binary) = {RocAuc.Score(binaryTruth, scores):F3}");

        // Row-major probabilities: sample 0's three classes, then sample 1's.
        // Each row sums to 1, which is what roc_auc_score demands of a
        // multiclass score matrix.
        int[] truth = [0, 1, 2, 2, 1, 0];
        double[] probabilities =
        [
            0.70, 0.20, 0.10,
            0.10, 0.60, 0.30,
            0.15, 0.25, 0.60,
            0.20, 0.20, 0.60,
            0.30, 0.50, 0.20,
            0.55, 0.30, 0.15,
        ];

        Console.WriteLine($"  MultiClass ovr macro  = "
            + $"{RocAuc.MultiClass(truth, probabilities, classCount: 3):F3}");
        Console.WriteLine($"  MultiClass ovr weight = "
            + $"{RocAuc.MultiClass(truth, probabilities, classCount: 3, new MultiClassRocOptions { Average = Averaging.Weighted }):F3}");
        Console.WriteLine($"  MultiClass ovo macro  = "
            + $"{RocAuc.MultiClass(truth, probabilities, classCount: 3, new MultiClassRocOptions { Strategy = MultiClassStrategy.OneVsOne }):F3}");

        // Opt-in parallelism over the per-class loop. Six samples and three
        // classes is far too small to gain anything — the point here is that the
        // number does not move, which is the guarantee the knob carries.
        //
        // Environment.ProcessorCount is spelled out here because it is the honest
        // way to say "every logical thread", not because it is the right number:
        // it counts logical threads, and on a hyperthreaded machine asking for all
        // of them is measurably slower than asking for half on several shapes. Do
        // not copy this figure into production code without reading
        // docs/guides/performance.md, which measures both and says which shapes
        // lose.
        Console.WriteLine($"  MultiClass ovr macro  = "
            + $"{RocAuc.MultiClass(truth, probabilities, classCount: 3, new MultiClassRocOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }):F3}"
            + "  (parallel, same value)");
        Console.WriteLine();
    }

    /// <summary>The metrics that read a matrix rather than the labels.</summary>
    private static void MatrixReaders()
    {
        int[] truth = [0, 0, 1, 1, 2, 2, 2];
        int[] predicted = [0, 1, 1, 1, 2, 0, 2];
        ConfusionMatrix cm = ConfusionMatrix.Compute(truth, predicted);

        Console.WriteLine($"  BalancedAccuracy      = {BalancedAccuracy.Score(cm):F3}");
        Console.WriteLine($"  MatthewsCorrelation   = {MatthewsCorrelation.Score(cm):F3}");
        Console.WriteLine($"  CohenKappa            = {CohenKappa.Score(cm):F3}");
        Console.WriteLine($"  CohenKappa (linear)   = {CohenKappa.Score(cm, KappaWeighting.Linear):F3}");

        // normalize= is a projection: the matrix itself never becomes fractions,
        // so Accuracy.Score(cm) above still means what it says.
        double[,] rowNormalised = cm.ToArray(Normalization.True);
        Console.WriteLine($"  row-normalised [0,0]  = {rowNormalised[0, 0]:F3}");
        Console.WriteLine();
    }

    private static string Format(double[] values) =>
        "[" + string.Join(", ", values.Select(v => v.ToString("F3", CultureInfo.InvariantCulture))) + "]";
}
