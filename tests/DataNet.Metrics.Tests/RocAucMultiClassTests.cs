using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class RocAucMultiClassTests
{
    [Theory]
    [MemberData(nameof(RocCorpus.MulticlassIndices), MemberType = typeof(RocCorpus))]
    public void Matches_sklearn_multiclass_roc_auc_score(int index)
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

            double actual = RocAuc.MultiClass(yTrue, scores, classCount, new MultiClassRocOptions
            {
                Strategy = strategy,
                Average = average,
                SampleWeight = weight,
            });

            Assert.True(Math.Abs(entry.Value.GetDouble() - actual) < MetricsCorpus.Tolerance,
                $"{RocCorpus.Describe(c)} {entry.Name}: expected {entry.Value.GetDouble()}, got {actual}");
        }
    }

    [Fact]
    public void Rejects_one_vs_one_with_sample_weights_as_sklearn_does()
    {
        int[] yTrue = [0, 1, 2, 0, 1, 2];
        double[] scores = Rows([[0.6, 0.2, 0.2], [0.2, 0.6, 0.2], [0.2, 0.2, 0.6],
                                [0.5, 0.3, 0.2], [0.3, 0.5, 0.2], [0.1, 0.2, 0.7]]);
        double[] weight = [1, 2, 1, 1, 2, 1];

        Assert.Throws<ArgumentException>(() => RocAuc.MultiClass(yTrue, scores, 3, new MultiClassRocOptions
        {
            Strategy = MultiClassStrategy.OneVsOne,
            SampleWeight = weight,
        }));
    }

    [Fact]
    public void Rejects_rows_that_do_not_sum_to_one()
    {
        int[] yTrue = [0, 1, 2];
        double[] scores = Rows([[0.6, 0.2, 0.1], [0.2, 0.6, 0.2], [0.2, 0.2, 0.6]]);

        Assert.Throws<ArgumentException>(() => RocAuc.MultiClass(yTrue, scores, 3));
    }

    [Fact]
    public void Rejects_a_span_whose_length_is_not_a_multiple_of_the_class_count()
    {
        int[] yTrue = [0, 1, 2];
        double[] scores = [0.5, 0.5, 0.5, 0.5];

        Assert.Throws<ArgumentException>(() => RocAuc.MultiClass(yTrue, scores, 3));
    }

    [Fact]
    public void Rejects_micro_averaging()
    {
        int[] yTrue = [0, 1, 2];
        double[] scores = Rows([[0.6, 0.2, 0.2], [0.2, 0.6, 0.2], [0.2, 0.2, 0.6]]);

        Assert.Throws<ArgumentException>(() => RocAuc.MultiClass(
            yTrue, scores, 3, new MultiClassRocOptions { Average = Averaging.Micro }));
    }

    [Fact]
    public void Rejects_unsorted_labels()
    {
        int[] yTrue = [0, 1, 2];
        double[] scores = Rows([[0.6, 0.2, 0.2], [0.2, 0.6, 0.2], [0.2, 0.2, 0.6]]);
        int[] labels = [2, 0, 1];

        Assert.Throws<ArgumentException>(() => RocAuc.MultiClass(
            yTrue, scores, 3, new MultiClassRocOptions { Labels = labels }));
    }

    [Fact]
    public void Rejects_empty_input()
    {
        int[] yTrue = [];
        double[] scores = [];

        Assert.Throws<ArgumentException>(() => RocAuc.MultiClass(yTrue, scores, 3));
    }

    [Fact]
    public void Rejects_fewer_than_two_classes()
    {
        int[] yTrue = [0, 0, 0];
        double[] scores = [1.0, 1.0, 1.0];

        Assert.Throws<ArgumentOutOfRangeException>(() => RocAuc.MultiClass(yTrue, scores, 1));
    }

    [Fact]
    public void Rejects_a_sample_weight_whose_length_disagrees_with_y_true()
    {
        int[] yTrue = [0, 1, 2];
        double[] scores = Rows([[0.6, 0.2, 0.2], [0.2, 0.6, 0.2], [0.2, 0.2, 0.6]]);
        double[] weight = [1, 2];

        Assert.Throws<ArgumentException>(() => RocAuc.MultiClass(
            yTrue, scores, 3, new MultiClassRocOptions { SampleWeight = weight }));
    }

    [Fact]
    public void Rejects_a_class_count_that_y_true_does_not_realise_when_labels_are_omitted()
    {
        int[] yTrue = [0, 0, 0];
        double[] scores = Rows([[0.6, 0.2, 0.2], [0.2, 0.6, 0.2], [0.2, 0.2, 0.6]]);

        Assert.Throws<ArgumentException>(() => RocAuc.MultiClass(yTrue, scores, 3));
    }

    [Fact]
    public void Rejects_labels_whose_count_disagrees_with_class_count()
    {
        int[] yTrue = [0, 1, 2];
        double[] scores = Rows([[0.6, 0.2, 0.2], [0.2, 0.6, 0.2], [0.2, 0.2, 0.6]]);
        int[] labels = [0, 1];

        Assert.Throws<ArgumentException>(() => RocAuc.MultiClass(
            yTrue, scores, 3, new MultiClassRocOptions { Labels = labels }));
    }

    [Fact]
    public void Default_options_reproduce_one_vs_rest_macro_exactly()
    {
        int[] yTrue = [0, 1, 2, 2, 1, 0];
        double[] scores = Rows([[0.70, 0.20, 0.10], [0.10, 0.60, 0.30], [0.15, 0.25, 0.60],
                                [0.20, 0.20, 0.60], [0.30, 0.50, 0.20], [0.55, 0.30, 0.15]]);

        double implicitDefaults = RocAuc.MultiClass(yTrue, scores, 3);
        double spelledOut = RocAuc.MultiClass(yTrue, scores, 3, new MultiClassRocOptions
        {
            Strategy = MultiClassStrategy.OneVsRest,
            Average = Averaging.Macro,
        });

        Assert.Equal(BitConverter.DoubleToInt64Bits(spelledOut), BitConverter.DoubleToInt64Bits(implicitDefaults));
    }

    [Fact]
    public void Zero_and_one_workers_are_the_same_sequential_path()
    {
        int[] yTrue = [0, 1, 2, 2, 1, 0];
        double[] scores = Rows([[0.70, 0.20, 0.10], [0.10, 0.60, 0.30], [0.15, 0.25, 0.60],
                                [0.20, 0.20, 0.60], [0.30, 0.50, 0.20], [0.55, 0.30, 0.15]]);

        double zero = RocAuc.MultiClass(yTrue, scores, 3, new MultiClassRocOptions { MaxDegreeOfParallelism = 0 });
        double one = RocAuc.MultiClass(yTrue, scores, 3, new MultiClassRocOptions { MaxDegreeOfParallelism = 1 });

        Assert.Equal(BitConverter.DoubleToInt64Bits(one), BitConverter.DoubleToInt64Bits(zero));
    }

    [Fact]
    public void Rejects_a_negative_degree_of_parallelism()
    {
        int[] yTrue = [0, 1, 2];
        double[] scores = Rows([[0.6, 0.2, 0.2], [0.2, 0.6, 0.2], [0.2, 0.2, 0.6]]);

        Assert.Throws<ArgumentOutOfRangeException>(() => RocAuc.MultiClass(
            yTrue, scores, 3, new MultiClassRocOptions { MaxDegreeOfParallelism = -1 }));
    }

    [Fact]
    public void Rejects_binary_averaging_even_though_it_is_the_enum_default()
    {
        int[] yTrue = [0, 1, 2];
        double[] scores = Rows([[0.6, 0.2, 0.2], [0.2, 0.6, 0.2], [0.2, 0.2, 0.6]]);

        Assert.Throws<ArgumentException>(() => RocAuc.MultiClass(
            yTrue, scores, 3, new MultiClassRocOptions { Average = Averaging.Binary }));
    }

    private static double[] Rows(double[][] rows) => [.. rows.SelectMany(r => r)];
}
