using BenchmarkDotNet.Attributes;
using Lodestar.Abstractions;
using Lodestar.Decomposition;
using Microsoft.ML;
using Microsoft.ML.Data;

namespace Lodestar.Text.Benchmarks;

/// <summary>One row of the dense twin ML.NET's <c>ProjectToPrincipalComponents</c> reads.</summary>
public sealed class DenseCorpusRow
{
    // CA1819 (properties should not return arrays): ML.NET binds its input column by
    // reflecting over a settable float[] property, so the rule's advice is not available.
#pragma warning disable CA1819
    /// <summary>The row, densified from the same <see cref="CsrMatrix"/> the sparse side reads.</summary>
    [VectorType(DecompositionBenchmarks.Columns)]
    public float[] Features { get; set; } = [];
#pragma warning restore CA1819
}

/// <summary>One row of the projection ML.NET's <c>ProjectToPrincipalComponents</c> produces.</summary>
public sealed class ProjectedCorpusRow
{
#pragma warning disable CA1819
    /// <summary>The rank-20 projection.</summary>
    [VectorType]
    public float[] Projected { get; set; } = [];
#pragma warning restore CA1819
}

/// <summary>
/// <see cref="TruncatedSvd"/> and <see cref="Nmf"/> beside ML.NET's
/// <c>ProjectToPrincipalComponents</c> — issue #438's per-package incumbent, and not a
/// like-for-like one: ML.NET centres and fixes the rank at 20 over an
/// <see cref="IDataView"/>, <see cref="TruncatedSvd"/> does neither over a
/// <see cref="CsrMatrix"/>, and <see cref="Nmf"/>'s basis is non-negative. Agreement between
/// three different decompositions cannot be checked; bench/README.md's section for this class
/// says what is checked instead, and carries the numbers once taken on a named machine.
/// </summary>
public class DecompositionBenchmarks
{
    /// <summary>Rows of the fixed corpus.</summary>
    private const int Rows = 2000;

    /// <summary>Columns of the fixed corpus.</summary>
    public const int Columns = 500;

    /// <summary>
    /// Non-zeros per row that put the corpus at 2% density (<c>Columns * 0.02</c>), computed once
    /// rather than re-derived at every call so the fixed shape reads as one fact.
    /// </summary>
    private const int NonZerosPerRow = 10;

    /// <summary>Seeds both the corpus and every fit, so two runs measure the same matrix.</summary>
    private const int Seed = 20260901;

    private CsrMatrix _matrix = null!;
    private MLContext _ml = null!;
    private IDataView _denseView = null!;

    [GlobalSetup]
    public void Setup()
    {
        _matrix = BuildSparseCorpus();

        // No CsrMatrix overload exists: the dense twin comes from the same values,
        // not a second, independently seeded draw.
        _ml = new MLContext(seed: Seed);
        _denseView = _ml.Data.LoadFromEnumerable(ToDenseRows(_matrix));
    }

    /// <summary>Truncated SVD, uncentred, at the rank the caller names.</summary>
    [Benchmark(Baseline = true)]
    public int TruncatedSvd_Rank20() =>
        TruncatedSvd.Fit(_matrix, 20, new TruncatedSvdOptions { Seed = Seed }).ComponentCount;

    /// <summary>Non-negative factorization at the same rank, capped rather than run to convergence.</summary>
    [Benchmark]
    public int Nmf_Rank20() =>
        Nmf.Fit(_matrix, 20, new NmfOptions { Seed = Seed, MaxIterations = 50 }).Iterations;

    /// <summary>
    /// ML.NET's centred PCA, fixed at rank 20. The pipeline is built and fit inside the
    /// measured region, on purpose: <see cref="TruncatedSvd_Rank20"/> and
    /// <see cref="Nmf_Rank20"/> both call their package's <c>Fit</c> as the thing being
    /// measured, and a row that instead measured a pre-fit model would not answer the same
    /// question. <c>Fit</c> is where PCA's own work happens — the projection <c>Transform</c>
    /// applies afterwards is a cheap dense multiply — so this is not folding hidden setup into
    /// the number the way that reasoning would if the arithmetic ran the other way.
    /// </summary>
    [Benchmark]
    public int MlNet_ProjectToPrincipalComponents_Rank20()
    {
        var pipeline = _ml.Transforms.ProjectToPrincipalComponents(
            "Projected", nameof(DenseCorpusRow.Features), rank: 20, ensureZeroMean: true, seed: Seed);
        var model = pipeline.Fit(_denseView);
        var transformed = model.Transform(_denseView);

        int total = 0;
        foreach (var row in _ml.Data.CreateEnumerable<ProjectedCorpusRow>(transformed, reuseRowObject: true))
        {
            total += row.Projected.Length;
        }
        return total;
    }

    /// <summary>
    /// A sparse term-document matrix, values drawn like term counts (positive, unbounded) so
    /// <see cref="Nmf"/>'s non-negativity requirement holds without post-processing. Distinct
    /// column indices per row rather than allowing repeats: a repeated index would still be a
    /// valid <see cref="CsrMatrix"/>, but it would understate <see cref="NonZerosPerRow"/>'s
    /// share of the row once summed, which is not the fixed density this corpus promises.
    /// </summary>
    // SonarLint S2245, CA5394: a seeded Random builds a reproducible benchmark corpus, read
    // for its density and column spread rather than for anything security-sensitive; every use
    // of it is local to this method.
#pragma warning disable S2245, CA5394
    private static CsrMatrix BuildSparseCorpus()
    {
        var rng = new Random(Seed);
        int nonZeroCount = Rows * NonZerosPerRow;
        var values = new double[nonZeroCount];
        var columnIndices = new int[nonZeroCount];
        var rowPointers = new int[Rows + 1];

        int cursor = 0;
        for (int row = 0; row < Rows; row++)
        {
            var chosen = new HashSet<int>();
            while (chosen.Count < NonZerosPerRow)
            {
                chosen.Add(rng.Next(Columns));
            }
            foreach (int column in chosen.OrderBy(c => c))
            {
                columnIndices[cursor] = column;
                values[cursor] = 1 + (rng.NextDouble() * 4);
                cursor++;
            }
            rowPointers[row + 1] = cursor;
        }

        return new CsrMatrix(Rows, Columns, values, columnIndices, rowPointers);
    }
#pragma warning restore S2245, CA5394

    /// <summary>The dense twin, row for row and value for value, ML.NET's PCA transform needs.</summary>
    private static DenseCorpusRow[] ToDenseRows(CsrMatrix matrix)
    {
        double[,] dense = matrix.ToDense();
        var rows = new DenseCorpusRow[matrix.RowCount];
        for (int row = 0; row < matrix.RowCount; row++)
        {
            var features = new float[matrix.ColumnCount];
            for (int column = 0; column < matrix.ColumnCount; column++)
            {
                features[column] = (float)dense[row, column];
            }
            rows[row] = new DenseCorpusRow { Features = features };
        }
        return rows;
    }
}
