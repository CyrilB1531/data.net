using System.Text.Json;
using Lodestar.Decomposition.Internal;
using Xunit;

namespace Lodestar.Decomposition.Tests;

/// <summary>
/// <c>scipy.linalg.lu(permute_l=True)</c>'s first factor, which is the whole of what the
/// <c>LU</c> power-iteration normalizer uses.
/// </summary>
public sealed class PartialPivotLuTests
{
    private const double Tolerance = 1e-9;

    private static readonly JsonDocument Document = OracleLoader.Load("decomposition_lu.json");

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

    private static double[] Doubles(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetDouble())];

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_permuted_lower_factor_matches_scipy(int index)
    {
        JsonElement c = Cases[index];

        double[] pl = PartialPivotLu.PermutedLower(
            Doubles(c, "matrix"), c.GetProperty("rows").GetInt32(),
            c.GetProperty("columns").GetInt32());

        double[] expected = Doubles(c, "permuted_lower");
        Assert.Equal(expected.Length, pl.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], pl[i], Tolerance);
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_product_with_scipys_upper_reproduces_the_input(int index)
    {
        // Independent of the factor assertion above: it would still catch a P L that is
        // self-consistent and wrong, because U comes from scipy rather than from here.
        JsonElement c = Cases[index];
        int rows = c.GetProperty("rows").GetInt32();
        int columns = c.GetProperty("columns").GetInt32();
        double[] a = Doubles(c, "matrix");
        double[] u = Doubles(c, "upper");

        double[] pl = PartialPivotLu.PermutedLower(a, rows, columns);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                double acc = 0;
                for (int k = 0; k < columns; k++)
                {
                    acc += pl[(i * columns) + k] * u[(k * columns) + j];
                }
                Assert.Equal(a[(i * columns) + j], acc, Tolerance);
            }
        }
    }

    [Fact]
    public void A_vanished_pivot_leaves_the_permuted_lower_factor_finite()
    {
        // Structural rather than a comparison: past a vanished pivot the elimination runs on
        // rounding noise, so the factor is the host's property and not the input's.
        const int rows = 4;
        const int columns = 3;

        // Column 1 is entirely zero, so step 1 finds no non-zero pivot to swap in and would
        // divide by an exactly zero head — the path EliminateColumn guards.
        double[] a =
        [
            4.0, 0.0, 1.0,
            3.0, 0.0, 0.0,
            0.0, 0.0, 2.0,
            0.0, 0.0, -1.0,
        ];

        double[] pl = PartialPivotLu.PermutedLower(a, rows, columns);

        Assert.All(pl, value => Assert.True(double.IsFinite(value)));
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                if (i == j)
                {
                    Assert.Equal(1.0, pl[(i * columns) + j]);
                }
                else if (j > i)
                {
                    Assert.Equal(0.0, pl[(i * columns) + j]);
                }
            }
        }
    }

    [Fact]
    public void A_wide_block_is_refused()
    {
        Assert.Throws<ArgumentException>(() => PartialPivotLu.PermutedLower(new double[6], 2, 3));
    }
}
