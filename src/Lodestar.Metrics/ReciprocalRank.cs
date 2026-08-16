namespace Lodestar.Metrics;

/// <summary>
/// How high the first relevant document lands, averaged over queries — mean reciprocal
/// rank, which scikit-learn does not implement.
/// </summary>
/// <remarks>
/// <strong>Not verified against a reference.</strong> Every other member of this package
/// replays a frozen corpus captured from Python; there is no <c>reciprocal</c> function in
/// <c>sklearn.metrics</c> to capture, so this one is proven by tests that pin its definition
/// instead — decision 0036, which also says what would retire that exception.
/// </remarks>
public static class ReciprocalRank
{
    /// <summary>Scores rankings by the position of their first relevant document.</summary>
    /// <param name="relevance">Whether each document is relevant, row-major: one row per query, <paramref name="labelCount"/> values each. Non-zero is relevant.</param>
    /// <param name="yScore">The scores the ranking was made from, same shape as <paramref name="relevance"/>.</param>
    /// <param name="labelCount">How many documents each row holds.</param>
    /// <returns><c>1</c> when every query puts a relevant document first, and <c>0</c> when no query retrieves one at all.</returns>
    /// <remarks>
    /// The definition, pinned by tests because no reference pins it: the reciprocal of the rank
    /// of the <em>first</em> relevant document, averaged over queries, with a query holding no
    /// relevant document contributing <c>0</c> rather than being dropped from the average.
    /// Ties are broken as everywhere else here — the reference's own order, higher index first.
    /// </remarks>
    /// <exception cref="ArgumentException">The rows disagree in length, or hold fewer than two documents.</exception>
    public static double Score(
        ReadOnlySpan<double> relevance, ReadOnlySpan<double> yScore, int labelCount)
    {
        Internal.Ranking.Validate(relevance, yScore, labelCount, nameof(relevance), nameof(yScore));

        double total = 0.0;
        int rows = relevance.Length / labelCount;
        for (int row = 0; row < rows; row++)
        {
            ReadOnlySpan<double> judged = relevance.Slice(row * labelCount, labelCount);
            int[] order = Internal.Ranking.Descending(yScore.Slice(row * labelCount, labelCount));
            for (int rank = 0; rank < order.Length; rank++)
            {
                // S1244: relevance is a judgement, not a measurement -- "not zero" is the
                // question, and a tolerance would make a 1e-16 label mean irrelevant.
#pragma warning disable S1244
                if (judged[order[rank]] != 0.0)
#pragma warning restore S1244
                {
                    total += 1.0 / (rank + 1);
                    break;
                }
            }
        }

        return total / rows;
    }
}
