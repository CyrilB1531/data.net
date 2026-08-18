using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// The detection error tradeoff curve as plot data — the equivalent of
/// <c>sklearn.metrics.det_curve</c>.
/// </summary>
/// <remarks>
/// The same two errors <see cref="RocCurve"/> plots, but both as errors and both
/// against each other: false positives against false <em>negatives</em>, rather than
/// against the true positives. Neither endpoint is carried, so it is the shortest of
/// the three on the same input — measured, 3 points where the ROC curve has 5.
/// </remarks>
public sealed class DetCurve
{
    private DetCurve(double[] falsePositiveRate, double[] falseNegativeRate, double[] thresholds)
    {
        FalsePositiveRate = falsePositiveRate;
        FalseNegativeRate = falseNegativeRate;
        Thresholds = thresholds;
    }

    /// <summary>The false-positive rate at each threshold.</summary>
    public IReadOnlyList<double> FalsePositiveRate { get; }

    /// <summary>The false-negative rate at each threshold — <c>1 - </c> the true-positive rate.</summary>
    public IReadOnlyList<double> FalseNegativeRate { get; }

    /// <summary>The score at each point, the same length as the other two.</summary>
    public IReadOnlyList<double> Thresholds { get; }

    /// <summary>Draws the curve — <c>det_curve(y_true, y_score, pos_label=…, sample_weight=…, drop_intermediate=…)</c>.</summary>
    /// <param name="yTrue">The true labels, one per sample.</param>
    /// <param name="yScore">A score per sample: the higher, the more the model believes <paramref name="posLabel"/>.</param>
    /// <param name="posLabel">The label counted as positive.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <param name="dropIntermediate">Drop points that do not turn the curve. <see langword="false"/> here, as the reference has it.</param>
    /// <exception cref="ArgumentException">The inputs disagree in length, are empty, or contain a NaN score.</exception>
    public static DetCurve Compute(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yScore,
        int posLabel = 1,
        ReadOnlySpan<double> sampleWeight = default,
        bool dropIntermediate = false)
    {
        ClassifierCurve.Points points = ClassifierCurve.Build(yTrue, yScore, posLabel, sampleWeight);

        // A threshold at +inf, where the model always answers negative, prepended
        // before the drop rather than after -- which is where the reference puts it.
        int n = points.Count + 1;
        var tp = new double[n];
        var fp = new double[n];
        var thresholds = new double[n];
        thresholds[0] = double.PositiveInfinity;
        for (int i = 0; i < points.Count; i++)
        {
            tp[i + 1] = points.TruePositives[i];
            fp[i + 1] = points.FalsePositives[i];
            thresholds[i + 1] = points.Thresholds[i];
        }

        bool[] kept = ClassifierCurve.KeepByCount(tp, dropIntermediate);
        tp = ClassifierCurve.Where(tp, kept);
        fp = ClassifierCurve.Where(fp, kept);
        thresholds = ClassifierCurve.Where(thresholds, kept);

        double positives = tp[^1];
        double negatives = fp[^1];

        // Slice from where false positives stop being zero to where false negatives
        // reach zero, then reverse so the false positives run downwards.
        int first = LastOfLeadingRun(fp);
        int last = FirstIndexOf(tp, positives) + 1;
        int kept2 = last - first;

        var fpr = new double[kept2];
        var fnr = new double[kept2];
        var scores = new double[kept2];
        for (int i = 0; i < kept2; i++)
        {
            int at = last - 1 - i;
            fpr[i] = Rate(fp[at], negatives);
            fnr[i] = Rate(positives - tp[at], positives);
            scores[i] = thresholds[at];
        }

        return new DetCurve(fpr, fnr, scores);
    }

    // S1244: each asks whether an accumulated weight equals a particular value, which
    // is what the reference's own searchsorted calls ask -- not two computations.
#pragma warning disable S1244

    /// <summary>The last index of the run <paramref name="values"/> opens with.</summary>
    private static int LastOfLeadingRun(double[] values)
    {
        int at = 0;
        while (at + 1 < values.Length && values[at + 1] == values[0])
        {
            at++;
        }

        return at;
    }

    /// <summary>The first index holding <paramref name="value"/>.</summary>
    private static int FirstIndexOf(double[] values, double value)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] == value)
            {
                return i;
            }
        }

        return values.Length - 1;
    }

    private static double Rate(double part, double total) => total == 0.0 ? double.NaN : part / total;
#pragma warning restore S1244
}
