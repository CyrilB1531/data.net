using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// Average precision — <c>sklearn.metrics.average_precision_score</c>, the step sum
/// over the precision-recall curve.
/// </summary>
/// <remarks>
/// A sum of <c>(R_n - R_(n-1)) * P_n</c> across thresholds, not the area under the
/// curve: the trapezoid reads two thresholds apart as though the curve were linear
/// between them and comes out optimistic. Measured, <c>[0, 0, 1, 1]</c> against
/// <c>[0.1, 0.4, 0.35, 0.8]</c> sums to 0.8333333333333333 and integrates to 0.7916666666666666.
/// </remarks>
public static class AveragePrecision
{
    /// <summary>The binary case — <c>average_precision_score(y_true, y_score, pos_label=…, sample_weight=…)</c>.</summary>
    /// <param name="yTrue">The true labels, one per sample.</param>
    /// <param name="yScore">A score per sample: the higher, the more the model believes <paramref name="posLabel"/>.</param>
    /// <param name="posLabel">The label counted as positive. scikit-learn infers this; 1 is what it infers for 0/1 labels.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <returns>
    /// <c>0</c> when no sample carries <paramref name="posLabel"/>: scikit-learn warns
    /// that recall is taken as one for all thresholds and returns that, and this
    /// reproduces the value rather than refusing the input.
    /// </returns>
    /// <exception cref="ArgumentException">The inputs disagree in length, are empty, or contain a NaN score.</exception>
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yScore,
        int posLabel = 1,
        ReadOnlySpan<double> sampleWeight = default) =>
        BinaryRoc.AveragePrecision(yTrue, yScore, posLabel, sampleWeight);

    /// <summary>
    /// The multilabel case — <c>average_precision_score(y_true, y_score, average=…, sample_weight=…)</c>
    /// over a label matrix.
    /// </summary>
    /// <param name="yTrue">Whether each label is relevant, row-major: one row per sample, <paramref name="labelCount"/> values each.</param>
    /// <param name="yScore">The scores the ranking was made from, same shape as <paramref name="yTrue"/>.</param>
    /// <param name="labelCount">How many labels each row holds.</param>
    /// <param name="averaging">How the per-label scores are combined. <see cref="Averaging.Binary"/> has no meaning over a matrix and is refused.</param>
    /// <param name="sampleWeight">One weight per sample — per row, not per label. Omit to weight every sample by 1.</param>
    /// <exception cref="ArgumentException">The shapes disagree, or <paramref name="sampleWeight"/> has the wrong length.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="averaging"/> is <see cref="Averaging.Binary"/> or is not a declared member.</exception>
    public static double Score(
        ReadOnlySpan<bool> yTrue,
        ReadOnlySpan<double> yScore,
        int labelCount,
        Averaging averaging = Averaging.Macro,
        ReadOnlySpan<double> sampleWeight = default)
    {
        LabelRanking.Validate(yTrue, yScore, labelCount, sampleWeight, singleLabelAllowed: true);

        return averaging switch
        {
            Averaging.Micro => Ravelled(yTrue, yScore, labelCount, sampleWeight),
            Averaging.Macro => Mean(PerLabelScores(yTrue, yScore, labelCount, sampleWeight)),
            Averaging.Weighted => Weighted(yTrue, yScore, labelCount, sampleWeight),
            Averaging.Binary => throw new ArgumentOutOfRangeException(
                nameof(averaging),
                averaging,
                "Averaging.Binary scores one positive label of two and has no meaning over a label matrix."),
            _ => throw new ArgumentOutOfRangeException(nameof(averaging), averaging, "Not a declared Averaging member."),
        };
    }

    /// <summary>One average precision per label — <c>average_precision_score(…, average=None)</c>.</summary>
    /// <param name="yTrue">Whether each label is relevant, row-major: one row per sample, <paramref name="labelCount"/> values each.</param>
    /// <param name="yScore">The scores the ranking was made from, same shape as <paramref name="yTrue"/>.</param>
    /// <param name="labelCount">How many labels each row holds.</param>
    /// <param name="sampleWeight">One weight per sample — per row, not per label. Omit to weight every sample by 1.</param>
    /// <returns>A score per label, in column order. A label no sample carries scores <c>0</c>, for the reason <see cref="Score(ReadOnlySpan{int}, ReadOnlySpan{double}, int, ReadOnlySpan{double})"/> gives.</returns>
    /// <exception cref="ArgumentException">The shapes disagree, or <paramref name="sampleWeight"/> has the wrong length.</exception>
    public static double[] PerLabel(
        ReadOnlySpan<bool> yTrue,
        ReadOnlySpan<double> yScore,
        int labelCount,
        ReadOnlySpan<double> sampleWeight = default)
    {
        LabelRanking.Validate(yTrue, yScore, labelCount, sampleWeight, singleLabelAllowed: true);
        return PerLabelScores(yTrue, yScore, labelCount, sampleWeight);
    }

    private static double[] PerLabelScores(
        ReadOnlySpan<bool> yTrue, ReadOnlySpan<double> yScore, int labelCount, ReadOnlySpan<double> sampleWeight)
    {
        int rows = yTrue.Length / labelCount;
        var scores = new double[labelCount];
        int[] column = new int[rows];
        double[] columnScores = new double[rows];

        for (int label = 0; label < labelCount; label++)
        {
            for (int row = 0; row < rows; row++)
            {
                int at = (row * labelCount) + label;
                column[row] = yTrue[at] ? 1 : 0;
                columnScores[row] = yScore[at];
            }

            scores[label] = BinaryRoc.AveragePrecision(column, columnScores, 1, sampleWeight);
        }

        return scores;
    }

    /// <summary>The whole matrix read as one binary problem, row by row — the reference's <c>ravel()</c>.</summary>
    private static double Ravelled(
        ReadOnlySpan<bool> yTrue, ReadOnlySpan<double> yScore, int labelCount, ReadOnlySpan<double> sampleWeight)
    {
        int[] flat = new int[yTrue.Length];
        for (int i = 0; i < yTrue.Length; i++)
        {
            flat[i] = yTrue[i] ? 1 : 0;
        }

        if (sampleWeight.IsEmpty)
        {
            return BinaryRoc.AveragePrecision(flat, yScore, 1, default);
        }

        // A weight belongs to a sample, so raveling repeats it across that row's labels.
        double[] repeated = new double[yTrue.Length];
        for (int i = 0; i < repeated.Length; i++)
        {
            repeated[i] = sampleWeight[i / labelCount];
        }

        return BinaryRoc.AveragePrecision(flat, yScore, 1, repeated);
    }

    /// <summary>Per-label scores averaged by how much positive weight each label carries.</summary>
    private static double Weighted(
        ReadOnlySpan<bool> yTrue, ReadOnlySpan<double> yScore, int labelCount, ReadOnlySpan<double> sampleWeight)
    {
        double[] scores = PerLabelScores(yTrue, yScore, labelCount, sampleWeight);
        int rows = yTrue.Length / labelCount;
        var weights = new double[labelCount];
        double total = 0.0;

        for (int row = 0; row < rows; row++)
        {
            double weight = sampleWeight.IsEmpty ? 1.0 : sampleWeight[row];
            for (int label = 0; label < labelCount; label++)
            {
                if (yTrue[(row * labelCount) + label])
                {
                    weights[label] += weight;
                    total += weight;
                }
            }
        }

        // No label carries a positive sample, so there is nothing to weight by and
        // the reference returns zero rather than dividing.
#pragma warning disable S1244
        if (total == 0.0)
        {
            return 0.0;
        }
#pragma warning restore S1244

        double sum = 0.0;
        for (int label = 0; label < labelCount; label++)
        {
            sum += scores[label] * weights[label];
        }

        return sum / total;
    }

    private static double Mean(double[] scores)
    {
        double sum = 0.0;
        foreach (double score in scores)
        {
            sum += score;
        }

        return sum / scores.Length;
    }
}
