using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

/// <summary>Hamming, zero-one and Jaccard against the frozen corpus.</summary>
public sealed class LabelLossTests
{
    private static readonly JsonDocument Document = OracleLoader.Load("label_losses.json");

    private static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("cases").EnumerateArray()];

    private static JsonElement Multilabel => Document.RootElement.GetProperty("multilabel");

    private static JsonElement Undefined => Document.RootElement.GetProperty("undefined");

    public static TheoryData<int> Indices()
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Cases.Count; i++)
        {
            data.Add(i);
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void Matches_sklearn_on_every_case(int index)
    {
        JsonElement c = Cases[index];
        int[] yTrue = MetricsCorpus.Ints(c, "y_true");
        int[] yPred = MetricsCorpus.Ints(c, "y_pred");
        double[] weight = MetricsCorpus.OptionalDoubles(c, "sample_weight");

        Assert.Equal(c.GetProperty("hamming").GetDouble(),
                     HammingLoss.Score(yTrue, yPred, weight), MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("zero_one").GetDouble(),
                     ZeroOneLoss.Score(yTrue, yPred, true, weight), MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("zero_one_count").GetDouble(),
                     ZeroOneLoss.Score(yTrue, yPred, false, weight), MetricsCorpus.Tolerance);

        foreach ((string name, Averaging averaging) in new[]
        {
            ("jaccard_macro", Averaging.Macro),
            ("jaccard_micro", Averaging.Micro),
            ("jaccard_weighted", Averaging.Weighted),
        })
        {
            Assert.Equal(c.GetProperty(name).GetDouble(),
                         JaccardScore.Score(yTrue, yPred, averaging, 1, ZeroDivision.Zero, default, weight),
                         MetricsCorpus.Tolerance);
        }

        double[] expected = MetricsCorpus.Doubles(c, "jaccard_per_class");
        double[] actual = JaccardScore.PerClass(yTrue, yPred, ZeroDivision.Zero, default, weight);
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i], MetricsCorpus.Tolerance);
        }
    }

    [Fact]
    public void Matches_sklearn_on_a_label_matrix()
    {
        bool[] yTrue = MetricsCorpus.Bools(Multilabel, "y_true");
        bool[] yPred = MetricsCorpus.Bools(Multilabel, "y_pred");
        double[] weight = MetricsCorpus.Doubles(Multilabel, "sample_weight");
        int labels = Multilabel.GetProperty("label_count").GetInt32();

        Assert.Equal(Multilabel.GetProperty("hamming").GetDouble(),
                     HammingLoss.Score(yTrue, yPred, labels), MetricsCorpus.Tolerance);
        Assert.Equal(Multilabel.GetProperty("hamming_weighted").GetDouble(),
                     HammingLoss.Score(yTrue, yPred, labels, weight), MetricsCorpus.Tolerance);
        Assert.Equal(Multilabel.GetProperty("zero_one").GetDouble(),
                     ZeroOneLoss.Score(yTrue, yPred, labels), MetricsCorpus.Tolerance);
        Assert.Equal(Multilabel.GetProperty("zero_one_count").GetDouble(),
                     ZeroOneLoss.Score(yTrue, yPred, labels, false), MetricsCorpus.Tolerance);
        Assert.Equal(Multilabel.GetProperty("zero_one_weighted").GetDouble(),
                     ZeroOneLoss.Score(yTrue, yPred, labels, true, weight), MetricsCorpus.Tolerance);
    }

    // The two agree on single-label input and part company on a matrix, which is the
    // one thing about this pair worth knowing.
    [Fact]
    public void Hamming_and_zero_one_agree_on_labels_and_disagree_on_a_matrix()
    {
        int[] yTrue = [0, 1, 2, 1];
        int[] yPred = [0, 2, 2, 1];
        Assert.Equal(HammingLoss.Score(yTrue, yPred), ZeroOneLoss.Score(yTrue, yPred));
        Assert.Equal(1.0 - Accuracy.Score(yTrue, yPred), ZeroOneLoss.Score(yTrue, yPred), MetricsCorpus.Tolerance);

        bool[] matrixTrue = MetricsCorpus.Bools(Multilabel, "y_true");
        bool[] matrixPred = MetricsCorpus.Bools(Multilabel, "y_pred");
        Assert.NotEqual(HammingLoss.Score(matrixTrue, matrixPred, 3), ZeroOneLoss.Score(matrixTrue, matrixPred, 3));
    }

    [Fact]
    public void Answers_a_class_neither_side_carries_as_asked()
    {
        int[] yTrue = MetricsCorpus.Ints(Undefined, "y_true");
        int[] yPred = MetricsCorpus.Ints(Undefined, "y_pred");
        int[] labels = MetricsCorpus.Ints(Undefined, "labels");

        double[] zero = MetricsCorpus.Doubles(Undefined, "jaccard_zero");
        double[] one = MetricsCorpus.Doubles(Undefined, "jaccard_one");
        Assert.Equal(zero, JaccardScore.PerClass(yTrue, yPred, ZeroDivision.Zero, labels));
        Assert.Equal(one, JaccardScore.PerClass(yTrue, yPred, ZeroDivision.One, labels));
        Assert.Equal(Undefined.GetProperty("jaccard_macro_zero").GetDouble(),
                     JaccardScore.Score(yTrue, yPred, Averaging.Macro, 1, ZeroDivision.Zero, labels),
                     MetricsCorpus.Tolerance);

        // NaN and Throw have no counterpart in the reference, which admits only 0 and 1.
        Assert.True(double.IsNaN(JaccardScore.PerClass(yTrue, yPred, ZeroDivision.NaN, labels)[2]));
        Assert.Throws<UndefinedMetricException>(
            () => JaccardScore.PerClass(yTrue, yPred, ZeroDivision.Throw, labels));
    }

    // Jaccard is precision's numerator over a larger denominator, so it can never read
    // above either of the two it sits between.
    [Fact]
    public void Never_reads_above_precision_or_recall()
    {
        int[] yTrue = [0, 1, 2, 1, 0, 2];
        int[] yPred = [0, 2, 2, 1, 1, 2];

        double[] jaccard = JaccardScore.PerClass(yTrue, yPred);
        double[] precision = Precision.PerClass(yTrue, yPred);
        double[] recall = Recall.PerClass(yTrue, yPred);

        for (int i = 0; i < jaccard.Length; i++)
        {
            Assert.True(jaccard[i] <= precision[i] + MetricsCorpus.Tolerance);
            Assert.True(jaccard[i] <= recall[i] + MetricsCorpus.Tolerance);
        }
    }

    [Fact]
    public void Refuses_a_matrix_that_is_not_whole_rows()
    {
        bool[] yTrue = [true, false, true];
        bool[] yPred = [true, false, true];

        Assert.Throws<ArgumentException>(() => HammingLoss.Score(yTrue, yPred, 2));
        Assert.Throws<ArgumentException>(() => ZeroOneLoss.Score(yTrue, yPred, 2));
        Assert.Throws<ArgumentException>(() => HammingLoss.Score(yTrue, [true, false], 3));
    }
}
