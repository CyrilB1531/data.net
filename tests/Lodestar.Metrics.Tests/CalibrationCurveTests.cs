using System.Globalization;
using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

/// <summary>The reliability curve against the frozen corpus, both arrays element by element.</summary>
public sealed class CalibrationCurveTests
{
    private static readonly JsonDocument Document = OracleLoader.Load("calibration_curve.json");

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

    private static void Same(string what, IReadOnlyList<double> actual, double[] expected)
    {
        Assert.Equal(expected.Length, actual.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.True(
                Math.Abs(expected[i] - actual[i]) <= MetricsCorpus.Tolerance,
                $"{what}[{i}]: expected {expected[i].ToString("R", CultureInfo.InvariantCulture)}, " +
                $"got {actual[i].ToString("R", CultureInfo.InvariantCulture)}");
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void Matches_scikit_learn(int index)
    {
        JsonElement c = Cases[index];
        int[] yTrue = [.. c.GetProperty("y_true").EnumerateArray().Select(v => v.GetInt32())];
        double[] yProb = [.. c.GetProperty("y_proba").EnumerateArray().Select(v => v.GetDouble())];
        double[] probTrue = [.. c.GetProperty("prob_true").EnumerateArray().Select(v => v.GetDouble())];
        double[] probPred = [.. c.GetProperty("prob_pred").EnumerateArray().Select(v => v.GetDouble())];

        CalibrationCurve curve = CalibrationCurve.Compute(
            yTrue,
            yProb,
            c.GetProperty("pos_label").GetInt32(),
            c.GetProperty("n_bins").GetInt32(),
            c.GetProperty("strategy").GetString() == "quantile"
                ? BinStrategy.Quantile
                : BinStrategy.Uniform);

        Same("prob_true", curve.ProbTrue, probTrue);
        Same("prob_pred", curve.ProbPred, probPred);
    }

    /// <summary>The corpus is only evidence if it holds a case where a bin comes out empty.</summary>
    /// <remarks>
    /// Both arrays are as long as the bins that held something, not as long as
    /// <c>nBins</c>, and a reader plotting them needs that to be true of the data rather
    /// than of the parameter. One fixture puts every probability in one bin.
    /// </remarks>
    [Fact]
    public void The_corpus_covers_a_bin_that_came_out_empty()
    {
        Assert.Contains(Cases, c =>
            c.GetProperty("prob_true").GetArrayLength() < c.GetProperty("n_bins").GetInt32());
    }

    [Fact]
    public void Both_arrays_always_share_a_length()
    {
        foreach (JsonElement c in Cases)
        {
            Assert.Equal(
                c.GetProperty("prob_true").GetArrayLength(),
                c.GetProperty("prob_pred").GetArrayLength());
        }
    }

    [Fact]
    public void A_probability_outside_the_unit_interval_is_refused()
    {
        int[] yTrue = [0, 1];
        double[] yProb = [0.5, 1.5];

        ArgumentException error = Assert.Throws<ArgumentException>(() =>
            CalibrationCurve.Compute(yTrue, yProb));

        Assert.Contains("[0, 1]", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_third_label_is_refused()
    {
        int[] yTrue = [0, 1, 2];
        double[] yProb = [0.1, 0.5, 0.9];

        Assert.Throws<ArgumentException>(() => CalibrationCurve.Compute(yTrue, yProb));
    }

    [Fact]
    public void Fewer_than_one_bin_is_refused()
    {
        int[] yTrue = [0, 1];
        double[] yProb = [0.1, 0.9];

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CalibrationCurve.Compute(yTrue, yProb, 1, 0));
    }
}
