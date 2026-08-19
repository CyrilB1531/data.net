using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// The precision-recall curve as plot data — the equivalent of
/// <c>sklearn.metrics.precision_recall_curve</c>.
/// </summary>
/// <remarks>
/// <see cref="Thresholds"/> is **one shorter** than the other two, deliberately: the
/// curve carries an endpoint at recall 0 and precision 1 that no threshold produces.
/// That asymmetry is the reference's and is reproduced rather than padded, because a
/// caller plotting thresholds against precision needs to know which point has none.
/// </remarks>
public sealed class PrecisionRecallCurve
{
    private PrecisionRecallCurve(double[] precision, double[] recall, double[] thresholds)
    {
        Precision = precision;
        Recall = recall;
        Thresholds = thresholds;
    }

    /// <summary>The precision at each point, ending at <c>1</c>.</summary>
    public IReadOnlyList<double> Precision { get; }

    /// <summary>The recall at each point, descending to <c>0</c>.</summary>
    public IReadOnlyList<double> Recall { get; }

    /// <summary>The score at each point — <b>one shorter</b> than <see cref="Precision"/> and <see cref="Recall"/>.</summary>
    public IReadOnlyList<double> Thresholds { get; }

    /// <summary>Draws the curve — <c>precision_recall_curve(y_true, y_score, pos_label=…, sample_weight=…, drop_intermediate=…)</c>.</summary>
    /// <param name="yTrue">The true labels, one per sample.</param>
    /// <param name="yScore">A score per sample: the higher, the more the model believes <paramref name="posLabel"/>.</param>
    /// <param name="posLabel">The label counted as positive.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <param name="dropIntermediate">Drop points that do not turn the curve. <see langword="false"/> here, matching the reference's default for this curve and not for <see cref="RocCurve"/>.</param>
    /// <exception cref="ArgumentException">The inputs disagree in length, are empty, or contain a NaN score.</exception>
    public static PrecisionRecallCurve Compute(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yScore,
        int posLabel = 1,
        ReadOnlySpan<double> sampleWeight = default,
        bool dropIntermediate = false)
    {
        ClassifierCurve.Points points = ClassifierCurve.Build(yTrue, yScore, posLabel, sampleWeight);
        bool[] kept = ClassifierCurve.KeepByCount(points.TruePositives, dropIntermediate);
        double[] truePositives = ClassifierCurve.Where(points.TruePositives, kept);
        double[] falsePositives = ClassifierCurve.Where(points.FalsePositives, kept);
        double[] scores = ClassifierCurve.Where(points.Thresholds, kept);
        double positives = points.PositiveTotal;

        int n = truePositives.Length;
        var precision = new double[n];
        var recall = new double[n];
        for (int i = 0; i < n; i++)
        {
            double predicted = truePositives[i] + falsePositives[i];

            // S1244: whether anything was predicted positive at all at this threshold,
            // and whether any sample is positive -- both are the reference's own tests
            // against exact zero, answered with 0 and with 1 rather than by dividing.
#pragma warning disable S1244
            precision[i] = predicted == 0.0 ? 0.0 : truePositives[i] / predicted;
            recall[i] = positives == 0.0 ? 1.0 : truePositives[i] / positives;
#pragma warning restore S1244
        }

        // The reference reverses the arrays and appends (1, 0) -- an endpoint no
        // threshold produces, which is why Thresholds stays one shorter.
        var outPrecision = new double[n + 1];
        var outRecall = new double[n + 1];
        var outThresholds = new double[n];
        for (int i = 0; i < n; i++)
        {
            outPrecision[i] = precision[n - 1 - i];
            outRecall[i] = recall[n - 1 - i];
            outThresholds[i] = scores[n - 1 - i];
        }

        outPrecision[n] = 1.0;
        outRecall[n] = 0.0;
        return new PrecisionRecallCurve(outPrecision, outRecall, outThresholds);
    }
}
