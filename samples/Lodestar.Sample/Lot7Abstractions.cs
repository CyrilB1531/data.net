using Lodestar.Abstractions;

namespace Lodestar.Sample;

/// <summary>
/// Lot 7 — Lodestar.Abstractions, the sparse primitive the other packages share.
/// </summary>
/// <remarks>
/// Named for a lot rather than for its class because <c>CsrMatrixSample.cs</c> still
/// demonstrates <c>Lodestar.Text</c>'s copy; the two exist together until that copy goes.
/// </remarks>
internal static class Lot7Abstractions
{
    // [[1, 0, 2], [0, 3, 0]] — three non-zeros, one gap, in CSR.
    private static readonly double[] Values = [1.0, 2.0, 3.0];
    private static readonly int[] Columns = [0, 2, 1];
    private static readonly int[] RowPointers = [0, 2, 3];

    // A 3 x 2 dense block, row-major: the shape a power iteration multiplies by.
    private static readonly double[] Block = [1.0, 0.5, 2.0, 1.5, 3.0, 2.5];

    public static void Run()
    {
        Console.WriteLine("lot 7 — the shared sparse primitive");

        CsrMatrix matrix = new(2, 3, Values, Columns, RowPointers);
        Console.WriteLine($"  shape                 = {Inv.F0(matrix.RowCount)} x {Inv.F0(matrix.ColumnCount)}, "
            + $"{Inv.F0(matrix.NonZeroCount)} non-zeros");
        Console.WriteLine($"  row 0 norms           = L1 {Inv.F3(matrix.RowL1Norm(0))}, L2 {Inv.F3(matrix.RowL2Norm(0))}");
        Console.WriteLine($"  values / indices      = {Inv.List(matrix.Values)} / [{string.Join(", ", matrix.ColumnIndices)}]");
        Console.WriteLine($"  row pointers          = [{string.Join(", ", matrix.RowPointers)}]");

        double[,] dense = matrix.ToDense();
        Console.WriteLine($"  dense row 0           = [{Inv.F1(dense[0, 0])}, {Inv.F1(dense[0, 1])}, {Inv.F1(dense[0, 2])}]");
        Console.WriteLine($"  · vector              = {Inv.List(matrix.Multiply([1.0, 2.0, 3.0]))}");
        Console.WriteLine($"  · 3x2 block           = {Inv.List(matrix.Multiply(Block, 2))}");
        Console.WriteLine($"  transposed · 2x2      = {Inv.List(matrix.TransposeMultiply([1.0, 0.5, 2.0, 1.5], 2))}");

        // NormalizeRows mutates, so it runs last and on a copy of the arrays.
        CsrMatrix scaled = new(2, 3, [.. Values], [.. Columns], [.. RowPointers]);
        scaled.NormalizeRows(SparseNorm.L2);
        Console.WriteLine($"  L2-normalized values  = {Inv.List(scaled.Values)}");
        Console.WriteLine();
    }
}
