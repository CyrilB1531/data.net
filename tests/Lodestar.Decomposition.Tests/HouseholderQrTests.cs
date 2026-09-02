using System.Text.Json;
using Lodestar.Decomposition.Internal;
using Xunit;

namespace Lodestar.Decomposition.Tests;

/// <summary>
/// The economic QR, against scipy. A QR is unique only up to a per-column sign, so what is
/// asserted is what the composed algorithm actually relies on: orthonormal columns, an upper
/// triangular R, and a product that reproduces the input.
/// </summary>
public sealed class HouseholderQrTests
{
    private const double Tolerance = 1e-9;

    private static readonly JsonDocument Document = OracleLoader.Load("decomposition_qr.json");

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
    public void The_product_reproduces_the_input(int index)
    {
        JsonElement c = Cases[index];
        int rows = c.GetProperty("rows").GetInt32();
        int columns = c.GetProperty("columns").GetInt32();
        double[] a = Doubles(c, "matrix");

        (double[] q, double[] r) = HouseholderQr.Decompose(a, rows, columns);

        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                double acc = 0;
                for (int k = 0; k < columns; k++)
                {
                    acc += q[(i * columns) + k] * r[(k * columns) + j];
                }
                Assert.Equal(a[(i * columns) + j], acc, Tolerance);
            }
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_columns_of_q_are_orthonormal(int index)
    {
        JsonElement c = Cases[index];
        int rows = c.GetProperty("rows").GetInt32();
        int columns = c.GetProperty("columns").GetInt32();

        (double[] q, _) = HouseholderQr.Decompose(Doubles(c, "matrix"), rows, columns);

        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                double acc = 0;
                for (int k = 0; k < rows; k++)
                {
                    acc += q[(k * columns) + i] * q[(k * columns) + j];
                }
                Assert.Equal(i == j ? 1.0 : 0.0, acc, Tolerance);
            }
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void R_is_upper_triangular(int index)
    {
        JsonElement c = Cases[index];
        int rows = c.GetProperty("rows").GetInt32();
        int columns = c.GetProperty("columns").GetInt32();

        (_, double[] r) = HouseholderQr.Decompose(Doubles(c, "matrix"), rows, columns);

        for (int i = 0; i < columns; i++)
        {
            for (int j = 0; j < i; j++)
            {
                Assert.Equal(0.0, r[(i * columns) + j], Tolerance);
            }
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_singular_values_of_r_match_scipy(int index)
    {
        // R differs from scipy's by a per-column sign; |diag| does not, and it is the
        // cheapest thing that would catch a pivot or a scaling error.
        JsonElement c = Cases[index];
        int columns = c.GetProperty("columns").GetInt32();
        double[] expected = Doubles(c, "scipy_r");

        (_, double[] r) = HouseholderQr.Decompose(
            Doubles(c, "matrix"), c.GetProperty("rows").GetInt32(), columns);

        for (int i = 0; i < columns; i++)
        {
            Assert.Equal(
                Math.Abs(expected[(i * columns) + i]), Math.Abs(r[(i * columns) + i]), Tolerance);
        }
    }

    [Fact]
    public void A_vanished_pivot_leaves_the_factors_finite()
    {
        // Structural rather than a comparison: past a vanished pivot the reflector is built
        // from rounding noise, so the factors are the host's property and not the input's.
        const int rows = 4;
        const int columns = 3;

        // Column 1 is entirely zero and the first reflection leaves it so, its update
        // subtracting a multiple of a zero dot product; step 1 is the vanished-column path.
        double[] a =
        [
            4.0, 0.0, 1.0,
            3.0, 0.0, 0.0,
            0.0, 0.0, 2.0,
            0.0, 0.0, -1.0,
        ];

        (double[] q, double[] r) = HouseholderQr.Decompose(a, rows, columns);

        Assert.All(q, value => Assert.True(double.IsFinite(value)));
        Assert.All(r, value => Assert.True(double.IsFinite(value)));
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                double acc = 0;
                for (int k = 0; k < columns; k++)
                {
                    acc += q[(i * columns) + k] * r[(k * columns) + j];
                }
                Assert.Equal(a[(i * columns) + j], acc, Tolerance);
            }
        }
    }

    [Fact]
    public void A_wide_block_is_refused()
    {
        Assert.Throws<ArgumentException>(() => HouseholderQr.Decompose(new double[6], 2, 3));
    }
}
