using System.Text.Json;
using Lodestar.Abstractions;
using Xunit;

namespace Lodestar.Decomposition.Tests;

/// <summary>
/// The wide shape — fewer rows than columns — against <c>randomized_svd(transpose=False)</c>.
/// </summary>
/// <remarks>
/// The reference is the bare function rather than <c>TruncatedSVD</c>, because the estimator's
/// <c>transpose="auto"</c> resolves to True exactly here: it would factorize the transpose and
/// hand back a different factorization to compare against. Only the singular values and the
/// components are frozen, so this class asserts the two the corpus carries and nothing else.
/// </remarks>
public sealed class TruncatedSvdWideTests
{
    private const double Tolerance = 1e-9;

    private static readonly JsonDocument Document = OracleLoader.Load("decomposition_svd.json");

    private static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("randomized_wide").EnumerateArray()];

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

    private static TruncatedSvd Fit(JsonElement c) => TruncatedSvd.Fit(
        Matrix(c),
        c.GetProperty("component_count").GetInt32(),
        new TruncatedSvdOptions
        {
            Oversampling = c.GetProperty("oversampling").GetInt32(),
            PowerIterations = c.GetProperty("power_iterations").GetInt32(),
            Normalizer = c.GetProperty("normalizer").GetString() switch
            {
                "QR" => PowerIterationNormalizer.Qr,
                "LU" => PowerIterationNormalizer.Lu,
                "none" => PowerIterationNormalizer.None,
                _ => PowerIterationNormalizer.Auto,
            },
            RandomMatrix = Doubles(c, "omega"),
        });

    private static void AssertSame(double[] expected, IReadOnlyList<double> actual)
    {
        Assert.Equal(expected.Length, actual.Count);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], actual[i], Tolerance);
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_fixture_has_fewer_rows_than_columns(int index)
    {
        CsrMatrix matrix = Matrix(Cases[index]);

        Assert.True(matrix.RowCount < matrix.ColumnCount, $"{matrix.RowCount} × {matrix.ColumnCount}");
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_singular_values_match_randomized_svd(int index)
    {
        JsonElement c = Cases[index];

        AssertSame(Doubles(c, "singular_values"), Fit(c).SingularValues);
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_components_match_randomized_svd(int index)
    {
        JsonElement c = Cases[index];

        AssertSame(Doubles(c, "components"), Fit(c).Components);
    }
}
