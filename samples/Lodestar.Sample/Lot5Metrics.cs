using Lodestar.Metrics;

namespace Lodestar.Sample;

/// <summary>
/// Lot 5 — Lodestar.Metrics, the scikit-learn-compatible evaluation surface.
/// </summary>
internal static class Lot5Metrics
{
    // Ten samples, three classes, deliberately imbalanced (class 2 half the
    // support, class 0 a fifth): balanced data would print the same number three times.
    private static readonly int[] YTrue = [0, 1, 2, 2, 1, 0, 1, 2, 2, 2];
    private static readonly int[] YPred = [0, 2, 2, 1, 1, 0, 1, 1, 2, 2];
    private static readonly string[] TargetNames = ["setosa", "versicolor", "virginica"];

    // Averaging.Binary is not an average — it scores one class against the rest,
    // and the library refuses it above two classes — so it needs a target of its own.
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
        Console.WriteLine($"  labels                = [{string.Join(", ", cm.Labels)}], total weight {Inv.F0(cm.TotalWeight)}");
        for (int row = 0; row < cm.Labels.Count; row++)
        {
            Console.WriteLine($"    row {cm.Labels[row]}               = "
                + string.Join(" ", Enumerable.Range(0, cm.Labels.Count).Select(col => $"{Inv.F0(cells[row, col])}")));
        }

        // [0,0] read through the indexer, to show the matrix answers without a copy.
        Console.WriteLine($"  cm[0,0]               = {Inv.F0(cm[0, 0])}");
        Console.WriteLine($"  Accuracy              = {Inv.F3(Accuracy.Score(cm))} normalized, "
            + $"{Inv.F0(Accuracy.Score(cm, normalize: false))} correct");
        Console.WriteLine();

