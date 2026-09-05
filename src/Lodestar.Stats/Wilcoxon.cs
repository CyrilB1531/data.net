using Lodestar.Stats.Internal;

namespace Lodestar.Stats;

/// <summary>The Wilcoxon signed-rank test on paired measurements.</summary>
/// <remarks>
/// The rank-based counterpart to <see cref="TTest.Paired"/>. What it does with
/// a pair whose difference is exactly zero is not a detail but part of the
/// test's definition, which is why <see cref="ZeroMethod"/> is a parameter and
/// not a hidden convention.
/// </remarks>
public static class Wilcoxon
{
    // Measured against scipy 1.18.0: Auto is asymptotic outright once the
    // sample, before any zero is dropped, exceeds fifty values.
    private const int AutoAsymptoticThreshold = 50;

    // long-comment: 13 is a measured constant from scipy's own docstring
    // notes, not a guess, so the reasoning belongs beside it.
    // Ties or zeros change the null distribution, so the plain signed-rank
    // table (built for 1..n, no repeats) no longer answers exactly. scipy's
    // notes give the reason for 13 specifically: two to that power is under
    // the resample count an exhaustive permutation test defaults to, so the
    // result stays deterministic. Above it, the normal approximation takes
    // over instead.
    private const int AutoPermutationThreshold = 13;

    /// <summary>Compares two paired samples by the ranks of their differences.</summary>
    /// <param name="x">The first measurement of each pair.</param>
    /// <param name="y">The second measurement of each pair, in the same order.</param>
    /// <param name="zeroMethod">What to do with pairs whose difference is zero.</param>
    /// <param name="alternative">Which tail the p-value covers.</param>
    /// <param name="continuity">Whether the normal approximation gets the half-unit correction.</param>
    /// <param name="method">
    /// Exact, asymptotic, or chosen by sample size and by whether ties or zeros
    /// are present.
    /// </param>
    /// <returns>The smaller signed-rank sum, and the p-value.</returns>
    /// <exception cref="ArgumentException">
    /// The samples differ in length, or are empty.
    /// </exception>
    public static TestResult Paired(
        ReadOnlySpan<double> x,
        ReadOnlySpan<double> y,
        ZeroMethod zeroMethod = ZeroMethod.Wilcox,
        Alternative alternative = Alternative.TwoSided,
        Continuity continuity = Continuity.None,
        ExactMethod method = ExactMethod.Auto)
    {
        if (x.Length != y.Length)
        {
            throw new ArgumentException(
                $"A paired test needs the same number of values in both samples; got {x.Length} and {y.Length}.",
                nameof(y));
        }

        double[] differences = new double[x.Length];
        for (int i = 0; i < x.Length; i++)
        {
            differences[i] = x[i] - y[i];
        }

        return OneSample(differences, zeroMethod, alternative, continuity, method);
    }

    /// <summary>Compares a sample of differences against a median of zero.</summary>
    /// <param name="differences">The differences; at least one value.</param>
    /// <param name="zeroMethod">What to do with differences that are exactly zero.</param>
    /// <param name="alternative">Which tail the p-value covers.</param>
    /// <param name="continuity">Whether the normal approximation gets the half-unit correction.</param>
    /// <param name="method">Exact, asymptotic, or chosen by the number of non-zero differences.</param>
    /// <returns>The smaller signed-rank sum, and the p-value.</returns>
    /// <exception cref="ArgumentException"><paramref name="differences"/> is empty.</exception>
    public static TestResult OneSample(
        ReadOnlySpan<double> differences,
        ZeroMethod zeroMethod = ZeroMethod.Wilcox,
        Alternative alternative = Alternative.TwoSided,
        Continuity continuity = Continuity.None,
        ExactMethod method = ExactMethod.Auto)
    {
        if (differences.Length == 0)
        {
            throw new ArgumentException("The sample is empty.", nameof(differences));
        }

        // Wilcox drops the zeros before ranking; the other two rank them and
        // differ only in what they do with the ranks afterwards. S1244: a
        // difference of exactly zero is the sentinel the three rules disagree
        // on, not a value with a tolerance band.
#pragma warning disable S1244
        double[] ranked = zeroMethod == ZeroMethod.Wilcox
            ? [.. differences.ToArray().Where(d => d != 0.0)]
            : differences.ToArray();
#pragma warning restore S1244

        if (ranked.Length == 0)
        {
            // Every difference was zero: there is no evidence either way, and
            // scipy answers with a statistic of zero and a p-value of one.
            return new TestResult(0.0, 1.0);
        }

        double[] magnitudes = new double[ranked.Length];
        for (int i = 0; i < ranked.Length; i++)
        {
            magnitudes[i] = Math.Abs(ranked[i]);
        }

        double[] ranks = Ranks.Average(magnitudes);
        RankSums sums = ComputeRankSums(ranked, ranks, zeroMethod);
        double positive = sums.Positive;
        double negative = sums.Negative;
        double[] nonZeroRanks = sums.NonZeroRanks;

        double statistic = alternative == Alternative.TwoSided
            ? Math.Min(positive, negative)
            : positive;

        int zeroCount = CountZeros(differences);

        NullDistribution distribution = ChooseDistribution(
            method, differences.Length, Ranks.HasTies(magnitudes), zeroCount);

        // long-comment: this line picks one of two arrays and the choice looks
        // arbitrary without the corpus evidence behind it.
        // Pratt drops the zero group's ranks from the variance along with the
        // sums, since they never carry a random sign; Wilcox has already
        // dropped them from `ranks` itself. ZSplit does not: measured against
        // scipy 1.18.0, its asymptotic variance is the plain sum over every
        // rank, zero group included, even though that group's contribution is
        // a fixed half-and-half split rather than a random sign. The two
        // zsplit corpus cases agree with the full array, not with excluding it.
        double[] varianceRanks = zeroMethod == ZeroMethod.Pratt ? nonZeroRanks : ranks;

        double pValue = distribution switch
        {
            NullDistribution.Permutation =>
                PermutationPValue(nonZeroRanks, sums.RawPositive, sums.RawNegative, alternative),
            // ranked.Length, not nonZeroRanks.Length: scipy's own table size
            // is the full zero-method-processed length, zero group included.
            NullDistribution.Exact => ExactPValue(positive, ranked.Length, alternative),
            _ => AsymptoticPValue(positive, negative, varianceRanks, alternative, continuity),
        };

        return new TestResult(statistic, pValue);
    }

