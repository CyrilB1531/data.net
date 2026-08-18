using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// Shared information between two labellings, corrected for the information chance
/// alone would produce — the equivalent of
/// <c>sklearn.metrics.adjusted_mutual_info_score</c>.
/// </summary>
public static class AdjustedMutualInformation
{
    /// <summary>Scores the shared information between two labellings against what their cluster sizes would give by chance — <c>sklearn.metrics.adjusted_mutual_info_score(labels_true, labels_pred)</c>.</summary>
    /// <param name="labelsTrue">The reference labelling.</param>
    /// <param name="labelsPred">The labelling to score, same length as <paramref name="labelsTrue"/>.</param>
    /// <returns><c>1</c> when each labelling determines the other, and about <c>0</c> for two independent ones however many clusters they have.</returns>
    /// <remarks>
    /// This is <see cref="NormalizedMutualInformation"/> with the chance correction
    /// <see cref="AdjustedRand"/> applies to pair counts. The correction is what makes it safe to
    /// compare across different numbers of clusters: splitting a labelling further raises the raw
    /// mutual information and raises the expected one with it.
    /// </remarks>
    /// <exception cref="ArgumentException">The two labellings disagree in length.</exception>
    public static double Score(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
    {
        Contingency.Validate(labelsTrue, labelsPred);
        Contingency table = Contingency.Build(labelsTrue, labelsPred);

        // Both labellings in one piece, or both empty: no split to disagree about.
        int classes = table.Rows.Length;
        int clusters = table.Columns.Length;
        if (classes == clusters && classes <= 1)
        {
            return 1.0;
        }

        double information = table.MutualInformation();
        double expected = ExpectedMutualInformation.Compute(table.Rows, table.Columns, table.Samples);
        double normalizer = (Contingency.Entropy(table.Rows, table.Samples) +
                             Contingency.Entropy(table.Columns, table.Samples)) / 2.0;

        // scikit-learn pushes a denominator away from zero rather than testing it,
        // keeping its sign so the quotient does not flip.
        double denominator = normalizer - expected;
        const double Epsilon = 2.220446049250313e-16;
        denominator = denominator < 0.0
            ? Math.Min(denominator, -Epsilon)
            : Math.Max(denominator, Epsilon);

        return (information - expected) / denominator;
    }
}
