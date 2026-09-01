using System.Text.Json;
using Xunit;

namespace Lodestar.Conformal.Tests;

/// <summary>Shared access to the frozen MAPIE corpus.</summary>
internal static class ConformalCorpus
{
    /// <summary>The tolerance the whole repository uses for oracle replay.</summary>
    public const double Tolerance = 1e-9;

    private static readonly JsonDocument Document = OracleLoader.Load("conformal.json");

    public static IReadOnlyList<JsonElement> Section(string name) =>
        [.. Document.RootElement.GetProperty(name).EnumerateArray()];

    /// <summary>One theory row per case, so a failure names the case that failed.</summary>
    public static TheoryData<int> Indices(string name)
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Section(name).Count; i++)
        {
            data.Add(i);
        }
        return data;
    }

    public static double Alpha(JsonElement c) => c.GetProperty("alpha").GetDouble();

    public static double Frozen(JsonElement c, string name) => c.GetProperty(name).GetDouble();

    public static double[] Doubles(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetDouble())];

    public static int[] Ints(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetInt32())];

    /// <summary>One row of a row-major block, as the span the API takes.</summary>
    public static ReadOnlySpan<double> Row(double[] flat, int index, int width) =>
        flat.AsSpan(index * width, width);
}
