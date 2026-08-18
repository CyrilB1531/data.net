using Xunit;

// SonarLint S2245 / CA5394: a seeded Random builds a reproducible score column
// large enough to reach the radix path. Nothing here is a secret, and a
// cryptographic generator would make the corpus differ between runs, which is
// the one property these tests need it not to have.
#pragma warning disable S2245, CA5394

namespace Lodestar.Metrics.Tests;

/// <summary>
/// The scale the frozen corpus does not reach. <c>tests/oracles/roc_auc.json</c>
/// tops out at 400 samples and <c>BinaryRoc</c> switches to a radix sort at 8 192,
/// so every other test in this project exercises the comparison-sort path only —
/// green there says nothing about the radix one (#206).
/// </summary>
public sealed class RocAucRadixTests
{
    /// <summary>Just below the radix threshold, at it, and well past it.</summary>
    [Theory]
    [InlineData(8_191)]
    [InlineData(8_192)]
    [InlineData(20_000)]
    public void A_curve_agrees_with_a_comparison_sort_at_every_size(int n)
    {
        (int[] yTrue, double[] scores) = Sample(n, distinctScores: n / 50);

        Assert.Equal(Reference(yTrue, scores), RocAuc.Score(yTrue, scores), 12);
    }

    /// <summary>
    /// The same samples in a different order. The radix is stable and the introsort
    /// is not, so a tie group is summed in a different order by each — this pins
    /// that the grouping itself, which is what decides the curve, did not move.
    /// </summary>
    [Fact]
    public void Shuffling_the_samples_does_not_move_the_curve()
    {
        (int[] yTrue, double[] scores) = Sample(20_000, distinctScores: 200);
        double before = RocAuc.Score(yTrue, scores);

        var rng = new Random(7);
        for (int i = yTrue.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (yTrue[i], yTrue[j]) = (yTrue[j], yTrue[i]);
            (scores[i], scores[j]) = (scores[j], scores[i]);
        }

        Assert.Equal(before, RocAuc.Score(yTrue, scores), 12);
    }

    /// <summary>
    /// Negative and positive zero are one threshold to the curve — they compare equal
    /// as doubles — and two distinct bit patterns to the radix. Ordering by bits has
    /// to leave them adjacent, or the group splits and the curve changes.
    /// </summary>
    [Fact]
    public void Both_zeroes_stay_one_threshold()
    {
        (int[] yTrue, double[] scores) = Sample(20_000, distinctScores: 50);
        for (int i = 0; i < scores.Length; i += 2)
        {
            scores[i] = 0.0;
            scores[i + 1] = -0.0;
        }

        Assert.Equal(Reference(yTrue, scores), RocAuc.Score(yTrue, scores), 12);
    }

    /// <summary>Infinities are ordered, not rejected; the encoding has to carry them.</summary>
    [Fact]
    public void Infinities_sort_to_the_ends()
    {
        (int[] yTrue, double[] scores) = Sample(20_000, distinctScores: 500);
        scores[0] = double.PositiveInfinity;
        yTrue[0] = 1;
        scores[1] = double.NegativeInfinity;
        yTrue[1] = 0;

        Assert.Equal(Reference(yTrue, scores), RocAuc.Score(yTrue, scores), 12);
    }

    private static (int[] YTrue, double[] Scores) Sample(int n, int distinctScores)
    {
        var rng = new Random(20_260_818);
        int[] yTrue = new int[n];
        double[] scores = new double[n];
        for (int i = 0; i < n; i++)
        {
            yTrue[i] = rng.Next(2);

            // Positives drawn a little higher, so the curve is a real one rather
            // than the 0.5 an independent label would give whatever the sort did.
            int bucket = rng.Next(distinctScores) + (yTrue[i] == 1 ? distinctScores / 4 : 0);
            scores[i] = bucket / (double)distinctScores;
        }

        return (yTrue, scores);
    }

    /// <summary>
    /// The same trapezoid over a stable comparison sort — a second implementation
    /// rather than a second call, which is what makes this a check and not a tautology.
    /// </summary>
    private static double Reference(int[] yTrue, double[] scores)
    {
        int n = yTrue.Length;
        int[] order = new int[n];
        for (int i = 0; i < n; i++)
        {
            order[i] = i;
        }

        Array.Sort(order, (a, b) => scores[b].CompareTo(scores[a]));

        double truePositives = 0.0;
        double falsePositives = 0.0;
        double previousTrue = 0.0;
        double previousFalse = 0.0;
        double area = 0.0;

        for (int i = 0; i < n; i++)
        {
            int s = order[i];
            truePositives += yTrue[s] == 1 ? 1.0 : 0.0;
            falsePositives += yTrue[s] == 1 ? 0.0 : 1.0;

            bool lastOfGroup = i == n - 1 || !scores[order[i + 1]].Equals(scores[s]);
            if (!lastOfGroup)
            {
                continue;
            }

            area += (falsePositives - previousFalse) * (truePositives + previousTrue) * 0.5;
            previousTrue = truePositives;
            previousFalse = falsePositives;
        }

        return area / (truePositives * falsePositives);
    }
}
