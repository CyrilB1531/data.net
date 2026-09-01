using Lodestar.Abstractions;
using Lodestar.Text.Vectorization;

namespace Lodestar.Sample;

/// <summary>The sparse exchange format between every vectorization stage.</summary>
internal static class CsrMatrixSample
{
    public static void Run()
    {
        CsrMatrix matrix = new CountVectorizer(TextCorpus.Counting()).FitTransform(TextCorpus.Documents);

        matrix.NormalizeRows(SparseNorm.L2);
        Console.WriteLine($"  row 0 L2 / L1    = {Inv.F4(matrix.RowL2Norm(0))} / {Inv.F4(matrix.RowL1Norm(0))}");
        Console.WriteLine($"  CSR arrays       : values={matrix.Values.Length} cols={matrix.ColumnIndices.Length} ptrs={matrix.RowPointers.Length}");

        double[] product = matrix.Multiply(new double[matrix.ColumnCount]);
        double[,] dense = matrix.ToDense();
        Console.WriteLine($"  Multiply / ToDense: {product.Length} rows, dense {dense.GetLength(0)}x{dense.GetLength(1)}");

        // Built by hand: the constructor is public API of Lodestar.Abstractions, and
        // this is the only place that proves it reachable once packaged.
        CsrMatrix built = new(2, 3, [1.0, 2.0, 3.0], [0, 2, 1], [0, 2, 3]);

        // The block forms are what a power iteration multiplies by: one pass over the
        // non-zeros rather than one per column of the dense operand.
        Console.WriteLine($"  · 3x2 block      = {Inv.List(built.Multiply([1.0, 0.5, 2.0, 1.5, 3.0, 2.5], 2))}");
        Console.WriteLine($"  transposed · 2x2 = {Inv.List(built.TransposeMultiply([1.0, 0.5, 2.0, 1.5], 2))}");
    }
}
