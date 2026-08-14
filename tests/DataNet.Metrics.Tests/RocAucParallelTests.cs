using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

// SonarLint S2245 / CA5394: a seeded Random builds a reproducible six-class score
// matrix for a bit-identity fixture; no security use.
#pragma warning disable S2245, CA5394

/// <summary>
/// The guarantee issue #86 rests on: parallelising the per-class and per-pair
/// loops must not move a single bit. Not "within 1e-9" — identically. Every
/// class writes its own slot and the averaging runs afterwards on the calling
/// thread in array order, so if a value moves, the parallelisation is unsound
/// and the change is wrong.
/// </summary>
public sealed class RocAucParallelTests
{
    private static readonly int[] WorkerCounts = [2, 3, 8];

    [Theory]
    [MemberData(nameof(RocCorpus.MulticlassIndices), MemberType = typeof(RocCorpus))]
    public void Replays_the_frozen_corpus_bit_identically_in_parallel(int index)
    {
        JsonElement c = RocCorpus.Cases[index];
        int[] yTrue = RocCorpus.YTrue(c);
        double[] scores = RocCorpus.RowMajorScores(c);
        double[] weight = RocCorpus.SampleWeight(c);
        int classCount = c.GetProperty("class_count").GetInt32();

        foreach (JsonProperty entry in c.GetProperty("values").EnumerateObject())
        {
            string[] parts = entry.Name.Split('|');
            MultiClassStrategy strategy = parts[0] == "ovr"
                ? MultiClassStrategy.OneVsRest
                : MultiClassStrategy.OneVsOne;
            Averaging average = parts[1] == "macro" ? Averaging.Macro : Averaging.Weighted;

            double sequential = RocAuc.MultiClass(yTrue, scores, classCount, new MultiClassRocOptions
            {
                Strategy = strategy,
                Average = average,
                SampleWeight = weight,
            });

            foreach (int workers in WorkerCounts)
            {
                double parallel = RocAuc.MultiClass(yTrue, scores, classCount, new MultiClassRocOptions
                {
                    Strategy = strategy,
                    Average = average,
                    SampleWeight = weight,
                    MaxDegreeOfParallelism = workers,
                });

                Assert.Equal(
                    BitConverter.DoubleToInt64Bits(sequential),
                    BitConverter.DoubleToInt64Bits(parallel));
            }
        }
    }

    [Fact]
    public void Reports_the_lowest_offending_class_not_the_fastest_worker()
    {
        // Classes 1 and 2 both hold NaN, class 1 earlier: the parallel path must
        // still name class 1 however workers are scheduled.
        int[] yTrue = [0, 1, 2, 0, 1, 2];
        double[] scores =
        [
            0.5, 0.3, 0.2,
            0.2, double.NaN, 0.3,
            0.1, 0.2, double.NaN,
            0.6, 0.2, 0.2,
            0.2, double.NaN, 0.3,
            0.1, 0.3, double.NaN,
        ];

        ArgumentException sequential = Assert.Throws<ArgumentException>(
            () => RocAuc.MultiClass(yTrue, scores, 3));
        ArgumentException parallel = Assert.Throws<ArgumentException>(
            () => RocAuc.MultiClass(yTrue, scores, 3, new MultiClassRocOptions { MaxDegreeOfParallelism = 8 }));

        Assert.Equal(sequential.Message, parallel.Message);
        Assert.Equal(sequential.ParamName, parallel.ParamName);
    }

    [Fact]
    public void A_class_absent_from_y_true_throws_the_same_way_in_parallel()
    {
        int[] yTrue = [0, 0, 1, 1];
        double[] scores = [0.9, 0.05, 0.05, 0.8, 0.1, 0.1, 0.1, 0.8, 0.1, 0.2, 0.7, 0.1];
        int[] labels = [0, 1, 2];

        ArgumentException sequential = Assert.Throws<ArgumentException>(
            () => RocAuc.MultiClass(yTrue, scores, 3, new MultiClassRocOptions { Labels = labels }));
        ArgumentException parallel = Assert.Throws<ArgumentException>(
            () => RocAuc.MultiClass(yTrue, scores, 3, new MultiClassRocOptions
            {
                Labels = labels,
                MaxDegreeOfParallelism = 8,
            }));

        Assert.Equal(sequential.Message, parallel.Message);
        Assert.Equal(sequential.ParamName, parallel.ParamName);
    }

