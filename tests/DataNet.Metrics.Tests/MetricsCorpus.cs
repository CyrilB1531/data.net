using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

/// <summary>Shared access to the frozen classification-metrics corpus.</summary>
internal static class MetricsCorpus
{
    /// <summary>The tolerance the whole repository uses for oracle replay.</summary>
    public const double Tolerance = 1e-9;

    private static readonly JsonDocument Document = OracleLoader.Load("classification_metrics.json");

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

    public static int[] Ints(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetInt32())];

    public static double[] Doubles(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetDouble())];

    /// <summary>Reads an array property that the corpus writes as null when absent.</summary>
    public static int[] OptionalInts(JsonElement c, string name) =>
        c.GetProperty(name).ValueKind == JsonValueKind.Null ? [] : Ints(c, name);

    public static double[] OptionalDoubles(JsonElement c, string name) =>
        c.GetProperty(name).ValueKind == JsonValueKind.Null ? [] : Doubles(c, name);

    /// <summary>
    /// The case's confusion matrix, with the case's own <c>labels</c> and weights.
    /// Not quite how every oracle test builds one: the balanced-accuracy, Matthews
    /// and kappa tests pass no <c>labels</c>, because their generators pass none
    /// either — <c>balanced_accuracy_score</c> and <c>matthews_corrcoef</c> have no
    /// such parameter at all. Callers that need the case's label order (the
    /// normalization test, whose oracle rows are shaped by it) use this.
    /// </summary>
    public static ConfusionMatrix Matrix(JsonElement c) => ConfusionMatrix.Compute(
        Ints(c, "y_true"),
        Ints(c, "y_pred"),
        OptionalInts(c, "labels"),
        OptionalDoubles(c, "sample_weight"));
}