    private enum NullDistribution
    {
        Exact,
        Permutation,
        Asymptotic,
    }

    // Raw fields exclude zsplit's even-split addition: PermutationPValue needs
    // the sums before that fixed offset.
    private readonly record struct RankSums(
        double Positive, double Negative, double RawPositive, double RawNegative, double[] NonZeroRanks);

    // A zero's sign can never change which side of the sum it falls on, so the
    // raw sums and nonZeroRanks exclude the zero group entirely.
    private static RankSums ComputeRankSums(double[] ranked, double[] ranks, ZeroMethod zeroMethod)
    {
        double rawPositive = 0.0;
        double rawNegative = 0.0;
        double zeroRankSum = 0.0;
        double[] nonZeroRanks = new double[ranked.Length];
        int nonZeroCount = 0;
        for (int i = 0; i < ranked.Length; i++)
        {
            if (ranked[i] > 0.0)
            {
                rawPositive += ranks[i];
                nonZeroRanks[nonZeroCount++] = ranks[i];
            }
            else if (ranked[i] < 0.0)
            {
                rawNegative += ranks[i];
                nonZeroRanks[nonZeroCount++] = ranks[i];
            }
            else
            {
                zeroRankSum += ranks[i];
            }
        }

        Array.Resize(ref nonZeroRanks, nonZeroCount);

        double positive = rawPositive;
        double negative = rawNegative;
        if (zeroMethod == ZeroMethod.ZSplit)
        {
            positive += zeroRankSum / 2.0;
            negative += zeroRankSum / 2.0;
        }

        return new RankSums(positive, negative, rawPositive, rawNegative, nonZeroRanks);
    }

    // scipy's own n_zero check counts a difference of exactly zero on the
    // values as given, regardless of zero_method -- not a value with a
    // tolerance band, so S1244 does not apply here.
#pragma warning disable S1244
    private static int CountZeros(ReadOnlySpan<double> differences)
    {
        int count = 0;
        for (int i = 0; i < differences.Length; i++)
        {
            if (differences[i] == 0.0)
            {
                count++;
            }
        }

        return count;
    }
#pragma warning restore S1244

    // long-comment: this is the one place all three constants above meet, and
    // the four-way branch reads as arbitrary without the rule that orders it.
    // Measured against scipy 1.18.0: above fifty values, Auto is
    // unconditionally asymptotic. At or below that, it is exact only when free
    // of both ties and zeros -- either one breaks the plain signed-rank table,
    // because that table assumes the ranks 1..n appear without repeats. Short
    // of the asymptotic cutoff but still tied or zero-bearing, Auto falls to
    // the exhaustive permutation test below AutoPermutationThreshold, and to
    // the normal approximation above it.
    private static NullDistribution ChooseDistribution(
        ExactMethod method, int sampleLength, bool ties, int zeroCount)
    {
        if (method == ExactMethod.Exact)
        {
            return NullDistribution.Exact;
        }
        if (method == ExactMethod.Asymptotic || sampleLength > AutoAsymptoticThreshold)
        {
            return NullDistribution.Asymptotic;
        }
        if (!ties && zeroCount == 0)
        {
            return NullDistribution.Exact;
        }

        return sampleLength <= AutoPermutationThreshold
            ? NullDistribution.Permutation
            : NullDistribution.Asymptotic;
    }