    /// <summary>
    /// k=2, n=10 is the <c>ArrayPool</c> collision ADR 0018's <c>ScoreSource</c>
    /// section measures (<c>Rent(10).Length * 2 == Rent(20).Length</c>): the
    /// corpus's class counts, 3 and 5, do not collide, so only this fixture would
    /// catch a span sliced to the rented length reading the wrong column. Both
    /// strategies, because k=2 is also the only shape giving
    /// <c>OneVsOneParallel</c> a single pair, collapsing <c>Math.Min(workers,
    /// count)</c> to one worker regardless of what the caller asked for.
    /// </summary>
    [Fact]
    public void A_power_of_two_class_count_is_bit_identical_in_parallel()
    {
        int[] yTrue = [0, 1, 0, 1, 0, 1, 0, 1, 0, 1];
        double[] scores = [0.9, 0.1, 0.2, 0.8, 0.7, 0.3, 0.4, 0.6, 0.55, 0.45,
                           0.35, 0.65, 0.85, 0.15, 0.25, 0.75, 0.6, 0.4, 0.3, 0.7];

        foreach (MultiClassStrategy strategy in new[] { MultiClassStrategy.OneVsRest, MultiClassStrategy.OneVsOne })
        {
            double sequential = RocAuc.MultiClass(yTrue, scores, 2,
                new MultiClassRocOptions { Strategy = strategy });

            foreach (int workers in WorkerCounts)
            {
                double parallel = RocAuc.MultiClass(yTrue, scores, 2, new MultiClassRocOptions
                {
                    Strategy = strategy,
                    MaxDegreeOfParallelism = workers,
                });

                Assert.Equal(BitConverter.DoubleToInt64Bits(sequential), BitConverter.DoubleToInt64Bits(parallel));
            }
        }
    }

    /// <summary>
    /// The parallel body must pass the <em>label</em> where <c>ClassScore</c>
    /// wants a label and the <em>column</em> where it wants a column. With labels
    /// 0..k-1 they are the same number, so every other test in this file passes
    /// with the two swapped. <see cref="RocAucMultiClassTests"/>'s sequential
    /// twin never sets <c>MaxDegreeOfParallelism</c>, so it guarded only the
    /// sequential driver — this one closes the gap on the parallel path.
    /// </summary>
    [Fact]
    public void Shifted_labels_are_bit_identical_in_parallel_too()
    {
        int[] shifted = [10, 20, 30, 30, 20, 10];
        double[] scores =
        [
            0.70, 0.20, 0.10,
            0.10, 0.60, 0.30,
            0.15, 0.25, 0.60,
            0.20, 0.20, 0.60,
            0.30, 0.50, 0.20,
            0.55, 0.30, 0.15,
        ];
        int[] labels = [10, 20, 30];

        foreach (MultiClassStrategy strategy in new[] { MultiClassStrategy.OneVsRest, MultiClassStrategy.OneVsOne })
        {
            double sequential = RocAuc.MultiClass(shifted, scores, 3, new MultiClassRocOptions
            {
                Strategy = strategy,
                Labels = labels,
            });

            foreach (int workers in WorkerCounts)
            {
                double parallel = RocAuc.MultiClass(shifted, scores, 3, new MultiClassRocOptions
                {
                    Strategy = strategy,
                    Labels = labels,
                    MaxDegreeOfParallelism = workers,
                });

                Assert.Equal(
                    BitConverter.DoubleToInt64Bits(sequential),
                    BitConverter.DoubleToInt64Bits(parallel));
            }
        }
    }

    /// <summary>
    /// ADR 0018's "does not stop early" hazard: a naive <c>Stop</c> would cancel
    /// unstarted iterations and report whichever class a worker reached first.
    /// <see cref="Reports_the_lowest_offending_class_not_the_fastest_worker"/>
    /// cannot catch that — its lowest failing class, 1, is one trivial class into
    /// the invoking thread's own range, so no cancellation could hide it. Here
    /// class 7 fails six full curves in, and class 8 (the second worker's first
    /// touch) fails immediately; at n=64 the loop was fast enough that the
    /// invoking thread sometimes reached class 7 first and the mutation survived.
    /// </summary>
    [Fact]
    public void An_early_failure_in_a_later_class_does_not_cancel_an_earlier_one()
    {
        const int k = 16;
        const int n = 4096;
        (int[] yTrue, double[] scores) = NanColumns(n, k, (7, 5), (8, 3));

        ArgumentException sequential = Assert.Throws<ArgumentException>(
            () => RocAuc.MultiClass(yTrue, scores, k));
        ArgumentException parallel = Assert.Throws<ArgumentException>(
            () => RocAuc.MultiClass(yTrue, scores, k, new MultiClassRocOptions { MaxDegreeOfParallelism = 2 }));

        // Up to the suffix only: the "(Parameter 'x')" tail is a localizable
        // CoreLib addition (RocAucBinaryTests documents the convention).
        Assert.StartsWith("yScore[5] is NaN; scores must be numbers.", sequential.Message, StringComparison.Ordinal);
        Assert.Equal("yScore", sequential.ParamName);
        Assert.Equal(sequential.Message, parallel.Message);
        Assert.Equal(sequential.ParamName, parallel.ParamName);
    }

