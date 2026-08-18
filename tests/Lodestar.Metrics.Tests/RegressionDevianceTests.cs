using System.Globalization;
using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

/// <summary>The three GLM deviances and the three D² scores against the frozen corpus.</summary>
public sealed class RegressionDevianceTests
{
    private static readonly JsonDocument Document = OracleLoader.Load("regression_deviance.json");

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

    private static (double[] YTrue, double[] YPred, double[] Weight) Read(JsonElement c) =>
        (MetricsCorpus.Doubles(c, "y_true"),
         MetricsCorpus.Doubles(c, "y_pred"),
         MetricsCorpus.OptionalDoubles(c, "sample_weight"));

    [Theory]
    [MemberData(nameof(Indices))]
    public void Matches_sklearn_on_every_tweedie_regime(int index)
    {
        JsonElement c = Cases[index];
        (double[] yTrue, double[] yPred, double[] weight) = Read(c);

        foreach (JsonElement entry in c.GetProperty("tweedie").EnumerateArray())
        {
            double power = entry.GetProperty("power").GetDouble();
            Assert.Equal(entry.GetProperty("deviance").GetDouble(),
                         TweedieDeviance.Score(yTrue, yPred, power, weight),
                         MetricsCorpus.Tolerance);

            if (entry.TryGetProperty("d2", out JsonElement d2))
            {
                Assert.Equal(d2.GetDouble(),
                             D2Tweedie.Score(yTrue, yPred, power, weight),
                             MetricsCorpus.Tolerance);
            }
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void Matches_sklearn_on_the_named_deviances(int index)
    {
        JsonElement c = Cases[index];
        (double[] yTrue, double[] yPred, double[] weight) = Read(c);

        if (c.TryGetProperty("poisson", out JsonElement poisson))
        {
            Assert.Equal(poisson.GetDouble(), PoissonDeviance.Score(yTrue, yPred, weight),
                         MetricsCorpus.Tolerance);
        }

        if (c.TryGetProperty("gamma", out JsonElement gamma))
        {
            Assert.Equal(gamma.GetDouble(), GammaDeviance.Score(yTrue, yPred, weight),
                         MetricsCorpus.Tolerance);
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void Matches_sklearn_on_the_pinball_scores(int index)
    {
        JsonElement c = Cases[index];
        (double[] yTrue, double[] yPred, double[] weight) = Read(c);

        Assert.Equal(c.GetProperty("d2_absolute_error").GetDouble(),
                     D2AbsoluteError.Score(yTrue, yPred, 1, weight),
                     MetricsCorpus.Tolerance);

        foreach (JsonElement entry in c.GetProperty("pinball").EnumerateArray())
        {
            Assert.Equal(entry.GetProperty("d2").GetDouble(),
                         D2Pinball.Score(yTrue, yPred, entry.GetProperty("alpha").GetDouble(), 1, weight),
                         MetricsCorpus.Tolerance);
        }
    }

    [Fact]
    public void Matches_sklearn_on_two_outputs()
    {
        JsonElement m = Document.RootElement.GetProperty("multioutput");
        double[] yTrue = MetricsCorpus.Doubles(m, "y_true");
        double[] yPred = MetricsCorpus.Doubles(m, "y_pred");
        int outputs = m.GetProperty("output_count").GetInt32();

        Assert.Equal(m.GetProperty("uniform_average").GetDouble(),
                     D2AbsoluteError.Score(yTrue, yPred, outputs), MetricsCorpus.Tolerance);
        Assert.Equal(m.GetProperty("pinball_uniform_average").GetDouble(),
                     D2Pinball.Score(yTrue, yPred, 0.75, outputs), MetricsCorpus.Tolerance);

        AssertPerOutput(MetricsCorpus.Doubles(m, "raw_values"),
                        D2AbsoluteError.PerOutput(yTrue, yPred, outputs));
        AssertPerOutput(MetricsCorpus.Doubles(m, "pinball_raw_values"),
                        D2Pinball.PerOutput(yTrue, yPred, 0.75, outputs));
    }

    private static void AssertPerOutput(double[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i], MetricsCorpus.Tolerance);
        }
    }

    // An invariant no oracle states, asserted across the corpus rather than on one
    // pair: the two reach their denominator by different code, a quantile and a median.
    [Theory]
    [MemberData(nameof(Indices))]
    public void D2_pinball_at_one_half_is_the_absolute_error_score(int index)
    {
        JsonElement c = Cases[index];
        (double[] yTrue, double[] yPred, double[] weight) = Read(c);

        Assert.Equal(D2AbsoluteError.Score(yTrue, yPred, 1, weight),
                     D2Pinball.Score(yTrue, yPred, 0.5, 1, weight),
                     MetricsCorpus.Tolerance);
    }

    // The two named deviances are the general one at a fixed power, which is how
    // scikit-learn defines them and the only reason they are separate types here.
    [Theory]
    [MemberData(nameof(Indices))]
    public void The_named_deviances_are_the_tweedie_at_their_power(int index)
    {
        JsonElement c = Cases[index];
        (double[] yTrue, double[] yPred, double[] weight) = Read(c);

        if (c.TryGetProperty("poisson", out _))
        {
            Assert.Equal(TweedieDeviance.Score(yTrue, yPred, 1.0, weight),
                         PoissonDeviance.Score(yTrue, yPred, weight));
        }

        if (c.TryGetProperty("gamma", out _))
        {
            Assert.Equal(TweedieDeviance.Score(yTrue, yPred, 2.0, weight),
                         GammaDeviance.Score(yTrue, yPred, weight));
        }
    }

    [Theory]
    [InlineData(0.001)]
    [InlineData(0.5)]
    [InlineData(0.999)]
    public void Refuses_a_power_naming_no_distribution(double power)
    {
        double[] yTrue = [1.0, 2.0, 3.0];
        double[] yPred = [1.5, 2.5, 2.0];

        var error = Assert.Throws<ArgumentOutOfRangeException>(
            () => TweedieDeviance.Score(yTrue, yPred, power));
        Assert.Equal("power", error.ParamName);
    }

    [Theory]
    // Below zero only the prediction is constrained; the truth may be anything.
    [InlineData(-1.0, "1,2,3", "1,2,0", true)]
    [InlineData(-1.0, "-1,2,3", "1,2,3", false)]
    // At zero nothing is.
    [InlineData(0.0, "-1,2,3", "0,2,-3", false)]
    // In [1, 2) a zero truth is legal and a zero prediction is not.
    [InlineData(1.0, "0,2,3", "1,2,3", false)]
    [InlineData(1.0, "-1,2,3", "1,2,3", true)]
    [InlineData(1.5, "1,2,3", "0,2,3", true)]
    // From 2 up both must be strictly positive.
    [InlineData(2.0, "0,2,3", "1,2,3", true)]
    [InlineData(3.0, "1,2,3", "0,2,3", true)]
    public void Applies_the_regime_of_its_power(double power, string truth, string prediction, bool refused)
    {
        double[] yTrue = Parse(truth);
        double[] yPred = Parse(prediction);

        if (refused)
        {
            Assert.Throws<ArgumentException>(() => TweedieDeviance.Score(yTrue, yPred, power));
            return;
        }

        Assert.True(double.IsFinite(TweedieDeviance.Score(yTrue, yPred, power)));
    }

    private static double[] Parse(string values) =>
        [.. values.Split(',').Select(v => double.Parse(v, CultureInfo.InvariantCulture))];

    [Fact]
    public void D2_tweedie_refuses_a_truth_that_never_varies()
    {
        double[] yTrue = [2.0, 2.0, 2.0];
        double[] yPred = [1.0, 2.0, 3.0];

        Assert.Throws<UndefinedMetricException>(() => D2Tweedie.Score(yTrue, yPred, 1.0));

        // The pinball D² masks the same denominator and answers 0, as its reference does.
        Assert.Equal(0.0, D2AbsoluteError.Score(yTrue, yPred));
    }

    [Fact]
    public void The_d2_scores_are_nan_below_two_samples()
    {
        double[] yTrue = [1.0];
        double[] yPred = [2.0];

        Assert.True(double.IsNaN(D2AbsoluteError.Score(yTrue, yPred)));
        Assert.True(double.IsNaN(D2Pinball.Score(yTrue, yPred, 0.9)));
        Assert.True(double.IsNaN(D2Tweedie.Score(yTrue, yPred)));
        Assert.Throws<UndefinedMetricException>(
            () => D2Tweedie.Score(yTrue, yPred, 0.0, default, ZeroDivision.Throw));
    }
}