        AveragesDisagree(cm);
        PerClass(cm);
        Beta(cm);
        ZeroDivisionModes();
        Weighted();
        Report(cm);
        Roc();
        MatrixReaders();
        Clustering();
        Ranking();
    }

    /// <summary>The ordered-list ranking metrics, on the rows that tell tie handling apart.</summary>
    private static void Ranking()
    {
        Console.WriteLine("  ranking, one ordered list of four documents");

        double[] relevance = [3, 2, 1, 0];
        double[] ordered = [0.9, 0.5, 0.4, 0.1];
        double[] reversed = [0.1, 0.4, 0.5, 0.9];
        double[] tied = [0.5, 0.5, 0.5, 0.5];

        Console.WriteLine($"    Dcg (perfect order) = {Inv.F3(Dcg.Score(relevance, ordered, 4))}, "
            + $"base e {Inv.F3(Dcg.Score(relevance, ordered, 4, logBase: Math.E))}");
        // The reversed row scores 0.614, not 0 -- the logarithmic discount is shallow
        // enough that even the worst ordering collects most of the ideal gain.
        Console.WriteLine($"    Ndcg perfect / worst= {Inv.F3(Ndcg.Score(relevance, ordered, 4))} / "
            + $"{Inv.F3(Ndcg.Score(relevance, reversed, 4))}");

        // Cutting the list at two positions is what makes a bad ordering look bad.
        Console.WriteLine($"    Ndcg at k=2 (worst) = {Inv.F3(Ndcg.Score(relevance, reversed, 4, k: 2))}");

        // Every score equal: averaging over the permutations of the tie against
        // ranking them arbitrarily, a 30% gap on the same input.
        Console.WriteLine($"    all tied, averaged  = {Inv.F3(Ndcg.Score(relevance, tied, 4))}");
        Console.WriteLine($"    all tied, ignored   = {Inv.F3(Ndcg.Score(relevance, tied, 4, ignoreTies: true))}");

        int[] classes = [0, 1, 2, 2];
        double[] probabilities =
        [
            0.7, 0.2, 0.1,
            0.3, 0.5, 0.2,
            0.2, 0.3, 0.5,
            0.5, 0.3, 0.2,
        ];
        Console.WriteLine($"    TopKAccuracy k=2    = {Inv.F3(TopKAccuracy.Score(classes, probabilities, 3))} "
            + $"({Inv.F0(TopKAccuracy.Score(classes, probabilities, 3, normalize: false))} of {classes.Length} samples)");

        // Two queries: the first relevant document second, then first.
        double[] judged = [0, 1, 0, 0, 1, 0, 0, 0];
        double[] retrieved = [0.9, 0.5, 0.4, 0.1, 0.9, 0.5, 0.4, 0.1];
        Console.WriteLine($"    ReciprocalRank      = {Inv.F3(ReciprocalRank.Score(judged, retrieved, 4))} "
            + "(no reference implementation — decision 0036)");
        Console.WriteLine();

        WeightedRanking();
        LabelMatrix();
    }

    /// <summary>A sample weight over two queries, which is the only shape that shows one.</summary>
    private static void WeightedRanking()
    {
        Console.WriteLine("  ranking, weighted: two queries, one ranked well and one reversed");

        // A weight over a single query cancels -- it multiplies both halves of the
        // mean -- so showing one at all needs two rows that score differently.
        double[] relevance = [3, 2, 1, 0, 3, 2, 1, 0];
        double[] scores = [0.9, 0.5, 0.4, 0.1, 0.1, 0.4, 0.5, 0.9];
        double[] onGood = [3.0, 1.0];
        double[] onBad = [1.0, 3.0];

        Console.WriteLine($"    Ndcg unweighted     = {Inv.F3(Ndcg.Score(relevance, scores, 4))}");
        Console.WriteLine($"    Ndcg weight on good = {Inv.F3(Ndcg.Score(relevance, scores, 4, sampleWeight: onGood))}");
        Console.WriteLine($"    Ndcg weight on bad  = {Inv.F3(Ndcg.Score(relevance, scores, 4, sampleWeight: onBad))}");
        Console.WriteLine($"    Dcg  weight on good = {Inv.F3(Dcg.Score(relevance, scores, 4, sampleWeight: onGood))}");

        int[] classes = [0, 1, 2, 2];
        double[] probabilities =
        [
            0.7, 0.2, 0.1,
            0.3, 0.5, 0.2,
            0.2, 0.3, 0.5,
            0.5, 0.3, 0.2,
        ];

        // normalize: false sums the WEIGHTS of the hits rather than counting them,
        // so the weighted count is 7 where the unweighted one is 3.
        double[] heavyFirst = [5.0, 1.0, 1.0, 1.0];
        Console.WriteLine($"    TopK weighted       = {Inv.F3(TopKAccuracy.Score(classes, probabilities, 3, sampleWeight: heavyFirst))} "
            + $"({Inv.F0(TopKAccuracy.Score(classes, probabilities, 3, normalize: false, sampleWeight: heavyFirst))} of weight, not of samples)");
        Console.WriteLine();
    }

    /// <summary>The three label-matrix metrics, and the places the reference disagrees with itself.</summary>
    private static void LabelMatrix()
    {
        Console.WriteLine("  ranking, a label matrix of two samples over three labels");

        // One relevant label per sample: the first ranks second of three, the
        // second ranks last, which is what makes the three numbers differ.
        bool[] relevant = [true, false, false, false, false, true];
        double[] labelScores = [0.75, 0.5, 1.0, 1.0, 0.2, 0.1];

        Console.WriteLine($"    LabelRankingAvgPrec = {Inv.F3(LabelRankingAveragePrecision.Score(relevant, labelScores, 3))}");
        Console.WriteLine($"    CoverageError       = {Inv.F3(CoverageError.Score(relevant, labelScores, 3))}");
        Console.WriteLine($"    LabelRankingLoss    = {Inv.F3(LabelRankingLoss.Score(relevant, labelScores, 3))}");

        // A sample with nothing relevant covers 0 labels rather than all of them,
        // so the mean sits below the 1 a reader takes for the floor.
        bool[] sparse = [false, false, false, true, false, false];
        double[] ranked = [0.7, 0.2, 0.1, 0.7, 0.2, 0.1];
        Console.WriteLine($"    empty row, coverage = {Inv.F3(CoverageError.Score(sparse, ranked, 3))} (below 1)");

        // A tie is an error: two relevant labels and one irrelevant, all scored
        // the same, loses both pairs.
        Console.WriteLine($"    all tied, loss      = {Inv.F3(LabelRankingLoss.Score([true, true, false], [0.5, 0.5, 0.5], 3))}");

        // The single label column the other two refuse with "binary format is not
        // supported" -- scikit-learn's own inconsistency, reproduced.
        Console.WriteLine($"    one label column    = {Inv.F3(LabelRankingAveragePrecision.Score([true], [0.7], 1))} (the other two refuse it)");

        // The same three, weighted: the second sample counts for three of the four.
        double[] weights = [1.0, 3.0];
        Console.WriteLine($"    weighted LRAP       = {Inv.F3(LabelRankingAveragePrecision.Score(relevant, labelScores, 3, weights))}");
        Console.WriteLine($"    weighted coverage   = {Inv.F3(CoverageError.Score(relevant, labelScores, 3, weights))}");
        Console.WriteLine($"    weighted loss       = {Inv.F3(LabelRankingLoss.Score(relevant, labelScores, 3, weights))}");
        Console.WriteLine();
    }

    /// <summary>The five agreement metrics, on the case that tells them apart.</summary>
    private static void Clustering()
    {
        Console.WriteLine("  clustering agreement, against a reference partition");

        int[] reference = [0, 0, 0, 1, 1, 1];
        int[] split = [0, 0, 1, 2, 2, 2];
        int[] alone = [0, 1, 2, 3, 4, 5];

        Console.WriteLine($"    AdjustedRand        = {Inv.F3(AdjustedRand.Score(reference, split))}");
        Console.WriteLine($"    NormalizedMutualInfo= {Inv.F3(NormalizedMutualInformation.Score(reference, split))}");
        Console.WriteLine($"    Homogeneity         = {Inv.F3(Homogeneity.Score(reference, split))}");
        Console.WriteLine($"    Completeness        = {Inv.F3(Completeness.Score(reference, split))}");
        Console.WriteLine($"    VMeasure            = {Inv.F3(VMeasure.Score(reference, split))}");
        Console.WriteLine($"    FowlkesMallows      = {Inv.F3(FowlkesMallows.Score(reference, split))}");
        Console.WriteLine($"    AdjustedMutualInfo  = {Inv.F3(AdjustedMutualInformation.Score(reference, split))}");

        // One clustering per sample: perfectly homogeneous, and worth nothing --
        // which is the pair of numbers the two families exist to show together.
        Console.WriteLine($"    every sample alone  = {Inv.F3(Homogeneity.Score(reference, alone))} homogeneity, "
            + $"{Inv.F3(AdjustedRand.Score(reference, alone))} adjusted Rand, "
            + $"{Inv.F3(AdjustedMutualInformation.Score(reference, alone))} adjusted mutual information");

        // Silhouette needs no reference partition at all: it reads the samples.
        double[] features = [0.0, 0.0, 0.2, 0.1, 4.0, 4.0, 4.2, 3.9, 0.1, 0.3];
        int[] guessed = [0, 0, 1, 1, 1];
        Console.WriteLine($"    Silhouette          = {Inv.F3(Silhouette.Score(guessed, features, 2))}");
        Console.WriteLine($"    worst sample        = {Inv.F3(Silhouette.PerSample(guessed, features, 2).Min())}");

        // The same five points as a matrix: the two paths agree exactly. The reason to
        // take this one is a metric the features cannot express, as cityblock below is.
        double[] euclidean = SquareDistances(features, guessed.Length, cityblock: false);
        Console.WriteLine($"    from distances      = {Inv.F3(Silhouette.ScoreFromDistances(guessed, euclidean))} "
            + $"(worst {Inv.F3(Silhouette.PerSampleFromDistances(guessed, euclidean).Min())}, the same numbers)");

        double[] cityblock = SquareDistances(features, guessed.Length, cityblock: true);
        Console.WriteLine($"    cityblock instead   = {Inv.F3(Silhouette.ScoreFromDistances(guessed, cityblock))} "
            + "(a metric the feature overload cannot take)");
        Console.WriteLine();
    }

    /// <summary>The pairwise distances of two-dimensional points, row-major and square.</summary>
    private static double[] SquareDistances(double[] features, int samples, bool cityblock)
    {
        double[] distances = new double[samples * samples];
        for (int i = 0; i < samples; i++)
        {
            for (int j = 0; j < samples; j++)
            {
                double dx = Math.Abs(features[i * 2] - features[j * 2]);
                double dy = Math.Abs(features[(i * 2) + 1] - features[(j * 2) + 1]);
                distances[(i * samples) + j] = cityblock ? dx + dy : Math.Sqrt((dx * dx) + (dy * dy));
            }
        }

        return distances;
    }

    /// <summary>The three multiclass averages, on one matrix, printed together.</summary>
    private static void AveragesDisagree(ConfusionMatrix cm)
    {
        Console.WriteLine("  precision / recall / F1, by averaging mode");
        foreach (Averaging average in new[] { Averaging.Micro, Averaging.Macro, Averaging.Weighted })
        {
            Console.WriteLine($"    {average,-8}            = "
                + $"{Inv.F3(Precision.Score(cm, average))} / "
                + $"{Inv.F3(Recall.Score(cm, average))} / "
                + $"{Inv.F3(F1.Score(cm, average))}");
        }

        // SpamTruth above explains why Binary needs its own two-class target.
        // posLabel picks which class counts as positive, here the spam one.
        Console.WriteLine($"    Binary, posLabel=1  = "
            + $"{Inv.F3(Precision.Score(SpamTruth, SpamPredicted, Averaging.Binary, posLabel: 1))} / "
            + $"{Inv.F3(Recall.Score(SpamTruth, SpamPredicted, Averaging.Binary, posLabel: 1))} / "
            + $"{Inv.F3(F1.Score(SpamTruth, SpamPredicted, Averaging.Binary, posLabel: 1))} (spam/not-spam)");
        Console.WriteLine();
    }

    /// <summary>The unreduced per-class vectors the averages are computed from.</summary>
    private static void PerClass(ConfusionMatrix cm)
    {
        Console.WriteLine($"  Precision.PerClass    = {Inv.List(Precision.PerClass(cm))}");
        Console.WriteLine($"  Recall.PerClass       = {Inv.List(Recall.PerClass(cm))}");
        Console.WriteLine($"  F1.PerClass           = {Inv.List(F1.PerClass(cm))}");
        Console.WriteLine();
    }

    /// <summary>F-beta either side of 1, where beta weights recall against precision.</summary>
    private static void Beta(ConfusionMatrix cm)
    {
        Console.WriteLine($"  FBeta β=0.5 (macro)   = {Inv.F3(FBeta.Score(cm, beta: 0.5, Averaging.Macro))} — leans on precision");
        Console.WriteLine($"  FBeta β=2   (macro)   = {Inv.F3(FBeta.Score(cm, beta: 2.0, Averaging.Macro))} — leans on recall");
        Console.WriteLine($"  FBeta.PerClass β=2    = {Inv.List(FBeta.PerClass(cm, beta: 2.0))}");
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
        Console.WriteLine($"    ZeroDivision.Zero   = {Inv.List(Precision.PerClass(cm, ZeroDivision.Zero))}");
        Console.WriteLine($"    ZeroDivision.One    = {Inv.List(Precision.PerClass(cm, ZeroDivision.One))}");
        Console.WriteLine($"    ZeroDivision.NaN    = {Inv.List(Precision.PerClass(cm, ZeroDivision.NaN))}");

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
        Console.WriteLine($"  weighted total        = {Inv.F0(weighted.TotalWeight)} (unweighted {YTrue.Length})");
        Console.WriteLine($"  weighted F1 (macro)   = {Inv.F3(F1.Score(weighted, Averaging.Macro))} "
            + $"(unweighted {Inv.F3(F1.Score(ConfusionMatrix.Compute(YTrue, YPred), Averaging.Macro))})");
        Console.WriteLine($"  weighted accuracy     = {Inv.F3(Accuracy.Score(YTrue, YPred, sampleWeight: weights))}");
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
                + $"{Inv.F3(row.Precision)} / {Inv.F3(row.Recall)} / {Inv.F3(row.F1)} on {Inv.F0(row.Support)} samples");
        }

        AverageRow macro = report.MacroAverage;
        AverageRow weighted = report.WeightedAverage;
        Console.WriteLine($"    {macro.Name,-18} = {Inv.F3(macro.Precision)} / {Inv.F3(macro.Recall)} / {Inv.F3(macro.F1)} on {Inv.F0(macro.Support)}");
        Console.WriteLine($"    {weighted.Name,-18} = {Inv.F3(weighted.Precision)} / {Inv.F3(weighted.Recall)} / {Inv.F3(weighted.F1)}");

        // Present only when the report is not over the full label set, exactly
        // as scikit-learn prints "micro avg" in place of "accuracy".
        Console.WriteLine($"    micro avg          = {Inv.F3(report.MicroAverage?.F1) ?? "<absent: every label is covered>"}");
        Console.WriteLine($"    accuracy           = {Inv.F3(report.Accuracy)} on {Inv.F0(report.TotalSupport)} samples");
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
        Console.WriteLine($"  RocAuc.Score (binary) = {Inv.F3(RocAuc.Score(binaryTruth, scores))}");

        // Row-major: sample 0's three classes, then sample 1's — each row sums to
        // 1, which a multiclass score matrix must.
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
            + $"{Inv.F3(RocAuc.MultiClass(truth, probabilities, classCount: 3))}");
        Console.WriteLine($"  MultiClass ovr weight = "
            + $"{Inv.F3(RocAuc.MultiClass(truth, probabilities, classCount: 3, new MultiClassRocOptions { Average = Averaging.Weighted }))}");
        Console.WriteLine($"  MultiClass ovo macro  = "
            + $"{Inv.F3(RocAuc.MultiClass(truth, probabilities, classCount: 3, new MultiClassRocOptions { Strategy = MultiClassStrategy.OneVsOne }))}");

        // Parallel and sequential agree by contract; Environment.ProcessorCount here
        // is honest, not optimal — see docs/guides/performance.md before copying it.
        Console.WriteLine($"  MultiClass ovr macro  = "
            + $"{Inv.F3(RocAuc.MultiClass(truth, probabilities, classCount: 3, new MultiClassRocOptions { MaxDegreeOfParallelism = Environment.ProcessorCount }))}"
            + "  (parallel, same value)");

        // Labels names the class set explicitly, where scikit-learn infers it; SampleWeight
        // weights the samples. Both are options a caller sets and then reads back.
        var described = new MultiClassRocOptions
        {
            Labels = [0, 1, 2],
            SampleWeight = [1.0, 1.0, 2.0, 1.0, 1.0, 1.0],
        };
        Console.WriteLine($"  MultiClass weighted   = "
            + $"{Inv.F3(RocAuc.MultiClass(truth, probabilities, classCount: 3, described))} "
            + $"({described.Labels.Length} labels named, {described.SampleWeight.Length} weights)");
        Console.WriteLine();
    }

    /// <summary>The metrics that read a matrix rather than the labels.</summary>
    private static void MatrixReaders()
    {
        int[] truth = [0, 0, 1, 1, 2, 2, 2];
        int[] predicted = [0, 1, 1, 1, 2, 0, 2];
        ConfusionMatrix cm = ConfusionMatrix.Compute(truth, predicted);

        Console.WriteLine($"  BalancedAccuracy      = {Inv.F3(BalancedAccuracy.Score(cm))}");
        Console.WriteLine($"  MatthewsCorrelation   = {Inv.F3(MatthewsCorrelation.Score(cm))}");
        Console.WriteLine($"  CohenKappa            = {Inv.F3(CohenKappa.Score(cm))}");
        Console.WriteLine($"  CohenKappa (linear)   = {Inv.F3(CohenKappa.Score(cm, KappaWeighting.Linear))}");

        // normalize= is a projection: the matrix itself never becomes fractions,
        // so Accuracy.Score(cm) above still means what it says.
        double[,] rowNormalised = cm.ToArray(Normalization.True);
        Console.WriteLine($"  row-normalised [0,0]  = {Inv.F3(rowNormalised[0, 0])}");
        Console.WriteLine();
    }

}
