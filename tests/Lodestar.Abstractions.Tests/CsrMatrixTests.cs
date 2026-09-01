using Xunit;

namespace Lodestar.Abstractions.Tests;

/// <summary>The surface as it moved: same behaviour, new namespace.</summary>
public sealed class CsrMatrixTests
{
    // [[1, 0, 2], [0, 3, 0]] — one row with a gap, one with a single entry.
    private static CsrMatrix Sample() =>
        new(2, 3, [1.0, 2.0, 3.0], [0, 2, 1], [0, 2, 3]);

    [Fact]
    public void The_dense_form_puts_every_value_back_where_it_came_from()
    {
        double[,] dense = Sample().ToDense();

        Assert.Equal(1.0, dense[0, 0]);
        Assert.Equal(0.0, dense[0, 1]);
        Assert.Equal(2.0, dense[0, 2]);
        Assert.Equal(3.0, dense[1, 1]);
    }

    /// <summary>
    /// The three arrays are public and have been since 0.1.0, so they are part of the
    /// contract that moved rather than an implementation detail behind it.
    /// </summary>
    [Fact]
    public void The_raw_arrays_are_the_ones_they_were_built_from()
    {
        CsrMatrix matrix = Sample();

        Assert.Equal([1.0, 2.0, 3.0], matrix.Values);
        Assert.Equal([0, 2, 1], matrix.ColumnIndices);
        Assert.Equal([0, 2, 3], matrix.RowPointers);
        Assert.Equal(3, matrix.NonZeroCount);
        Assert.Equal(2, matrix.RowCount);
        Assert.Equal(3, matrix.ColumnCount);
    }

    [Fact]
    public void Row_norms_read_only_the_row_they_name()
    {
        CsrMatrix matrix = Sample();

        Assert.Equal(3.0, matrix.RowL1Norm(0));
        Assert.Equal(Math.Sqrt(5.0), matrix.RowL2Norm(0), 1e-12);
        Assert.Equal(3.0, matrix.RowL2Norm(1));
    }

    [Fact]
    public void Normalizing_by_L2_leaves_every_row_of_unit_length()
    {
        CsrMatrix matrix = Sample();

        matrix.NormalizeRows(SparseNorm.L2);

        Assert.Equal(1.0, matrix.RowL2Norm(0), 1e-12);
        Assert.Equal(1.0, matrix.RowL2Norm(1), 1e-12);
    }

    [Fact]
    public void The_vector_product_skips_the_zeros() =>
        Assert.Equal([7.0, 6.0], Sample().Multiply([1.0, 2.0, 3.0]));

    [Fact]
    public void A_vector_of_the_wrong_length_is_refused() =>
        Assert.Throws<ArgumentException>(() => Sample().Multiply([1.0, 2.0]));

    [Fact]
    public void Arrays_that_do_not_describe_a_matrix_are_refused() =>
        Assert.Throws<ArgumentException>(
            () => new CsrMatrix(2, 3, [1.0], [0, 2], [0, 2, 3]));

    /// <summary>
    /// The unchecked factory is what the vectorizers use, and it stays internal after
    /// the move: <c>InternalsVisibleTo</c> is what keeps step B compiling.
    /// </summary>
    [Fact]
    public void The_unchecked_factory_is_reachable_from_a_friend_assembly() =>
        Assert.Equal(3, CsrMatrix.CreateUnchecked(2, 3, [1.0, 2.0, 3.0], [0, 2, 1], [0, 2, 3])
                                 .NonZeroCount);
}
