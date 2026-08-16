using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// Agreement between two partitions, corrected for the agreement chance alone would
/// produce — the equivalent of <c>sklearn.metrics.adjusted_rand_score</c>.
/// </summary>
public static class AdjustedRand
{
    /// <summary>Scores how well two labellings of the same samples agree — <c>sklearn.metrics.adjusted_rand_score(labels_true, labels_pred)</c>.</summary>
    /// <param name="labelsTrue">The reference labelling.</param>
    /// <param name="labelsPred">The labelling to score, same length as <paramref name="labelsTrue"/>.</param>
    /// <returns><c>1</c> for the same partition however it is named, <c>0</c> for the agreement chance gives, and negative below that.</returns>
    /// <remarks>
    /// The label values carry no meaning: only which samples share one does, so renaming every
    /// cluster changes nothing. Measured against scikit-learn 1.9.0, two independent partitions of
    /// four samples score <c>-0.5</c> rather than <c>0</c>, and an empty input scores <c>1.0</c> —
    /// agreeing about nothing is agreeing. The reference page has the rest of that table.
    /// </remarks>
    /// <exception cref="ArgumentException">The two labellings disagree in length.</exception>
    public static double Score(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
    {
        Contingency.Validate(labelsTrue, labelsPred);
        Contingency table = Contingency.Build(labelsTrue, labelsPred);

        // The shapes scikit-learn answers before computing: one cluster each, none each,
        // or one per sample each -- all the same partition, so all perfect agreement.
        int classes = table.Rows.Length;
        int clusters = table.Columns.Length;
        if ((classes == clusters && classes <= 1) ||
            (classes == clusters && classes == table.Samples))
        {
            return 1.0;
        }

        double pairs = 0.0;
        foreach (KeyValuePair<long, int> cell in table.Cells)
        {
            pairs += Choose2(cell.Value);
        }

        double rowPairs = 0.0;
        foreach (int count in table.Rows)
        {
            rowPairs += Choose2(count);
        }

        double columnPairs = 0.0;
        foreach (int count in table.Columns)
        {
            columnPairs += Choose2(count);
        }

        double expected = rowPairs * columnPairs / Choose2(table.Samples);
        double best = (rowPairs + columnPairs) / 2.0;
        return (pairs - expected) / (best - expected);
    }

    private static double Choose2(double count) => count * (count - 1.0) / 2.0;
}
