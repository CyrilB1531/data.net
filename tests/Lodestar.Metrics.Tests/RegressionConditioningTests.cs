using System.Globalization;
using System.Text.Json;
using Lodestar.Metrics;
using Xunit;

namespace Lodestar.Metrics.Tests;

/// <summary>
/// Replays <c>regression_conditioning.json</c>: 200 000 samples of a target with a
/// large offset over a small spread, which is where a sequential sum and numpy's
/// pairwise one part company (issue #127). The corpus carries no arrays — they
/// would be megabytes — but the closed form that builds them, and the raw bits of
/// five values along the way, compared before anything is scored: two sides that
/// built slightly different arrays would otherwise compare their scores happily
/// and prove nothing.
/// </summary>
public sealed class RegressionConditioningTests
{
    private static readonly JsonDocument Corpus = OracleLoader.Load("regression_conditioning.json");

    [Fact]
    public void The_rebuilt_arrays_are_the_ones_scikit_learn_scored()
    {
        (double[] yTrue, double[] yPred) = Build();
        JsonElement metadata = Corpus.RootElement.GetProperty("metadata");
        int[] indices = [.. metadata.GetProperty("probe_indices").EnumerateArray().Select(e => e.GetInt32())];
        string[] trueBits = [.. metadata.GetProperty("probe_bits_y_true").EnumerateArray().Select(e => e.GetString()!)];
        string[] predBits = [.. metadata.GetProperty("probe_bits_y_pred").EnumerateArray().Select(e => e.GetString()!)];

        Assert.NotEmpty(indices);
        for (int probe = 0; probe < indices.Length; probe++)
        {
            Assert.Equal(trueBits[probe], Bits(yTrue[indices[probe]]));
            Assert.Equal(predBits[probe], Bits(yPred[indices[probe]]));
        }
    }

    [Theory]
    [InlineData("r2")]
    [InlineData("explained_variance")]
    [InlineData("mse")]
    [InlineData("mae")]
    public void Each_metric_matches_scikit_learn(string key)
    {
        (double[] yTrue, double[] yPred) = Build();
        double expected = OracleLoader.Number(Corpus.RootElement.GetProperty("values").GetProperty(key));

        double actual = key switch
        {
            "r2" => R2.Score(yTrue, yPred),
            "explained_variance" => ExplainedVariance.Score(yTrue, yPred),
            "mse" => MeanSquaredError.Score(yTrue, yPred),
            "mae" => MeanAbsoluteError.Score(yTrue, yPred),
            _ => throw new ArgumentOutOfRangeException(nameof(key), key, "no metric for this corpus key"),
        };

        AssertRelative(expected, actual, key);
    }

    /// <summary>
    /// A pure relative bound, in place of <see cref="RegressionCorpus.AssertClose"/>'s
    /// <c>1e-9 * max(1, |expected|)</c>: this fixture's own <c>mse</c> (3.97e-12)
    /// and <c>mae</c> (1.70e-6) sit far below 1, where the shared floor turns into
    /// an absolute 1e-9 — 250× the <c>mse</c> itself, so <c>0.0</c> would still
    /// pass. Dropped here only, not in the shared helper other corpora rely on at
    /// their own scale. The NaN/infinity branches carry over unused today, but
    /// share a generator with <c>tests/oracles/regression.json</c>, which already
    /// stores them, so a fifth row landing on one here is not hypothetical.
    /// </summary>
    private static void AssertRelative(double expected, double actual, string because)
    {
        if (double.IsNaN(expected))
        {
            Assert.True(double.IsNaN(actual), $"{because}: expected NaN, got {actual}");
            return;
        }

        if (double.IsInfinity(expected))
        {
            bool matches = double.IsPositiveInfinity(expected)
                ? double.IsPositiveInfinity(actual)
                : double.IsNegativeInfinity(actual);

            Assert.True(matches, $"{because}: expected {expected}, got {actual}");
            return;
        }

        double bound = 1e-9 * Math.Abs(expected);
        Assert.True(Math.Abs(expected - actual) <= bound,
            $"{because}: expected {expected:R}, got {actual:R} (tolerance {bound:R})");
    }

    /// <summary>The corpus's own closed form, evaluated in the same order Python evaluates it.</summary>
    private static (double[] YTrue, double[] YPred) Build()
    {
        JsonElement metadata = Corpus.RootElement.GetProperty("metadata");
        int samples = metadata.GetProperty("samples").GetInt32();
        double offset = metadata.GetProperty("offset").GetDouble();
        double step = metadata.GetProperty("spread").GetDouble() / samples;
        double perturbation = metadata.GetProperty("perturbation").GetDouble();

        double[] yTrue = new double[samples];
        double[] yPred = new double[samples];
        for (int i = 0; i < samples; i++)
        {
            yTrue[i] = offset + (i * step);
            yPred[i] = yTrue[i] + (((i % 7) - 3) * perturbation);
        }
        return (yTrue, yPred);
    }

    private static string Bits(double value) =>
        BitConverter.DoubleToInt64Bits(value).ToString("x16", CultureInfo.InvariantCulture);
}
