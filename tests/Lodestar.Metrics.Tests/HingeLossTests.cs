using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

/// <summary>The hinge loss over a decision function, against the frozen corpus.</summary>
public sealed class HingeLossTests
{
    private static readonly JsonDocument Document = OracleLoader.Load("hinge_loss.json");

    private static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("cases").EnumerateArray()];

    private static JsonElement Multiclass => Document.RootElement.GetProperty("multiclass");

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
    public void Matches_sklearn_on_every_binary_case(int index)
    {
        JsonElement c = Cases[index];
        int[] yTrue = MetricsCorpus.Ints(c, "y_true");
        double[] decision = MetricsCorpus.Doubles(c, "pred_decision");
        double[] weight = MetricsCorpus.OptionalDoubles(c, "sample_weight");

        Assert.Equal(c.GetProperty("hinge").GetDouble(),
                     HingeLoss.Score(yTrue, decision, 1, weight),
                     MetricsCorpus.Tolerance);
    }

    [Fact]
    public void Matches_sklearn_on_one_decision_per_class()
    {
        int[] yTrue = MetricsCorpus.Ints(Multiclass, "y_true");
        double[] decision = MetricsCorpus.Doubles(Multiclass, "pred_decision");
        double[] weight = MetricsCorpus.Doubles(Multiclass, "sample_weight");
        int classes = Multiclass.GetProperty("class_count").GetInt32();

        Assert.Equal(Multiclass.GetProperty("hinge").GetDouble(),
                     HingeLoss.MultiClass(yTrue, decision, classes), MetricsCorpus.Tolerance);
        Assert.Equal(Multiclass.GetProperty("hinge_weighted").GetDouble(),
                     HingeLoss.MultiClass(yTrue, decision, classes, weight), MetricsCorpus.Tolerance);
    }

    // The loss stops charging at a margin of 1 rather than at 0, which is what makes
    // it the thing a support vector machine minimises rather than an error count.
    [Fact]
    public void Charges_nothing_past_a_margin_of_one_and_rises_linearly_below_it()
    {
        int[] yTrue = [1, 1];

        Assert.Equal(0.0, HingeLoss.Score(yTrue, [1.0, 2.0]));
        // Costs 0.5 and nothing, so the mean is a quarter, not a half.
        Assert.Equal(0.25, HingeLoss.Score(yTrue, [0.5, 1.0]), MetricsCorpus.Tolerance);

        // Right side, but inside the margin: still charged, where an error count is 0.
        Assert.True(HingeLoss.Score(yTrue, [0.2, 0.2]) > 0.0);
        Assert.Equal(0.0, ZeroOneLoss.Score(yTrue, [1, 1]));
    }

    // Only the sign of the decision matters against the label, so relabelling the two
    // classes cannot move the number.
    [Fact]
    public void Reads_the_sign_rather_than_the_label()
    {
        double[] decision = [-0.5, 1.2, 0.3, 0.8];

        Assert.Equal(HingeLoss.Score([-1, 1, 1, -1], decision),
                     HingeLoss.Score([0, 1, 1, 0], decision), MetricsCorpus.Tolerance);
        Assert.Equal(HingeLoss.Score([-1, 1, 1, -1], decision),
                     HingeLoss.Score([7, 3, 3, 7], decision, 3), MetricsCorpus.Tolerance);
    }

    [Fact]
    public void Refuses_shapes_that_disagree()
    {
        Assert.Throws<ArgumentException>(() => HingeLoss.Score([0, 1], [0.5]));
        Assert.Throws<ArgumentException>(() => HingeLoss.Score([], []));
        Assert.Throws<ArgumentException>(() => HingeLoss.MultiClass([0, 1], [0.5, 0.3, 0.2], 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => HingeLoss.MultiClass([0], [0.5], 1));
        Assert.Throws<ArgumentException>(() => HingeLoss.MultiClass([0, 5], [0.5, 0.3, 0.2, 0.1], 2));
    }
}
