using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// The receiver operating characteristic as plot data — the equivalent of
/// <c>sklearn.metrics.roc_curve</c>.
/// </summary>
/// <remarks>
/// A class rather than a record, as <see cref="ClassificationReport"/> is: value
/// equality over three arrays compares references, and a <c>with</c> copy would be
/// neither cheap nor honest. The three are parallel and of equal length here, which is
/// not true of <see cref="PrecisionRecallCurve"/>.
/// </remarks>
public sealed class RocCurve
{
    private RocCurve(double[] falsePositiveRate, double[] truePositiveRate, double[] thresholds)
    {
        FalsePositiveRate = falsePositiveRate;
        TruePositiveRate = truePositiveRate;
        Thresholds = thresholds;
    }

    /// <summary>The false-positive rate at each threshold, ascending from <c>0</c>.</summary>
    public IReadOnlyList<double> FalsePositiveRate { get; }

    /// <summary>The true-positive rate at each threshold, ascending from <c>0</c>.</summary>
    public IReadOnlyList<double> TruePositiveRate { get; }

    /// <summary>The score at each point. The first is <see cref="double.PositiveInfinity"/> — no sample is above it, which is what puts the curve at the origin.</summary>
    public IReadOnlyList<double> Thresholds { get; }

    /// <summary>Draws the curve — <c>roc_curve(y_true, y_score, pos_label=…, sample_weight=…, drop_intermediate=…)</c>.</summary>
    /// <param name="yTrue">The true labels, one per sample.</param>
    /// <param name="yScore">A score per sample: the higher, the more the model believes <paramref name="posLabel"/>.</param>
    /// <param name="posLabel">The label counted as positive. scikit-learn infers this; 1 is what it infers for 0/1 labels.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <param name="dropIntermediate">Drop points that do not turn the curve. <see langword="true"/> here, matching the reference's default for this curve and not for the other two.</param>
    /// <returns>Three parallel arrays of the same length, with the origin prepended.</returns>
    /// <exception cref="ArgumentException">The inputs disagree in length, are empty, or contain a NaN score.</exception>
    public static RocCurve Compute(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yScore,
        int posLabel = 1,
        ReadOnlySpan<double> sampleWeight = default,
        bool dropIntermediate = true)
    {
        ClassifierCurve.Points points = ClassifierCurve.Build(yTrue, yScore, posLabel, sampleWeight);
        bool[] keep = ClassifierCurve.Keep(points.TruePositives, points.FalsePositives, dropIntermediate);

        double[] tp = ClassifierCurve.Where(points.TruePositives, keep);
        double[] fp = ClassifierCurve.Where(points.FalsePositives, keep);
        double[] thresholds = ClassifierCurve.Where(points.Thresholds, keep);

        // The origin is a point no threshold produces: nothing is above +inf, so both
        // rates are 0 there. The reference prepends it rather than deriving it.
        var fpr = new double[tp.Length + 1];
        var tpr = new double[tp.Length + 1];
        var scores = new double[tp.Length + 1];
        scores[0] = double.PositiveInfinity;

        for (int i = 0; i < tp.Length; i++)
        {
            fpr[i + 1] = Rate(fp[i], points.NegativeTotal);
            tpr[i + 1] = Rate(tp[i], points.PositiveTotal);
            scores[i + 1] = thresholds[i];
        }

        return new RocCurve(fpr, tpr, scores);
    }

    // S1244: whether the class is absent altogether, which the reference answers with
    // nan rather than dividing -- it warns about it and returns the array anyway.
#pragma warning disable S1244
    private static double Rate(double part, double total) => total == 0.0 ? double.NaN : part / total;
#pragma warning restore S1244
}
