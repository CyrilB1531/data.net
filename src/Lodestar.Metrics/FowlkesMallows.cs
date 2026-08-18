using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// Agreement between two partitions as the geometric mean of pair precision and pair
/// recall — the equivalent of <c>sklearn.metrics.fowlkes_mallows_score</c>.
/// </summary>
public static class FowlkesMallows
{
    /// <summary>Scores two labellings on the pairs of samples they agree to put together — <c>sklearn.metrics.fowlkes_mallows_score(labels_true, labels_pred)</c>.</summary>
    /// <param name="labelsTrue">The reference labelling.</param>
    /// <param name="labelsPred">The labelling to score, same length as <paramref name="labelsTrue"/>.</param>
    /// <returns><c>1</c> when both labellings pair the same samples, <c>0</c> when they share no pair.</returns>
    /// <remarks>
    /// Counts pairs rather than samples, like <see cref="AdjustedRand"/>, and unlike it is
    /// <b>not</b> corrected for chance — so two independent partitions score above zero here
    /// where <see cref="AdjustedRand"/> scores zero or below. That makes it the wrong choice for
    /// comparing across different numbers of clusters, and a reasonable one for comparing two
    /// clusterings of the same data at the same size.
    /// </remarks>
    /// <exception cref="ArgumentException">The two labellings disagree in length.</exception>
    public static double Score(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
    {
        Contingency.Validate(labelsTrue, labelsPred);
        Contingency table = Contingency.Build(labelsTrue, labelsPred);

        long samples = table.Samples;
        long agreeing = SumOfSquares(table.Cells.Values) - samples;
        long inTrue = SumOfSquares(table.Rows) - samples;
        long inPred = SumOfSquares(table.Columns) - samples;

        if (agreeing == 0)
        {
            return 0.0;
        }

        // sqrt(tk/pk) * sqrt(tk/qk), which is scikit-learn's own grouping rather than
        // tk/sqrt(pk*qk): the two disagree in the last places, and the corpus reads them.
        return Math.Sqrt(agreeing / (double)inTrue) * Math.Sqrt(agreeing / (double)inPred);
    }

    /// <summary>Accumulates in <see cref="long"/> because n squared overflows an int at 46 341 samples.</summary>
    private static long SumOfSquares(IEnumerable<int> counts)
    {
        long total = 0;
        foreach (int count in counts)
        {
            total += (long)count * count;
        }

        return total;
    }
}
