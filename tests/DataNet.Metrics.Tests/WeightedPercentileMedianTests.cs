using Xunit;

namespace DataNet.Metrics.Tests;

// SonarLint S2245: a seeded Random builds reproducible arrays for a
// differential test against a full-sort reference; no security use.
#pragma warning disable S2245

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
    /// The reference implementation: sort fully, then apply the exact index
    /// arithmetic <c>WeightedPercentile.Average</c> uses for uniform weights.
    /// </summary>
    private static double ReferenceMedian(double[] values)
    {
        double[] sorted = (double[])values.Clone();
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
