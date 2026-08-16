using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// The discounted gain of a ranking over the best it could have been — the equivalent
/// of <c>sklearn.metrics.ndcg_score</c>.
/// </summary>
public static class Ndcg
{
    /// <summary>Scores one or more ranked rows in <c>[0, 1]</c> — <c>sklearn.metrics.ndcg_score(y_true, y_score, k=…, ignore_ties=…)</c>.</summary>
    /// <param name="yTrue">The relevance of each document, row-major: one row per query, <paramref name="labelCount"/> values each.</param>
    /// <param name="yScore">The scores the ranking was made from, same shape as <paramref name="yTrue"/>.</param>
    /// <param name="labelCount">How many documents each row holds.</param>
    /// <param name="k">Score only the first <c>k</c> positions, or <c>null</c> for all of them.</param>
    /// <param name="ignoreTies">Rank equal scores arbitrarily instead of averaging over their permutations.</param>
    /// <returns><c>1</c> when the ranking is as good as its relevance allows, <c>0</c> when no document is relevant.</returns>
    /// <remarks>
    /// No <c>logBase</c>, because <c>ndcg_score</c> has none: the discount cancels in the ratio
    /// only when the base is shared, and scikit-learn shares base 2 on both halves. A row with
    /// no relevant document scores <c>0</c> rather than dividing by zero, measured.
    /// </remarks>
    /// <exception cref="ArgumentException">The rows disagree in length, hold fewer than two documents, or hold a negative relevance.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="k"/> is below 1.</exception>
    public static double Score(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yScore,
        int labelCount,
        int? k = null,
        bool ignoreTies = false)
    {
        Ranking.Validate(yTrue, yScore, labelCount, nameof(yTrue), nameof(yScore));

        // A negative relevance can drive the ratio below zero -- measured, -1 on a
        // two-document row -- so it is refused here as `ndcg_score` refuses it.
        if (Ranking.HasNegative(yTrue))
        {
            throw new ArgumentException(
                "ndcg_score should not be used on negative y_true values.", nameof(yTrue));
        }

        double[] discounts = Ranking.Discounts(labelCount, k, 2.0);

        double total = 0.0;
        int rows = yTrue.Length / labelCount;
        for (int row = 0; row < rows; row++)
        {
            total += Ranking.Normalized(
                yTrue.Slice(row * labelCount, labelCount),
                yScore.Slice(row * labelCount, labelCount),
                discounts,
                ignoreTies);
        }

        return total / rows;
    }
}
