using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

/// <summary>Brier score and log loss against the frozen corpus.</summary>
public sealed class CalibrationTests
{
    private static readonly JsonDocument Document = OracleLoader.Load("calibration.json");

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
        double[] yProba = MetricsCorpus.Doubles(c, "y_proba");
        double[] weight = MetricsCorpus.OptionalDoubles(c, "sample_weight");
        int posLabel = c.GetProperty("pos_label").GetInt32();

        Assert.Equal(c.GetProperty("brier").GetDouble(),
                     BrierScore.Score(yTrue, yProba, posLabel, true, weight),
                     MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("brier_unscaled").GetDouble(),
                     BrierScore.Score(yTrue, yProba, posLabel, false, weight),
                     MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("log_loss").GetDouble(),
                     LogLoss.Score(yTrue, yProba, posLabel, true, weight),
                     MetricsCorpus.Tolerance);
        Assert.Equal(c.GetProperty("log_loss_total").GetDouble(),
                     LogLoss.Score(yTrue, yProba, posLabel, false, weight),
                     MetricsCorpus.Tolerance);
    }

    [Fact]
    public void Matches_sklearn_on_a_probability_matrix()
    {
        int[] yTrue = MetricsCorpus.Ints(Multiclass, "y_true");
        double[] yProba = MetricsCorpus.Doubles(Multiclass, "y_proba");
        double[] weight = MetricsCorpus.Doubles(Multiclass, "sample_weight");
        int classes = Multiclass.GetProperty("class_count").GetInt32();

        Assert.Equal(Multiclass.GetProperty("log_loss").GetDouble(),
                     LogLoss.MultiClass(yTrue, yProba, classes), MetricsCorpus.Tolerance);
        Assert.Equal(Multiclass.GetProperty("log_loss_total").GetDouble(),
                     LogLoss.MultiClass(yTrue, yProba, classes, false), MetricsCorpus.Tolerance);
        Assert.Equal(Multiclass.GetProperty("log_loss_weighted").GetDouble(),
                     LogLoss.MultiClass(yTrue, yProba, classes, true, weight), MetricsCorpus.Tolerance);
        Assert.Equal(Multiclass.GetProperty("brier").GetDouble(),
                     BrierScore.MultiClass(yTrue, yProba, classes), MetricsCorpus.Tolerance);
        Assert.Equal(Multiclass.GetProperty("brier_scaled").GetDouble(),
                     BrierScore.MultiClass(yTrue, yProba, classes, true), MetricsCorpus.Tolerance);
    }

    // The reference warns and scores the values as given rather than renormalising.
    // C# has no warning channel, so the number is what carries the behaviour.
    [Fact]
    public void Scores_rows_that_do_not_sum_to_one_as_given()
    {
        int[] yTrue = MetricsCorpus.Ints(Multiclass, "y_true");
        double[] yProba = [.. MetricsCorpus.Doubles(Multiclass, "y_proba").Select(v => v * 0.5)];
        int classes = Multiclass.GetProperty("class_count").GetInt32();

        Assert.Equal(Multiclass.GetProperty("half_rows_log_loss").GetDouble(),
                     LogLoss.MultiClass(yTrue, yProba, classes), MetricsCorpus.Tolerance);
        Assert.Equal(Multiclass.GetProperty("half_rows_brier").GetDouble(),
                     BrierScore.MultiClass(yTrue, yProba, classes), MetricsCorpus.Tolerance);
    }

    // The clip is the specification, and it has moved across scikit-learn versions.
    [Fact]
    public void Clips_at_machine_epsilon_and_nowhere_else()
    {
        int[] yTrue = [1, 0];

        // Below the clip every probability gives the same loss; above it, they differ.
        double atZero = LogLoss.Score(yTrue, [0.0, 0.5]);
        double belowClip = LogLoss.Score(yTrue, [1e-20, 0.5]);
        double aboveClip = LogLoss.Score(yTrue, [1e-15, 0.5]);

        Assert.Equal(atZero, belowClip, MetricsCorpus.Tolerance);
        Assert.True(aboveClip < atZero);

        // A perfect prediction is one epsilon from zero rather than zero, because the
        // upper end is clipped too.
        Assert.Equal(2.220446049250313e-16, LogLoss.Score([0, 1], [0.0, 1.0]), 1e-24);
    }

    [Fact]
    public void Brier_never_exceeds_one_where_log_loss_is_unbounded()
    {
        int[] yTrue = [1, 1];
        double[] confidentlyWrong = [0.0, 0.0];

        Assert.Equal(1.0, BrierScore.Score(yTrue, confidentlyWrong));
        Assert.True(LogLoss.Score(yTrue, confidentlyWrong) > 30.0);
    }

    [Theory]
    [InlineData(1.5, "greater than 1")]
    [InlineData(-0.1, "lower than 0")]
    public void Log_loss_refuses_a_value_that_is_not_a_probability(double bad, string expected)
    {
        var error = Assert.Throws<ArgumentException>(() => LogLoss.Score([0, 1], [bad, 0.5]));
        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
    }

    // The two reference functions word the lower refusal differently, and each is kept.
    [Fact]
    public void Brier_words_its_lower_refusal_as_the_reference_does()
    {
        var error = Assert.Throws<ArgumentException>(() => BrierScore.Score([0, 1], [-0.1, 0.9]));
        Assert.Contains("less than 0", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Refuses_a_matrix_that_is_not_a_whole_number_of_rows()
    {
        int[] yTrue = [0, 1];
        double[] yProba = [0.5, 0.3, 0.2, 0.4, 0.4];

        Assert.Throws<ArgumentException>(() => LogLoss.MultiClass(yTrue, yProba, 3));
        Assert.Throws<ArgumentOutOfRangeException>(() => BrierScore.MultiClass(yTrue, yProba, 1));
        Assert.Throws<ArgumentException>(() => BrierScore.MultiClass([0, 5], [0.5, 0.5, 0.5, 0.5], 2));
    }
}
