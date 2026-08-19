using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

/// <summary>The two ratios, and the four ways one of them has no value.</summary>
public sealed class LikelihoodRatiosTests
{
    private static readonly JsonDocument Document = OracleLoader.Load("likelihood_ratios.json");

    private static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("cases").EnumerateArray()];

    public static TheoryData<int> Indices()
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Cases.Count; i++)
        {
            data.Add(i);
        }

        return data;
    }

    /// <summary>The corpus writes an undefined ratio as null, JSON having no NaN.</summary>
    private static double Expected(JsonElement c, string name) =>
        c.GetProperty(name).ValueKind == JsonValueKind.Null
            ? double.NaN
            : c.GetProperty(name).GetDouble();

    private static void Same(string what, double expected, double actual)
    {
        if (double.IsNaN(expected))
        {
            Assert.True(double.IsNaN(actual), $"{what}: expected no value, got {actual}");
            return;
        }

        Assert.Equal(expected, actual, MetricsCorpus.Tolerance);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void Matches_sklearn_on_every_case(int index)
    {
        JsonElement c = Cases[index];
        int[] yTrue = MetricsCorpus.Ints(c, "y_true");
        int[] yPred = MetricsCorpus.Ints(c, "y_pred");
        double[] weight = MetricsCorpus.OptionalDoubles(c, "sample_weight");

        LikelihoodRatios plain = LikelihoodRatios.Compute(yTrue, yPred, 1, double.NaN, double.NaN, weight);
        Same("LR+", Expected(c, "positive"), plain.Positive);
        Same("LR-", Expected(c, "negative"), plain.Negative);

        LikelihoodRatios one = LikelihoodRatios.Compute(yTrue, yPred, 1, 1.0, 1.0, weight);
        Same("LR+ replaced by 1", Expected(c, "positive_replaced_by_one"), one.Positive);
        Same("LR- replaced by 1", Expected(c, "negative_replaced_by_one"), one.Negative);

        LikelihoodRatios apart = LikelihoodRatios.Compute(yTrue, yPred, 1, 10.0, 0.5, weight);
        Same("LR+ replaced apart", Expected(c, "positive_replaced_apart"), apart.Positive);
        Same("LR- replaced apart", Expected(c, "negative_replaced_apart"), apart.Negative);
    }

    // The two absences do not answer alike, and nothing in the reference's signature
    // says so — it is only visible by running both.
    [Fact]
    public void A_missing_positive_class_refuses_the_replacement_and_a_missing_negative_takes_it()
    {
        LikelihoodRatios noPositive = LikelihoodRatios.Compute([0, 0], [0, 1], 1, 1.0, 1.0);
        Assert.True(double.IsNaN(noPositive.Positive));
        Assert.True(double.IsNaN(noPositive.Negative));

        LikelihoodRatios noNegative = LikelihoodRatios.Compute([1, 1], [0, 1], 1, 1.0, 1.0);
        Assert.Equal(1.0, noNegative.Positive);
        Assert.Equal(1.0, noNegative.Negative);
    }

    // The added negatives keep the original three-to-one split, holding specificity
    // fixed; adding only correct ones would raise it and move both ratios.
    [Fact]
    public void Does_not_move_with_the_base_rate_where_precision_does()
    {
        int[] yTrue = [0, 0, 0, 0, 1, 1];
        int[] yPred = [0, 0, 0, 1, 1, 0];

        int[] rarer = [.. yTrue, 0, 0, 0, 0];
        int[] rarerPred = [.. yPred, 0, 0, 0, 1];

        LikelihoodRatios before = LikelihoodRatios.Compute(yTrue, yPred);
        LikelihoodRatios after = LikelihoodRatios.Compute(rarer, rarerPred);

        Assert.Equal(before.Positive, after.Positive, MetricsCorpus.Tolerance);
        Assert.Equal(before.Negative, after.Negative, MetricsCorpus.Tolerance);
        Assert.NotEqual(Precision.Score(yTrue, yPred), Precision.Score(rarer, rarerPred), 6);
    }

    [Fact]
    public void Refuses_more_than_two_classes()
    {
        var error = Assert.Throws<ArgumentException>(
            () => LikelihoodRatios.Compute([0, 1, 2], [0, 1, 2]));
        Assert.Contains("binary classification", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Refuses_shapes_that_disagree()
    {
        Assert.Throws<ArgumentException>(() => LikelihoodRatios.Compute([0, 1], [0]));
        Assert.Throws<ArgumentException>(() => LikelihoodRatios.Compute([], []));
    }
}
