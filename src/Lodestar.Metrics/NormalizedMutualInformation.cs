using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// How much one labelling tells you about the other, scaled into <c>[0, 1]</c> —
/// the equivalent of <c>sklearn.metrics.normalized_mutual_info_score</c>.
/// </summary>
public static class NormalizedMutualInformation
{
    /// <summary>Scores the shared information between two labellings against the mean of their entropies — <c>sklearn.metrics.normalized_mutual_info_score(labels_true, labels_pred)</c>.</summary>
    /// <param name="labelsTrue">The reference labelling.</param>
    /// <param name="labelsPred">The labelling to score, same length as <paramref name="labelsTrue"/>.</param>
    /// <returns><c>1</c> when each labelling determines the other, <c>0</c> when neither says anything about the other.</returns>
    /// <remarks>
    /// <c>MI</c> over the arithmetic mean of the two entropies, which is the same quantity as
    /// <see cref="VMeasure"/> — its remark writes the cancellation out. The other three
    /// <c>average_method</c> normalizers are absent rather than refused: no corpus row covers them.
    /// Nothing here is corrected for chance, and the gap is measurable: <c>[0,0,1,1]</c> against
    /// <c>[0,1,2,3]</c> scores <c>0.667</c> where <see cref="AdjustedRand"/> scores <c>0</c>. The
    /// split labelling does determine the truth; what it does not do is beat chance at it.
    /// </remarks>
    /// <exception cref="ArgumentException">The two labellings disagree in length.</exception>
    public static double Score(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
    {
        Contingency.Validate(labelsTrue, labelsPred);
        Contingency table = Contingency.Build(labelsTrue, labelsPred);

        int classes = table.Rows.Length;
        int clusters = table.Columns.Length;
        if (classes == clusters && classes <= 1)
        {
            return 1.0;
        }

        double information = table.MutualInformation();
        if (information <= 0.0)
        {
            return 0.0;
        }

        double normalizer = (Contingency.Entropy(table.Rows, table.Samples) +
                             Contingency.Entropy(table.Columns, table.Samples)) / 2.0;

        // scikit-learn floors the normalizer at the machine epsilon rather than
        // testing it against zero, and the quotient inherits that.
        return information / Math.Max(normalizer, 2.220446049250313e-16);
    }
}
