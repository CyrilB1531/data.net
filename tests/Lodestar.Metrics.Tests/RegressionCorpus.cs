using System.Text.Json;
using Xunit;

namespace Lodestar.Metrics.Tests;

/// <summary>Shared access to the frozen regression corpus.</summary>
internal static class RegressionCorpus
{
    private static readonly JsonDocument Document = OracleLoader.Load("regression.json");

    public static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("cases").EnumerateArray()];

    /// <summary>One theory row per case, so a failure names the case that failed.</summary>
    public static TheoryData<int> Indices()
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Cases.Count; i++)
        {
            data.Add(i);
        }
        return data;
    }

    public static string Describe(JsonElement c) =>
        $"{c.GetProperty("fixture").GetString()} (weighted={c.GetProperty("weighted").GetBoolean()})";

    public static int OutputCount(JsonElement c) => c.GetProperty("output_count").GetInt32();

    public static double[] Doubles(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetDouble())];

    public static double[] OptionalDoubles(JsonElement c, string name) =>
        c.GetProperty(name).ValueKind == JsonValueKind.Null ? [] : Doubles(c, name);

    /// <summary>The output weights the generator froze for this case's width.</summary>
    public static double[] OutputWeights(JsonElement c) => OutputCount(c) switch
    {
        2 => [0.3, 0.7],
        3 => [0.2, 0.3, 0.5],
        _ => [],
    };

    public static bool Has(JsonElement c, string key) =>
        c.GetProperty("values").TryGetProperty(key, out _);

    public static double Value(JsonElement c, string key) =>
        OracleLoader.Number(c.GetProperty("values").GetProperty(key));

    public static double[] Values(JsonElement c, string key) =>
        [.. c.GetProperty("values").GetProperty(key).EnumerateArray().Select(OracleLoader.Number)];

    /// <summary>
    /// The comparison rule for this corpus, which cannot be a single absolute
    /// tolerance: its values span 0.0 to 4.5e15, where an absolute 1e-9 is
    /// meaningless. Scaling by <c>max(1, |expected|)</c> reduces to
    /// <c>CONTRIBUTING.md</c>'s absolute 1e-9 at or below 1 — where scikit-learn
    /// defines rather than approximates, 0.0 and 1.0 among them — and stays
    /// meaningful above it. A non-finite expectation is matched by predicate, not
    /// comparison, since <c>==</c> is false for <c>NaN</c> against itself.
    /// </summary>
    public static void AssertClose(double expected, double actual, string because)
    {
        if (double.IsNaN(expected))
        {
            Assert.True(double.IsNaN(actual), $"{because}: expected NaN, got {actual}");
            return;
        }

        if (double.IsInfinity(expected))
        {
            // Two predicates, not an equality (S1244 would object): whether it is
            // the *same* infinity, without comparing two floating-point values.
            bool matches = double.IsPositiveInfinity(expected)
                ? double.IsPositiveInfinity(actual)
                : double.IsNegativeInfinity(actual);

            Assert.True(matches, $"{because}: expected {expected}, got {actual}");
            return;
        }

        double bound = 1e-9 * Math.Max(1.0, Math.Abs(expected));
        Assert.True(Math.Abs(expected - actual) <= bound,
            $"{because}: expected {expected:R}, got {actual:R} (tolerance {bound:R})");
    }

    public static void AssertClose(double[] expected, double[] actual, string because)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            AssertClose(expected[i], actual[i], $"{because}[{i}]");
        }
    }
}
