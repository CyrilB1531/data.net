using System.Text.Json;
using Xunit;

namespace DataNet.Metrics.Tests;

/// <summary>Shared access to the frozen ROC-AUC corpus.</summary>
internal static class RocCorpus
{
    private static readonly JsonDocument Document = OracleLoader.Load("roc_auc.json");

    public static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("cases").EnumerateArray()];

    public static TheoryData<int> Indices(string kind)
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Cases.Count; i++)
        {
            if (Cases[i].GetProperty("kind").GetString() == kind)
            {
                data.Add(i);
            }
        }
        return data;
    }

    public static TheoryData<int> BinaryIndices() => Indices("binary");

    public static TheoryData<int> MulticlassIndices() => Indices("multiclass");

    public static string Describe(JsonElement c) =>
        $"{c.GetProperty("fixture").GetString()} (weighted={c.GetProperty("weighted").GetBoolean()})";

    public static int[] YTrue(JsonElement c) =>
        [.. c.GetProperty("y_true").EnumerateArray().Select(x => x.GetInt32())];

    public static double[] SampleWeight(JsonElement c) =>
        c.GetProperty("sample_weight").ValueKind == JsonValueKind.Null
            ? []
            : [.. c.GetProperty("sample_weight").EnumerateArray().Select(x => x.GetDouble())];

    /// <summary>Binary scores: one per sample.</summary>
    public static double[] FlatScores(JsonElement c) =>
        [.. c.GetProperty("scores").EnumerateArray().Select(x => x.GetDouble())];

    /// <summary>Multiclass scores flattened row-major, which is what the API takes.</summary>
    public static double[] RowMajorScores(JsonElement c) =>
        [.. c.GetProperty("scores").EnumerateArray()
              .SelectMany(row => row.EnumerateArray())
              .Select(x => x.GetDouble())];
}
