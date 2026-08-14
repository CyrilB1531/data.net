using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

public sealed class LogErrorTests
{
    [Theory]
    [MemberData(nameof(RegressionCorpus.Indices), MemberType = typeof(RegressionCorpus))]
    public void Matches_sklearn_mean_squared_log_error(int index)
    {
        JsonElement c = RegressionCorpus.Cases[index];
        if (!RegressionCorpus.Has(c, "msle|uniform"))
        {
            // The log family is defined only where every target exceeds -1.
            return;
        }

        double[] yTrue = RegressionCorpus.Doubles(c, "y_true");
        double[] yPred = RegressionCorpus.Doubles(c, "y_pred");
        int k = RegressionCorpus.OutputCount(c);
        double[] sw = RegressionCorpus.OptionalDoubles(c, "sample_weight");
        string who = RegressionCorpus.Describe(c);

        RegressionCorpus.AssertClose(
            RegressionCorpus.Value(c, "msle|uniform"),
            MeanSquaredLogError.Score(yTrue, yPred, k, sw), $"{who} msle|uniform");
        RegressionCorpus.AssertClose(
            RegressionCorpus.Values(c, "msle|raw"),
            MeanSquaredLogError.PerOutput(yTrue, yPred, k, sw), $"{who} msle|raw");

        if (RegressionCorpus.Has(c, "msle|weights"))
        {
            RegressionCorpus.AssertClose(
                RegressionCorpus.Value(c, "msle|weights"),
                MeanSquaredLogError.Score(yTrue, yPred, k, sw, RegressionCorpus.OutputWeights(c)),
                $"{who} msle|weights");
        }
    }

    [Theory]
    [MemberData(nameof(RegressionCorpus.Indices), MemberType = typeof(RegressionCorpus))]
    public void Matches_sklearn_root_mean_squared_log_error(int index)
    {
        JsonElement c = RegressionCorpus.Cases[index];
        if (!RegressionCorpus.Has(c, "rmsle|uniform"))
        {
            // The log family is defined only where every target exceeds -1.
            return;
        }

        double[] yTrue = RegressionCorpus.Doubles(c, "y_true");
        double[] yPred = RegressionCorpus.Doubles(c, "y_pred");
        int k = RegressionCorpus.OutputCount(c);
        double[] sw = RegressionCorpus.OptionalDoubles(c, "sample_weight");
        string who = RegressionCorpus.Describe(c);

        RegressionCorpus.AssertClose(
            RegressionCorpus.Value(c, "rmsle|uniform"),
            RootMeanSquaredLogError.Score(yTrue, yPred, k, sw), $"{who} rmsle|uniform");
        RegressionCorpus.AssertClose(
            RegressionCorpus.Values(c, "rmsle|raw"),
            RootMeanSquaredLogError.PerOutput(yTrue, yPred, k, sw), $"{who} rmsle|raw");

        if (RegressionCorpus.Has(c, "rmsle|weights"))
        {
            RegressionCorpus.AssertClose(
                RegressionCorpus.Value(c, "rmsle|weights"),
                RootMeanSquaredLogError.Score(yTrue, yPred, k, sw, RegressionCorpus.OutputWeights(c)),
                $"{who} rmsle|weights");
        }
    }

    [Fact]
    public void The_corpus_actually_carries_log_family_keys_on_at_least_one_case()
    {
        // Guards against the two "if (Has(...)) return" checks above turning into
        // a silent no-op if the corpus ever stopped freezing the log family —
        // this fails loudly instead of the theories above quietly asserting
        // nothing. positive_three_outputs is also the only fixture that carries
        // the |weights variant of either key.
        Assert.Contains(RegressionCorpus.Cases, c => RegressionCorpus.Has(c, "msle|uniform"));
        Assert.Contains(RegressionCorpus.Cases, c => RegressionCorpus.Has(c, "msle|raw"));
        Assert.Contains(RegressionCorpus.Cases, c => RegressionCorpus.Has(c, "msle|weights"));
        Assert.Contains(RegressionCorpus.Cases, c => RegressionCorpus.Has(c, "rmsle|uniform"));
        Assert.Contains(RegressionCorpus.Cases, c => RegressionCorpus.Has(c, "rmsle|raw"));
        Assert.Contains(RegressionCorpus.Cases, c => RegressionCorpus.Has(c, "rmsle|weights"));
    }

    [Fact]
    public void Reproduces_the_hand_measured_values()
    {
        double[] yTrue = [3.0, 5.0, 2.5, 7.0];
        double[] yPred = [2.5, 5.0, 4.0, 8.0];

        Assert.Equal(0.03973012298459379, MeanSquaredLogError.Score(yTrue, yPred), 12);
        Assert.Equal(0.19932416558108, RootMeanSquaredLogError.Score(yTrue, yPred), 12);
    }

    [Fact]
    public void The_root_is_the_root_of_the_squared_form()
    {
        // Invariant 3 of the spec, on a single output where the reduction cannot
        // hide a per-output ordering mistake.
        double[] yTrue = [3.0, 5.0, 2.5, 7.0];
        double[] yPred = [2.5, 5.0, 4.0, 8.0];

        Assert.Equal(
            Math.Sqrt(MeanSquaredLogError.Score(yTrue, yPred)),
            RootMeanSquaredLogError.Score(yTrue, yPred),
            12);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void A_target_at_or_below_minus_one_is_refused_on_either_side(bool onTruth)
    {
        double[] bad = [-1.5, 1.0];
        double[] good = [1.0, 1.0];

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => MeanSquaredLogError.Score(onTruth ? bad : good, onTruth ? good : bad));

        Assert.Contains(onTruth ? "yTrue" : "yPred", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Exactly_minus_one_is_refused_too()
    {
        // log(1 + -1) is log(0), which is -inf rather than an error, so a check
        // written as "< -1" would return -inf instead of throwing.
        Assert.Throws<ArgumentException>(() => MeanSquaredLogError.Score([-1.0, 1.0], [1.0, 1.0]));
    }

    [Fact]
    public void A_tiny_target_keeps_the_bits_that_one_plus_x_would_round_away()
    {
        // The corpus cannot police this: its comparison rule scales by
        // max(1, |expected|), so at 3e-18 it reduces to an absolute 1e-9 and
        // every implementation passes, including one that returns zero. The
        // assertion here is therefore relative.
        //
        // Measured against scikit-learn 1.9.0:
        //   mean_squared_log_error([1e-9, 2e-9, 3e-9], [2e-9, 4e-9, 1e-9])
        //     = 2.9999999856666664e-18
        // Math.Log(1.0 + x) gives 3.000000038019698e-18 — out by 1.7e-8
        // relative, about 17 000 times the 1e-12 bound asserted below.
        const double Expected = 2.9999999856666664e-18;

        double actual = MeanSquaredLogError.Score([1e-9, 2e-9, 3e-9], [2e-9, 4e-9, 1e-9]);

        double relative = Math.Abs(actual - Expected) / Expected;
        Assert.True(relative <= 1e-12, $"expected {Expected:R}, got {actual:R} (relative {relative:R})");
    }
}
