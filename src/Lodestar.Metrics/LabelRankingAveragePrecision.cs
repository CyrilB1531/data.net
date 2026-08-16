using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// How much of the ranking above each relevant label is itself relevant — the
/// equivalent of <c>sklearn.metrics.label_ranking_average_precision_score</c>.
/// </summary>
public static class LabelRankingAveragePrecision
{
    /// <summary>Scores a boolean label matrix — <c>sklearn.metrics.label_ranking_average_precision_score(y_true, y_score, sample_weight=…)</c>.</summary>
    /// <param name="yTrue">Whether each label is relevant, row-major: one row per sample, <paramref name="labelCount"/> values each.</param>
    /// <param name="yScore">The scores the ranking was made from, same shape as <paramref name="yTrue"/>.</param>
    /// <param name="labelCount">How many labels each row holds.</param>
    /// <param name="sampleWeight">One weight per sample, or empty for an unweighted mean.</param>
    /// <returns><c>1</c> when every relevant label outranks every irrelevant one. A row where every label or no label is relevant scores <c>1</c> too — its ranking carries no information, and the reference says so in a comment.</returns>
    /// <exception cref="ArgumentException">The shapes disagree, or <paramref name="sampleWeight"/> has the wrong length.</exception>
    public static double Score(
        ReadOnlySpan<bool> yTrue,
        ReadOnlySpan<double> yScore,
        int labelCount,
        ReadOnlySpan<double> sampleWeight = default)
    {
        LabelRanking.Validate(yTrue, yScore, labelCount, sampleWeight, singleLabelAllowed: true);

        int rows = yTrue.Length / labelCount;
        int[] ranks = new int[labelCount];
        double[] relevantScores = new double[labelCount];
        int[] relevantRanks = new int[labelCount];

        double total = 0.0;
        double weights = 0.0;
        for (int row = 0; row < rows; row++)
        {
            ReadOnlySpan<bool> relevant = yTrue.Slice(row * labelCount, labelCount);
            ReadOnlySpan<double> scores = yScore.Slice(row * labelCount, labelCount);
            int positives = LabelRanking.RelevantCount(relevant);
            double aux = positives == 0 || positives == labelCount
                ? 1.0
                : Precision(relevant, scores, positives, ranks, relevantScores, relevantRanks);

            double weight = sampleWeight.Length == 0 ? 1.0 : sampleWeight[row];
            total += aux * weight;
            weights += weight;
        }

        // Divided directly rather than through LabelRanking.Weighted: the reference divides
        // here too, so a weight vector summing to zero gives NaN where the other two throw.
        return total / weights;
    }

    /// <summary>One row's mean of <c>L / rank</c> over its relevant labels.</summary>
    private static double Precision(
        ReadOnlySpan<bool> relevant,
        ReadOnlySpan<double> scores,
        int positives,
        int[] ranks,
        double[] relevantScores,
        int[] relevantRanks)
    {
        LabelRanking.MaxRank(scores, ranks);

        int taken = 0;
        for (int label = 0; label < relevant.Length; label++)
        {
            if (relevant[label])
            {
                relevantScores[taken++] = scores[label];
            }
        }

        LabelRanking.MaxRank(
            relevantScores.AsSpan(0, positives), relevantRanks.AsSpan(0, positives));

        double sum = 0.0;
        taken = 0;
        for (int label = 0; label < relevant.Length; label++)
        {
            if (relevant[label])
            {
                sum += (double)relevantRanks[taken++] / ranks[label];
            }
        }

        return sum / positives;
    }
}
