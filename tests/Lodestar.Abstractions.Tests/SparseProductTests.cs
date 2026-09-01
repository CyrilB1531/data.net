using System.Text.Json;
using Xunit;

namespace Lodestar.Abstractions.Tests;

/// <summary>The sparse-dense products, against scipy.</summary>
public sealed class SparseProductTests
{
    private const double Tolerance = 1e-9;

    private static readonly JsonDocument Document = OracleLoader.Load("sparse_matmul.json");

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

    private static int[] Ints(JsonElement c, string name) =>
        [.. c.GetProperty(name).EnumerateArray().Select(x => x.GetInt32())];

    private static CsrMatrix Matrix(JsonElement c) => new(
        c.GetProperty("rows").GetInt32(),
        c.GetProperty("columns").GetInt32(),
        Doubles(c, "values"),
        Ints(c, "column_indices"),
        Ints(c, "row_pointers"));

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_dense_block_product_matches_scipy(int index)
    {
        JsonElement c = Cases[index];
        int width = c.GetProperty("block_columns").GetInt32();

        double[] product = Matrix(c).Multiply(Doubles(c, "block"), width);

        double[] expected = Doubles(c, "product");
        Assert.Equal(expected.Length, product.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], product[i], Tolerance);
        }
    }

    [Theory]
    [MemberData(nameof(Indices))]
    public void The_transposed_product_matches_scipy(int index)
    {
        JsonElement c = Cases[index];
        int width = c.GetProperty("block_columns").GetInt32();

        double[] product = Matrix(c).TransposeMultiply(Doubles(c, "transpose_block"), width);

        double[] expected = Doubles(c, "transpose_product");
        Assert.Equal(expected.Length, product.Length);
        for (int i = 0; i < expected.Length; i++)
        {
            Assert.Equal(expected[i], product[i], Tolerance);
        }
    }

    [Fact]
    public void A_block_whose_length_does_not_fit_the_matrix_is_refused()
    {
        CsrMatrix matrix = new(2, 3, [1.0], [0], [0, 1, 1]);

        Assert.Throws<ArgumentException>(() => matrix.Multiply([1.0, 2.0, 3.0, 4.0], 2));
        Assert.Throws<ArgumentException>(() => matrix.TransposeMultiply([1.0, 2.0, 3.0], 2));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void A_block_width_that_is_not_positive_is_refused(int width)
    {
        CsrMatrix matrix = new(2, 3, [1.0], [0], [0, 1, 1]);

        Assert.Throws<ArgumentOutOfRangeException>(() => matrix.Multiply([], width));
        Assert.Throws<ArgumentOutOfRangeException>(() => matrix.TransposeMultiply([], width));
    }

    /// <summary>
    /// A one-column block is the vector product, which is what makes the wider one
    /// worth having rather than a loop the caller writes.
    /// </summary>
    [Fact]
    public void One_column_agrees_with_the_vector_overload()
    {
        CsrMatrix matrix = new(2, 3, [1.0, 2.0, 3.0], [0, 2, 1], [0, 2, 3]);

        Assert.Equal(matrix.Multiply([1.0, 2.0, 3.0]), matrix.Multiply([1.0, 2.0, 3.0], 1));
    }
}