    /// <summary>
    /// The one-vs-one twin of
    /// <see cref="Reports_the_lowest_offending_class_not_the_fastest_worker"/>:
    /// every other error-path test here drives <c>OneVsRestParallel</c>, so none
    /// ever pushed a failure through <c>OneVsOneParallel</c>'s own handler before.
    /// k=4, NaN at (row 3, col 2) and (row 5, col 3): yTrue[i] is i % 4, so pair
    /// 5=(2,3) reaches the first and pair 4=(1,3) the second, making 4 the lowest
    /// offending pair — not pair 0, the thread's own starting iteration.
    /// </summary>
    [Fact]
    public void Reports_the_lowest_offending_pair_not_the_fastest_worker()
    {
        const int k = 4;
        const int n = 64;
        (int[] yTrue, double[] scores) = NanColumns(n, k, (2, 3), (3, 5));

        ArgumentException sequential = Assert.Throws<ArgumentException>(
            () => RocAuc.MultiClass(yTrue, scores, k, new MultiClassRocOptions
            {
                Strategy = MultiClassStrategy.OneVsOne,
            }));

        // Pair 4's message, not pair 5's: the NaN lands at index 2 of pair 4's
        // compacted column, naming the index rather than just "a pair won".
        Assert.StartsWith("yScore[2] is NaN; scores must be numbers.", sequential.Message, StringComparison.Ordinal);
        Assert.Equal("yScore", sequential.ParamName);

        foreach (int workers in WorkerCounts)
        {
            // Exact-type match: an AggregateException reaching the caller fails
            // here instead of passing via a base-class match.
            ArgumentException parallel = Assert.Throws<ArgumentException>(
                () => RocAuc.MultiClass(yTrue, scores, k, new MultiClassRocOptions
                {
                    Strategy = MultiClassStrategy.OneVsOne,
                    MaxDegreeOfParallelism = workers,
                }));

            Assert.Equal(sequential.Message, parallel.Message);
            Assert.Equal(sequential.ParamName, parallel.ParamName);
        }
    }

    /// <summary>
    /// An <paramref name="n"/> by <paramref name="k"/> probability matrix whose
    /// rows sum to 1, with a NaN planted at each given (column, row).
    /// </summary>
    private static (int[] YTrue, double[] Scores) NanColumns(int n, int k, params (int Column, int Row)[] nans)
    {
        int[] yTrue = new int[n];
        double[] scores = new double[n * k];
        double rest = 0.5 / (k - 1);

        for (int i = 0; i < n; i++)
        {
            yTrue[i] = i % k;
            for (int c = 0; c < k; c++)
            {
                scores[(i * k) + c] = c == yTrue[i] ? 0.5 : rest;
            }
        }

        foreach ((int column, int row) in nans)
        {
            scores[(row * k) + column] = double.NaN;
        }

        return (yTrue, scores);
    }

    [Fact]
    public void One_vs_one_over_six_classes_is_bit_identical_in_parallel()
    {
        // 15 pairs and 30 curves, more pairs than workers and more workers than
        // any single pair needs: the shape where a per-pair race would show.
        const int k = 6;
        const int n = 240;
        int[] yTrue = new int[n];
        double[] scores = new double[n * k];
        var random = new Random(20260808);

        for (int i = 0; i < n; i++)
        {
            yTrue[i] = i % k;
            double total = 0.0;
            for (int c = 0; c < k; c++)
            {
                double draw = random.NextDouble() + (c == yTrue[i] ? 0.75 : 0.0);
                scores[(i * k) + c] = draw;
                total += draw;
            }
            for (int c = 0; c < k; c++)
            {
                scores[(i * k) + c] /= total;
            }
        }

        foreach (Averaging average in new[] { Averaging.Macro, Averaging.Weighted })
        {
            double sequential = RocAuc.MultiClass(yTrue, scores, k, new MultiClassRocOptions
            {
                Strategy = MultiClassStrategy.OneVsOne,
                Average = average,
            });

            // Bit equality alone would hold if both paths degenerated to the same
            // NaN; pin the value to a separable problem's band first.
            Assert.InRange(sequential, 0.5, 1.0);

            foreach (int workers in WorkerCounts)
            {
                double parallel = RocAuc.MultiClass(yTrue, scores, k, new MultiClassRocOptions
                {
                    Strategy = MultiClassStrategy.OneVsOne,
                    Average = average,
                    MaxDegreeOfParallelism = workers,
                });

                Assert.Equal(
                    BitConverter.DoubleToInt64Bits(sequential),
                    BitConverter.DoubleToInt64Bits(parallel));
            }
        }
    }

    [Fact]
    public void More_workers_than_classes_is_not_an_error()
    {
        int[] yTrue = [0, 1, 0, 1];
        double[] scores = [0.9, 0.1, 0.2, 0.8, 0.7, 0.3, 0.4, 0.6];

        double sequential = RocAuc.MultiClass(yTrue, scores, 2);
        double parallel = RocAuc.MultiClass(yTrue, scores, 2,
            new MultiClassRocOptions { MaxDegreeOfParallelism = 64 });

        Assert.Equal(BitConverter.DoubleToInt64Bits(sequential), BitConverter.DoubleToInt64Bits(parallel));
    }
}