    // The exact null distribution under ties, classical construction: every
    // sign of the actual ranks is equally likely, counted directly.
    private static double PermutationPValue(
        double[] contributingRanks, double rawPositive, double rawNegative, Alternative alternative)
    {
        double[] counts = SubsetSumCounts(contributingRanks);
        double total = Math.Pow(2.0, contributingRanks.Length);

        // Doubled to integers: mid-ranks are always whole or half numbers, so
        // this trades a tolerance comparison for an exact index.
        int positiveDoubled = (int)Math.Round(rawPositive * 2.0);
        int negativeDoubled = (int)Math.Round(rawNegative * 2.0);

        double atMostPositive = CumulativeAtMost(counts, positiveDoubled);
        double atMostNegative = CumulativeAtMost(counts, negativeDoubled);

        return alternative switch
        {
            Alternative.Less => atMostPositive / total,
            Alternative.Greater => atMostNegative / total,
            Alternative.TwoSided => Math.Min(
                1.0, 2.0 * Math.Min(atMostPositive, atMostNegative) / total),
            _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
        };
    }

    // The same product-of-(1 + x^rank) construction as
    // RankDistributions.SignedRankCounts, generalised past the fixed 1..n.
    private static double[] SubsetSumCounts(double[] contributingRanks)
    {
        int[] doubled = new int[contributingRanks.Length];
        int total = 0;
        for (int i = 0; i < contributingRanks.Length; i++)
        {
            doubled[i] = (int)Math.Round(contributingRanks[i] * 2.0);
            total += doubled[i];
        }

        double[] counts = new double[total + 1];
        counts[0] = 1.0;

        int reach = 0;
        for (int i = 0; i < doubled.Length; i++)
        {
            int v = doubled[i];
            reach += v;
            for (int w = reach; w >= v; w--)
            {
                counts[w] += counts[w - v];
            }
        }

        return counts;
    }

    private static double CumulativeAtMost(double[] counts, int doubledValue)
    {
        double sum = 0.0;
        for (int i = 0; i < counts.Length && i <= doubledValue; i++)
        {
            sum += counts[i];
        }

        return sum;
    }

    // long-comment: the choice to read only `positive`, rounded two different
    // ways by alternative, looks arbitrary without the source it comes from.
    // Measured against scipy 1.18.0 (gh-19872): the exact branch reads only
    // the positive rank sum, never the negative one, rounded toward whichever
    // tail it is about to ask for -- ceiling before a CDF, floor before a
    // survival function. Untied, rounding is a no-op and the negative sum
    // would have agreed by symmetry; under ties neither is true, which is
    // exactly what no corpus case exercised (task-6-report.md, fix round 1,
    // Finding 2).
    private static double ExactPValue(double positive, int n, Alternative alternative)
    {
        double[] counts = RankDistributions.SignedRankCounts(n);
        double total = Math.Pow(2.0, n);

        return alternative switch
        {
            Alternative.Less => RoundedCumulativeAtMost(counts, Math.Ceiling(positive)) / total,
            Alternative.Greater => SurvivalAtLeast(counts, Math.Floor(positive)) / total,
            Alternative.TwoSided => Math.Min(1.0, 2.0 * Math.Min(
                SurvivalAtLeast(counts, Math.Floor(positive)),
                RoundedCumulativeAtMost(counts, Math.Ceiling(positive))) / total),
            _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
        };
    }

    private static double RoundedCumulativeAtMost(double[] counts, double ceiling)
    {
        double sum = 0.0;
        for (int i = 0; i < counts.Length && i <= ceiling; i++)
        {
            sum += counts[i];
        }

        return sum;
    }

    private static double SurvivalAtLeast(double[] counts, double floor)
    {
        int start = Math.Max(0, (int)floor);
        double sum = 0.0;
        for (int i = start; i < counts.Length; i++)
        {
            sum += counts[i];
        }

        return sum;
    }

    private static double AsymptoticPValue(
        double positive,
        double negative,
        double[] ranks,
        Alternative alternative,
        Continuity continuity)
    {
        double total = positive + negative;
        double mean = total / 2.0;

        double squares = 0.0;
        for (int i = 0; i < ranks.Length; i++)
        {
            squares += ranks[i] * ranks[i];
        }

        // The variance is the sum of squared ranks over four, which reduces to
        // n(n+1)(2n+1)/24 only when the ranks are untied.
        double variance = squares / 4.0;
        double deviation = positive - mean;
        double correction = continuity == Continuity.Applied ? 0.5 : 0.0;

        double z = alternative switch
        {
            Alternative.Less => (deviation + correction) / Math.Sqrt(variance),
            Alternative.Greater => (deviation - correction) / Math.Sqrt(variance),
            Alternative.TwoSided => (Math.Abs(deviation) - correction) / Math.Sqrt(variance),
            _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
        };

        return alternative switch
        {
            // Sf(-z), not 1 - Sf(z): the far tail is exactly where 1 - (a
            // value near 1) cancels the bits a corpus case at 1e-14 needs.
            Alternative.Less => Normal.Sf(-z),
            Alternative.Greater => Normal.Sf(z),
            Alternative.TwoSided => Math.Min(1.0, 2.0 * Normal.Sf(z)),
            _ => throw new ArgumentOutOfRangeException(nameof(alternative), alternative, null),
        };
    }
}
