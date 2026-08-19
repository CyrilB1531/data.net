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
    }
}
