using System.Text.Json;
using Lodestar.Decomposition.Internal;
using Xunit;

namespace Lodestar.Decomposition.Tests;

/// <summary>
/// The dense factorization the whole method exists to reach, on its own and against scipy.
/// Singular values are unique and asserted directly; the vectors are unique only up to a
/// per-triplet sign, so what is asserted of them is the reconstruction and orthonormality.
/// </summary>
public sealed class JacobiSvdTests
{
    private const double Tolerance = 1e-9;

    private static readonly JsonDocument Document = OracleLoader.Load("decomposition_svd.json");

    private static IReadOnlyList<JsonElement> Cases { get; } =
        [.. Document.RootElement.GetProperty("dense").EnumerateArray()];

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

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_singular_values_match_scipy(int index)
    {
        JsonElement c = Cases[index];

        (_, double[] s, _) = JacobiSvd.Decompose(
            Doubles(c, "matrix"), c.GetProperty("rows").GetInt32(),
            c.GetProperty("columns").GetInt32());

        double[] expected = Doubles(c, "singular_values");
        Assert.Equal(expected.Length, s.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], s[i], Tolerance);
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_factors_reproduce_the_input(int index)
    {
        JsonElement c = Cases[index];
        int rows = c.GetProperty("rows").GetInt32();
        int columns = c.GetProperty("columns").GetInt32();
        double[] a = Doubles(c, "matrix");
        int rank = Math.Min(rows, columns);

        (double[] u, double[] s, double[] vt) = JacobiSvd.Decompose(a, rows, columns);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                double acc = 0;
                for (int k = 0; k < rank; k++)
                {
                    acc += u[(i * rank) + k] * s[k] * vt[(k * columns) + j];
                }
                Assert.Equal(a[(i * columns) + j], acc, Tolerance);
            }
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_singular_values_come_out_in_descending_order(int index)
    {
        JsonElement c = Cases[index];

        (_, double[] s, _) = JacobiSvd.Decompose(
            Doubles(c, "matrix"), c.GetProperty("rows").GetInt32(),
            c.GetProperty("columns").GetInt32());

        for (int i = 1; i < s.Length; i++)
        {
            Assert.True(s[i - 1] >= s[i], $"s[{i - 1}] = {s[i - 1]} < s[{i}] = {s[i]}");
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_rows_of_vt_are_orthonormal(int index)
    {
        JsonElement c = Cases[index];
        int rows = c.GetProperty("rows").GetInt32();
        int columns = c.GetProperty("columns").GetInt32();
        int rank = Math.Min(rows, columns);

        (_, _, double[] vt) = JacobiSvd.Decompose(Doubles(c, "matrix"), rows, columns);

        for (int i = 0; i < rank; i++)
        {
            for (int j = 0; j < rank; j++)
            {
                double acc = 0;
                for (int k = 0; k < columns; k++)
                {
                    acc += vt[(i * columns) + k] * vt[(j * columns) + k];
                }
                Assert.Equal(i == j ? 1.0 : 0.0, acc, Tolerance);
            }
        }
    }
}
