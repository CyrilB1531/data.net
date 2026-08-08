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
