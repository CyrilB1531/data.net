using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// How often an irrelevant label outranks a relevant one — the equivalent of
/// <c>sklearn.metrics.label_ranking_loss</c>.
/// </summary>
public static class LabelRankingLoss
{
    /// <summary>Scores a boolean label matrix — <c>sklearn.metrics.label_ranking_loss(y_true, y_score, sample_weight=…)</c>.</summary>
    /// <param name="yTrue">Whether each label is relevant, row-major: one row per sample, <paramref name="labelCount"/> values each.</param>
    /// <param name="yScore">The scores the ranking was made from, same shape as <paramref name="yTrue"/>.</param>
    /// <param name="labelCount">How many labels each row holds.</param>
    /// <param name="sampleWeight">One weight per sample, or empty for an unweighted mean.</param>
    /// <returns>The mean fraction of wrongly ordered pairs, in <c>[0, 1]</c>. <c>0</c> is perfect, and a row where every label or no label is relevant contributes <c>0</c> — it holds no pair to order.</returns>
    /// <exception cref="ArgumentException">The shapes disagree, <paramref name="labelCount"/> is <c>1</c>, or <paramref name="sampleWeight"/> sums to zero.</exception>
    public static double Score(
        ReadOnlySpan<bool> yTrue,
        ReadOnlySpan<double> yScore,
        int labelCount,
        ReadOnlySpan<double> sampleWeight = default)
    {
        LabelRanking.Validate(yTrue, yScore, labelCount, sampleWeight, singleLabelAllowed: false);

        int rows = yTrue.Length / labelCount;
        double[] perRow = new double[rows];
        for (int row = 0; row < rows; row++)
        {
            ReadOnlySpan<bool> relevant = yTrue.Slice(row * labelCount, labelCount);
            ReadOnlySpan<double> scores = yScore.Slice(row * labelCount, labelCount);
            int positives = LabelRanking.RelevantCount(relevant);
            if (positives == 0 || positives == labelCount)
            {
                continue;
            }

            long wrong = CountWrongPairs(relevant, scores, labelCount);
            perRow[row] = (double)wrong / ((long)positives * (labelCount - positives));
        }

        return LabelRanking.Weighted(perRow, sampleWeight);
    }

    /// <summary>How many (relevant, irrelevant) pairs of one row score the irrelevant label at least as high.</summary>
    private static long CountWrongPairs(ReadOnlySpan<bool> relevant, ReadOnlySpan<double> scores, int labelCount)
    {
        long wrong = 0;
        for (int r = 0; r < labelCount; r++)
        {
            if (!relevant[r])
            {
                continue;
            }

            for (int f = 0; f < labelCount; f++)
            {
                // A tie is an error: the reference counts an irrelevant label sharing a
                // relevant one's score as outranking it, and the corpus pins that.
                if (!relevant[f] && scores[r] <= scores[f])
                {
                    wrong++;
                }
            }
        }

        return wrong;
    }
}
