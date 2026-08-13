using Xunit;

namespace DataNet.Metrics.Tests;

// SonarLint S2245 / CA5394: RandomArray and RandomSignedArray draw from the
// same seeded generator to build reproducible arrays for a differential test
// against a full-sort reference; no security use.
#pragma warning disable S2245, CA5394

/// <summary>
/// The unweighted median resolves through a partial quickselect instead of a
/// full sort (issue #92's performance follow-up — see
/// <c>docs/guides/performance.md</c>). This differential test is the proof
/// that the shortcut agrees with a full sort plus the same order-statistic
/// arithmetic on every input shape that could expose an off-by-one or a
/// partition bug. A fixed seed keeps the suite reproducible, following the
/// same convention as
/// <c>tests/DataNet.Text.Tests/Distances/LevenshteinPropertyTests.cs</c>.
/// </summary>
public sealed class WeightedPercentileMedianTests
{
    private const int Seed = 20260810;

    public static TheoryData<double[]> Cases()
    {
        var data = new TheoryData<double[]>();
        var rng = new Random(Seed);

        // Small, hand-picked shapes: every length from 1 to 5, odd and even,
        // sorted, reverse-sorted, duplicated and all-equal. An off-by-one in
        // the lower/upper index arithmetic shows up first here.
        foreach (double[] fixture in new[]
        {
            new double[] { 5.0 },
            new double[] { 1.0, 2.0 },
            new double[] { 2.0, 1.0 },
            new double[] { 3.0, 3.0 },
            new double[] { 1.0, 2.0, 3.0 },
            new double[] { 3.0, 2.0, 1.0 },
            new double[] { 4.0, 4.0, 4.0 },
            new double[] { 1.0, 1.0, 2.0 },
            new double[] { 1.0, 2.0, 3.0, 4.0 },
            new double[] { 4.0, 3.0, 2.0, 1.0 },
            new double[] { 5.0, 5.0, 5.0, 5.0 },
            new double[] { 1.0, 1.0, 2.0, 2.0 },
            new double[] { 1.0, 2.0, 3.0, 4.0, 5.0 },
            new double[] { 5.0, 4.0, 3.0, 2.0, 1.0 },
            new double[] { 7.0, 7.0, 7.0, 7.0, 7.0 },
            new double[] { 1.0, 1.0, 1.0, 2.0, 2.0 },
            // Negative-valued: PerOutput feeds these through Math.Abs(value - 0)
            // before WeightedPercentile ever sees them, and ReferenceMedian
            // mirrors that with its own Math.Abs, so these two rows are what
            // proves the reference actually tracks the abs transform rather
            // than agreeing with it by accident, the way an all-non-negative
            // suite would.
            new double[] { -3.0, -1.0, -2.0 },
            new double[] { -1.0, 2.0, -3.0, 4.0 },
        })
        {
            data.Add(fixture);
        }

        // QuickSelect's insertion cutoff sits at width 12: cases either side
        // of it, plus one right on it.
        foreach (int n in new[] { 11, 12, 13, 14 })
        {
            data.Add(RandomArray(rng, n));
        }

        // A larger shape with negative values too, so the abs transform is
        // exercised past the insertion cutoff and not just on hand-picked
        // small arrays.
        data.Add(RandomSignedArray(rng, 257));

        // Larger random shapes, with repeats frequent enough that the
        // Lomuto partition sees ties often.
        foreach (int n in new[] { 25, 50, 137, 501, 1000, 4096 })
        {
            data.Add(RandomArray(rng, n));
        }

        // Shapes that specifically target the introselect fallback:
        // already-sorted, reverse-sorted and all-equal defeat (or nearly
        // defeat) a median-of-three pivot at sizes well past the insertion
        // cutoff, which is exactly what the log2(n) partitioning budget
        // exists to bound.
        foreach (int n in new[] { 500, 2000 })
        {
            data.Add(SortedArray(n, descending: false));
            data.Add(SortedArray(n, descending: true));
            data.Add(AllEqualArray(n));
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Cases))]
    public void Matches_a_full_sort_plus_the_same_order_statistics(double[] values)
    {
        double[] zeros = new double[values.Length];

        // MedianAbsoluteError.PerOutput is the public entry point that
        // reaches WeightedPercentile.Median with an empty weight array —
        // the internal type itself is not visible from this project.
        double actual = MedianAbsoluteError.PerOutput(values, zeros)[0];
        double expected = ReferenceMedian(values);

        Assert.Equal(expected, actual, 12);
    }

    /// <summary>
    /// The inputs a partition scheme gets wrong, on a size large enough to pass the
    /// insertion cutoff and exercise the introselect loop rather than the sort
    /// fallback. Written against the branchy partition and expected to pass there:
    /// they exist to catch what a rewrite of the index arithmetic would break, and
    /// a test added after a change cannot do that.
    ///
    /// Not all five shapes guard a wrong rank equally. "already sorted", "reverse
    /// sorted" and "organ pipe" hold every value distinct at the two selected
    /// ranks (organ pipe's ranks land either side of its one tied pair), so any
    /// off-by-one there changes the result: full teeth. "two distinct values" is
    /// half zeros and half ones, so it only catches a rank shifted across that
    /// boundary — a shift that stays inside one run returns the same value.
    /// "all equal" cannot detect a wrong rank at all: every element is 3.0, so no
    /// index error changes the result. It stays in the theory anyway because both
    /// degenerate shapes still defeat a median-of-three pivot, which is where a
    /// rewritten loop would hang or exhaust the introselect budget rather than
    /// return a wrong number — a failure mode distinct from, and not covered by,
    /// the three rank-guarding shapes.
    /// </summary>
    [Theory]
    [InlineData("all equal")]
    [InlineData("already sorted")]
    [InlineData("reverse sorted")]
    [InlineData("two distinct values")]
    [InlineData("organ pipe")]
    public void The_median_is_right_on_the_shapes_that_break_a_partition(string shape)
    {
        const int Samples = 5_000;
        double[] yTrue = new double[Samples];
        double[] yPred = new double[Samples];
        for (int i = 0; i < Samples; i++)
        {
            double residual = shape switch
            {
                "all equal" => 3.0,
                "already sorted" => i,
                "reverse sorted" => Samples - i,
                "two distinct values" => i % 2 == 0 ? 0.0 : 1.0,
                "organ pipe" => i < Samples / 2 ? i : Samples - i,
                _ => throw new ArgumentOutOfRangeException(nameof(shape), shape, "no such shape"),
            };
            yTrue[i] = residual;
        }

        // MedianAbsoluteError.PerOutput is the public entry point used above,
        // for the same reason: the internal WeightedPercentile type is not
        // visible from this project.
        double actual = MedianAbsoluteError.PerOutput(yTrue, yPred)[0];

        Assert.Equal(ExpectedMedian(yTrue), actual, 12);
    }

    /// <summary>
    /// The median by the definition, computed by sorting a copy — independent of the
    /// selection under test, which is the point.
    /// </summary>
    private static double ExpectedMedian(double[] residuals)
    {
        double[] sorted = (double[])residuals.Clone();
        Array.Sort(sorted);
        int n = sorted.Length;
        return n % 2 == 1 ? sorted[n / 2] : (sorted[(n / 2) - 1] + sorted[n / 2]) / 2.0;
    }

    /// <summary>
    /// The reference implementation: take the same absolute value
    /// <c>MedianAbsoluteError.PerOutput(values, zeros)</c> computes internally
    /// (<c>Math.Abs(yTrue - yPred)</c> with <c>yPred</c> all zero), sort that
    /// fully, then apply the exact index arithmetic
    /// <c>WeightedPercentile.Average</c> uses for uniform weights.
    /// </summary>
    private static double ReferenceMedian(double[] values)
    {
        double[] sorted = new double[values.Length];
        for (int i = 0; i < values.Length; i++)
        {
            sorted[i] = Math.Abs(values[i]);
        }

        Array.Sort(sorted);

        int n = sorted.Length;
        int lower = (n - 1) / 2;
        int upper = n / 2;
        return (sorted[lower] + sorted[upper]) / 2.0;
    }

    private static double[] RandomArray(Random rng, int n)
    {
        var values = new double[n];
        for (int i = 0; i < n; i++)
        {
            // A narrow integral range forces frequent duplicates, which is
            // exactly the shape that stresses a Lomuto partition's ties.
            values[i] = rng.Next(0, 20);
        }

        return values;
    }

    private static double[] RandomSignedArray(Random rng, int n)
    {
        var values = new double[n];
        for (int i = 0; i < n; i++)
        {
            values[i] = rng.Next(-20, 20);
        }

        return values;
    }

    private static double[] SortedArray(int n, bool descending)
    {
        var values = new double[n];
        for (int i = 0; i < n; i++)
        {
            values[i] = descending ? n - i : i;
        }

        return values;
    }

    private static double[] AllEqualArray(int n)
    {
        var values = new double[n];
        Array.Fill(values, 42.0);
        return values;
    }
}
