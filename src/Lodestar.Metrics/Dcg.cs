using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// How much relevance a ranking puts near the top, discounted by position — the
/// equivalent of <c>sklearn.metrics.dcg_score</c>.
/// </summary>
public static class Dcg
{
    /// <summary>Scores one or more ranked rows — <c>sklearn.metrics.dcg_score(y_true, y_score, k=…, log_base=…, sample_weight=…, ignore_ties=…)</c>.</summary>
    /// <param name="yTrue">The relevance of each document, row-major: one row per query, <paramref name="labelCount"/> values each.</param>
    /// <param name="yScore">The scores the ranking was made from, same shape as <paramref name="yTrue"/>.</param>
    /// <param name="labelCount">How many documents each row holds.</param>
    /// <param name="k">Score only the first <c>k</c> positions, or <c>null</c> for all of them.</param>
    /// <param name="logBase">The base of the positional discount, anywhere in <c>(0, ∞)</c>; <c>2</c> is scikit-learn's default.</param>
    /// <param name="ignoreTies">Rank equal scores arbitrarily instead of averaging over their permutations.</param>
    /// <param name="sampleWeight">One weight per query, or empty for an unweighted mean.</param>
    /// <returns>The mean discounted gain over the rows. Unbounded above: it grows with the relevance values.</returns>
    /// <remarks>
    /// The gains are linear, <c>Σ relevance / log(rank + 1)</c>. Much of the literature uses
    /// <c>2^relevance − 1</c> instead, which on the same row gives <c>9.3927…</c> where this
    /// gives <c>4.7618…</c> — the difference is the definition, not an error on either side.
    /// </remarks>
    /// <exception cref="ArgumentException">The rows disagree in length, hold fewer than two documents, or <paramref name="sampleWeight"/> has the wrong length or sums to zero.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="k"/> is below 1, or <paramref name="logBase"/> is outside <c>(0, ∞)</c> — zero, negative, <c>NaN</c> or infinite.</exception>
    public static double Score(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yScore,
        int labelCount,
        int? k = null,
        double logBase = 2.0,
        bool ignoreTies = false,
        ReadOnlySpan<double> sampleWeight = default)
    {
        Ranking.Validate(yTrue, yScore, labelCount, nameof(yTrue), nameof(yScore));
        int rows = yTrue.Length / labelCount;
        Weights.Validate(sampleWeight, rows, nameof(sampleWeight));
        double[] discounts = Ranking.Discounts(labelCount, k, logBase);

        double[] perQuery = new double[rows];
        for (int row = 0; row < rows; row++)
        {
            ReadOnlySpan<double> relevance = yTrue.Slice(row * labelCount, labelCount);
            ReadOnlySpan<double> scores = yScore.Slice(row * labelCount, labelCount);
            perQuery[row] = ignoreTies
                ? Ranking.Gain(relevance, scores, discounts)
                : Ranking.TieAveragedGain(relevance, scores, discounts);
        }

        return Weights.Mean(perQuery, sampleWeight);
    }
}
