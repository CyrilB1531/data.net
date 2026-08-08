using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

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
        // Classes 1 and 2 both hold a NaN score, class 1 in an earlier column.
        // Sequential scoring meets class 1 first and names column 1's row; the
        // parallel path must name the same one however the workers are
        // scheduled, so an AggregateException or a race would fail here.
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

    [Fact]
    public void A_power_of_two_class_count_is_bit_identical_in_parallel()
    {
        // k=2 with n=10 is a shape where ArrayPool hands back Rent(10).Length * 2
        // == Rent(20).Length, so a span sliced to the rented length instead of the
        // sample count satisfies ScoreSource's length check and reads the wrong
        // column silently. The corpus cannot catch this: its class counts are 3
        // and 5, which do not collide.
        int[] yTrue = [0, 1, 0, 1, 0, 1, 0, 1, 0, 1];
        double[] scores = [0.9, 0.1, 0.2, 0.8, 0.7, 0.3, 0.4, 0.6, 0.55, 0.45,
                           0.35, 0.65, 0.85, 0.15, 0.25, 0.75, 0.6, 0.4, 0.3, 0.7];

        double sequential = RocAuc.MultiClass(yTrue, scores, 2);

        foreach (int workers in WorkerCounts)
        {
            double parallel = RocAuc.MultiClass(yTrue, scores, 2,
                new MultiClassRocOptions { MaxDegreeOfParallelism = workers });

            Assert.Equal(BitConverter.DoubleToInt64Bits(sequential), BitConverter.DoubleToInt64Bits(parallel));
        }
    }

    [Fact]
    public void Shifted_labels_are_bit_identical_in_parallel_too()
    {
        // The parallel body must pass the *label* where ClassScore wants a label
        // and the *column* where it wants a column. With labels 0..k-1 they are
        // the same number, so every other test in this file passes with the two
        // swapped. RocAucMultiClassTests has the sequential twin of this test,
        // which never sets MaxDegreeOfParallelism and so guarded only the
        // sequential driver — this one closes that gap on the parallel path.
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

    [Fact]
    public void An_early_failure_in_a_later_class_does_not_cancel_an_earlier_one()
    {
        // Hazard: stopping the loop on the first failure would cancel iterations
        // that had not started yet, and the caller would then be told about
        // whichever class a worker happened to reach first.
        //
        // Reports_the_lowest_offending_class_not_the_fastest_worker cannot catch
        // that, because its lowest failing class is 0 — the index the invoking
        // thread always begins with, so no cancellation can hide it. Here the
        // lowest failing class is 7, six full curves into the first worker's
        // range, while class 8 is the first thing a second worker touches and
        // fails on immediately. Two workers, so the ranges split at 8.
        //
        // The two NaNs sit in different rows on purpose: the message names the
        // row, so class 7's failure and class 8's failure are distinguishable.
        // n is large enough that a curve costs a sort worth measuring: the six
        // curves before class 7 are what class 8's worker has to beat for early
        // termination to skip class 7. At n=64 the whole loop was fast enough
        // that the invoking thread sometimes finished class 7 first and the
        // mutation survived.
        const int k = 16;
        const int n = 4096;
        (int[] yTrue, double[] scores) = NanColumns(n, k, (7, 5), (8, 3));

        ArgumentException sequential = Assert.Throws<ArgumentException>(
            () => RocAuc.MultiClass(yTrue, scores, k));
        ArgumentException parallel = Assert.Throws<ArgumentException>(
            () => RocAuc.MultiClass(yTrue, scores, k, new MultiClassRocOptions { MaxDegreeOfParallelism = 2 }));

        Assert.Equal("yScore[5] is NaN; scores must be numbers. (Parameter 'yScore')", sequential.Message);
        Assert.Equal(sequential.Message, parallel.Message);
        Assert.Equal(sequential.ParamName, parallel.ParamName);
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
