namespace Lodestar.Metrics.Internal;

/// <summary>The ranks of one row of scores, and the shape the three label-matrix metrics share.</summary>
/// <remarks>
/// The rank is <c>rankdata(-y_score, "max")</c>: 1 is the best score, and every member of a tied
/// group takes the group's worst rank. Written as a count rather than a sorted permutation —
/// <c>|{k : score[k] >= score[j]}|</c> — so no ordering of equal scores exists to be wrong about.
/// </remarks>
internal static class LabelRanking
{
    /// <summary>The 1-based rank of each label, best first, ties taking the group's worst.</summary>
    public static void MaxRank(ReadOnlySpan<double> scores, Span<int> ranks)
    {
        double[] sorted = scores.ToArray();
        Array.Sort(sorted);
        for (int j = 0; j < scores.Length; j++)
        {
            ranks[j] = scores.Length - LowerBound(sorted, scores[j]);
        }
    }

    /// <summary>How many of <paramref name="sorted"/> are strictly below <paramref name="value"/>.</summary>
    private static int LowerBound(double[] sorted, double value)
    {
        int low = 0;
        int high = sorted.Length;
        while (low < high)
        {
            int mid = low + ((high - low) / 2);
            if (sorted[mid] < value)
            {
                low = mid + 1;
            }
            else
            {
                high = mid;
            }
        }

        return low;
    }

    /// <summary>How many labels of one row are relevant.</summary>
    public static int RelevantCount(ReadOnlySpan<bool> row)
    {
        int count = 0;
        foreach (bool relevant in row)
        {
            if (relevant)
            {
                count++;
            }
        }

        return count;
    }

    /// <summary>Refuses the shapes scikit-learn refuses, with the sentences it prints.</summary>
    /// <remarks>
    /// <paramref name="singleLabelAllowed"/> because the reference is not of one mind:
    /// <c>label_ranking_average_precision_score</c> scores a single label column and returns 1,
    /// where <c>coverage_error</c> and <c>label_ranking_loss</c> refuse it. Reproduced rather
    /// than smoothed — making the three agree would invent a divergence instead of copying one.
    /// </remarks>
    public static void Validate(
        ReadOnlySpan<bool> yTrue,
        ReadOnlySpan<double> yScore,
        int labelCount,
        ReadOnlySpan<double> sampleWeight,
        bool singleLabelAllowed)
    {
        if (labelCount < 1)
        {
            throw new ArgumentException(
                $"yScore holds {labelCount} labels; a label matrix needs at least 1.",
                nameof(labelCount));
        }

        if (labelCount == 1 && !singleLabelAllowed)
        {
            throw new ArgumentException("binary format is not supported", nameof(labelCount));
        }

        if (yTrue.Length != yScore.Length)
        {
            throw new ArgumentException(
                $"yTrue holds {yTrue.Length} values and yScore holds {yScore.Length}; " +
                "y_true and y_score have different shape.",
                nameof(yScore));
        }

        if (yTrue.Length == 0 || yTrue.Length % labelCount != 0)
        {
            throw new ArgumentException(
                $"yTrue holds {yTrue.Length} values, which is not a whole number of rows " +
                $"of {labelCount}.",
                nameof(yTrue));
        }

        if (sampleWeight.Length != 0 && sampleWeight.Length != yTrue.Length / labelCount)
        {
            throw new ArgumentException(
                $"sampleWeight holds {sampleWeight.Length} values for " +
                $"{yTrue.Length / labelCount} samples; they must agree.",
                nameof(sampleWeight));
        }
    }

    /// <summary>The mean of the per-row values, weighted when weights are given.</summary>
    /// <remarks>
    /// <see cref="Weights.Mean"/> is where this lives, shared with the ordered-list metrics
    /// since #216. <c>LabelRankingAveragePrecision</c> does not call it: the reference
    /// divides by the weight sum directly there and returns <c>NaN</c>, which C# does too.
    /// </remarks>
    public static double Weighted(ReadOnlySpan<double> perRow, ReadOnlySpan<double> sampleWeight) =>
        Weights.Mean(perRow, sampleWeight);
}
