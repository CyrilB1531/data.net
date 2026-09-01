using Xunit;

namespace Lodestar.Abstractions.Tests;

/// <summary>
/// The public constructor takes raw arrays, so it is the boundary a deserialized
/// matrix crosses. Each case here would otherwise become an out-of-bounds read in
/// <see cref="CsrMatrix.ToDense"/> or <see cref="CsrMatrix.Multiply"/> — a caller
/// discipline problem before there was I/O, a memory-safety one after.
/// </summary>
public sealed class CsrMatrixValidationTests
{
    [Fact]
    public void A_well_formed_matrix_is_accepted()
    {
        var matrix = new CsrMatrix(2, 3, [1.0, 2.0, 3.0], [0, 2, 1], [0, 2, 3]);

        Assert.Equal(2, matrix.RowCount);
        Assert.Equal(3, matrix.NonZeroCount);
    }

    [Fact]
    public void An_empty_matrix_is_accepted()
    {
        var matrix = new CsrMatrix(0, 0, [], [], [0]);

        Assert.Equal(0, matrix.NonZeroCount);
    }

    [Fact]
    public void Row_pointers_that_do_not_start_at_zero_are_rejected()
    {
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new CsrMatrix(2, 3, [1.0, 2.0], [0, 1], [1, 2, 2]));

        Assert.Contains("rowPointers[0] must be 0", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Row_pointers_that_go_backwards_are_rejected()
    {
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new CsrMatrix(3, 3, [1.0, 2.0], [0, 1], [0, 2, 1, 2]));

        Assert.Contains("non-decreasing", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Row_pointers_that_do_not_end_at_the_value_count_are_rejected()
    {
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new CsrMatrix(2, 3, [1.0, 2.0, 3.0], [0, 1, 2], [0, 1, 2]));

        Assert.Contains("must end at the number of stored values (3)", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_column_index_past_the_last_column_is_rejected()
    {
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new CsrMatrix(1, 2, [1.0], [5], [0, 1]));

        Assert.Contains("columnIndices[0] = 5 is outside the column range [0, 2)", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_negative_column_index_is_rejected()
    {
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new CsrMatrix(1, 2, [1.0], [-1], [0, 1]));

        Assert.Contains("outside the column range", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_row_pointer_array_of_the_wrong_length_is_rejected()
    {
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new CsrMatrix(2, 2, [1.0], [0], [0, 1]));

        Assert.Contains("rowPointers length must be rowCount + 1", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Values_and_column_indices_of_different_lengths_are_rejected()
    {
        Assert.Throws<ArgumentException>(() => new CsrMatrix(1, 2, [1.0, 2.0], [0], [0, 2]));
    }

    [Fact]
    public void A_negative_row_or_column_count_is_rejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new CsrMatrix(-1, 2, [], [], [0]));
        Assert.Throws<ArgumentOutOfRangeException>(() => new CsrMatrix(0, -2, [], [], [0]));
    }

    [Fact]
    public void Null_arrays_are_rejected()
    {
        Assert.Throws<ArgumentNullException>(() => new CsrMatrix(1, 1, null!, [0], [0, 1]));
        Assert.Throws<ArgumentNullException>(() => new CsrMatrix(1, 1, [1.0], null!, [0, 1]));
        Assert.Throws<ArgumentNullException>(() => new CsrMatrix(1, 1, [1.0], [0], null!));
    }
}
