using System.Text.Json;
using Lodestar.Abstractions;
using Lodestar.Decomposition.Internal;
using Xunit;

namespace Lodestar.Decomposition.Tests;

/// <summary>
/// <c>_initialize_nmf</c>'s NNDSVD family, over the same frozen Ω its internal
/// <c>randomized_svd</c> would have drawn.
/// </summary>
public sealed class NndSvdTests
{
    private const double Tolerance = 1e-9;

    private static readonly JsonDocument Document = OracleLoader.Load("decomposition_nmf.json");

    private static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("initialization").EnumerateArray()];

    public static TheoryData<int> Indices()
    {
        var data = new TheoryData<int>();
        for (int i = 0; i < Cases.Count; i++)
        {
            data.Add(i);
        }
        return data;
    }

    private static double[] Doubles(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetDouble())];

    private static int[] Ints(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetInt32())];

    private static CsrMatrix Matrix(JsonElement c) => new(
        c.GetProperty("rows").GetInt32(),
        c.GetProperty("columns").GetInt32(),
        Doubles(c, "values"),
        Ints(c, "column_indices"),
        Ints(c, "row_pointers"));

    private static (double[] W, double[] H) Initialize(JsonElement c) => NndSvd.Initialize(
        Matrix(c),
        c.GetProperty("component_count").GetInt32(),
        c.GetProperty("initialization").GetString() == "nndsvda"
            ? NmfInitialization.NndSvda
            : NmfInitialization.NndSvd,
        seed: 0,
        randomMatrix: Doubles(c, "omega"));

    private static void AssertSame(double[] expected, double[] actual)
    {
        Assert.Equal(expected.Length, actual.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i], Tolerance);
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void W_matches_scikit_learn(int index)
    {
        JsonElement c = Cases[index];

        AssertSame(Doubles(c, "initial_w"), Initialize(c).W);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void H_matches_scikit_learn(int index)
    {
        JsonElement c = Cases[index];

        AssertSame(Doubles(c, "initial_h"), Initialize(c).H);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void Neither_factor_holds_a_negative_number(int index)
    {
        JsonElement c = Cases[index];

        (double[] w, double[] h) = Initialize(c);

        Assert.All(w, value => Assert.True(value >= 0, $"W holds {value}"));
        Assert.All(h, value => Assert.True(value >= 0, $"H holds {value}"));
    }
}
