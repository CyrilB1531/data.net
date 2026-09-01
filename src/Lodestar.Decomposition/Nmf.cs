using Lodestar.Abstractions;
using Lodestar.Decomposition.Internal;

namespace Lodestar.Decomposition;

/// <summary>What <see cref="Nmf.Fit(CsrMatrix, int, NmfOptions)"/> is allowed to vary.</summary>
public sealed class NmfOptions
{
    /// <summary>What the factorization minimises.</summary>
    public NmfBetaLoss BetaLoss { get; init; } = NmfBetaLoss.Frobenius;

    /// <summary>Where the iteration starts. Ignored by the overload that is handed W and H.</summary>
    public NmfInitialization Initialization { get; init; } = NmfInitialization.NndSvd;

    /// <summary>The iteration cap. scikit-learn's default is 200.</summary>
    public int MaxIterations { get; init; } = 200;

    /// <summary>The relative improvement below which the iteration stops, checked every ten.</summary>
    /// <remarks>
    /// Zero disables the stop, which turns <see cref="MaxIterations"/> into an input rather than
    /// a cap — that is what the oracle corpus does, so an iteration count cannot silently differ.
    /// </remarks>
    public double Tolerance { get; init; } = 1e-4;

    /// <summary>Seeds the initialisation's own generator when <see cref="RandomMatrix"/> is null.</summary>
    public int Seed { get; init; }

    /// <summary>Ω for the initialisation, row-major <c>features × (components + 10)</c>.</summary>
    // CA1819 (properties should not return arrays): the same bargain
    // TruncatedSvdOptions.RandomMatrix strikes. Ω is a dense block the caller already
    // holds — copying it defensively would double the largest allocation the fit makes,
    // to protect a value this type reads once and never keeps.
#pragma warning disable CA1819
    public double[]? RandomMatrix { get; init; }
#pragma warning restore CA1819
}

/// <summary>A fitted non-negative matrix factorization, <c>X ≈ W H</c>.</summary>
/// <remarks>
/// There is no unfitted state and no <c>Transform</c>: projecting an unseen row onto a
/// non-negative basis is itself a factorization — the same multiplicative loop with H held
/// fixed — rather than the product a name borrowed from the SVD would suggest.
/// </remarks>
public sealed class Nmf
{
    private readonly double[] _weights;
    private readonly double[] _components;

    private Nmf(int featureCount, int componentCount, double[] weights, double[] components,
                int iterations, double reconstructionError)
    {
        FeatureCount = featureCount;
        ComponentCount = componentCount;
        _weights = weights;
        _components = components;
        Iterations = iterations;
        ReconstructionError = reconstructionError;
    }

    /// <summary>How many components were asked for.</summary>
    public int ComponentCount { get; }

    /// <summary>How many columns the factorized matrix had.</summary>
    public int FeatureCount { get; }

    /// <summary>How many multiplicative updates ran — scikit-learn's <c>n_iter_</c>.</summary>
    public int Iterations { get; }

    /// <summary>The beta divergence at the end, square-rooted — scikit-learn's <c>reconstruction_err_</c>.</summary>
    public double ReconstructionError { get; }

    /// <summary><c>W</c>, row-major rows × <see cref="ComponentCount"/>: each row's mix of components.</summary>
    public IReadOnlyList<double> Weights => _weights;

    /// <summary><c>H</c>, row-major <see cref="ComponentCount"/> × <see cref="FeatureCount"/> — scikit-learn's <c>components_</c>.</summary>
    public IReadOnlyList<double> Components => _components;

    /// <summary>Factorizes <paramref name="matrix"/>, initialising it with the NNDSVD family.</summary>
    /// <param name="matrix">The non-negative matrix to factorize, rows as samples and columns as features.</param>
    /// <param name="componentCount">How many components to keep.</param>
    /// <param name="options">The solver's settings, or null for scikit-learn's defaults.</param>
    /// <exception cref="ArgumentNullException"><paramref name="matrix"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="componentCount"/> is not in <c>[1, matrix.ColumnCount)</c>, is above <c>matrix.RowCount</c>, or an option is out of range.</exception>
    /// <exception cref="ArgumentException"><paramref name="matrix"/> holds a negative value, or <see cref="NmfOptions.RandomMatrix"/> is not <c>matrix.ColumnCount × (componentCount + 10)</c>.</exception>
    public static Nmf Fit(CsrMatrix matrix, int componentCount, NmfOptions? options = null)
    {
        Guard.NotNull(matrix);
        NmfOptions settings = options ?? new NmfOptions();
        if (componentCount < 1 || componentCount >= matrix.ColumnCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(componentCount), componentCount,
                $"A factorization keeps between 1 and {matrix.ColumnCount - 1} components; " +
                "Fit(matrix, initialWeights, initialComponents) reads the rank off the blocks " +
                "instead and carries no column bound.");
        }
        if (componentCount > matrix.RowCount)
        {
            // A rank above the row count survives the range finder and breaks the truncation
            // once the economic QR narrows the block, which is a stack trace and not an answer.
            throw new ArgumentOutOfRangeException(
                nameof(componentCount), componentCount,
                $"A matrix of {matrix.RowCount} rows has no more than that many components.");
        }

        int size = componentCount + NndSvd.Oversampling;
        if (settings.RandomMatrix is { } omega && omega.Length != (long)matrix.ColumnCount * size)
        {
            throw new ArgumentException(
                $"Ω is {omega.Length} long, not {matrix.ColumnCount} × {size}.", nameof(options));
        }

        RequireNonNegativeMatrix(matrix);
        (double[] w, double[] h) = NndSvd.Initialize(
            matrix, componentCount, settings.Initialization, settings.Seed, settings.RandomMatrix);
        return Fit(matrix, w, h, settings);
    }

    /// <summary>Factorizes <paramref name="matrix"/> from an initialisation you supply.</summary>
    /// <param name="matrix">The non-negative matrix to factorize.</param>
    /// <param name="initialWeights">W₀, row-major <c>matrix.RowCount × componentCount</c> and non-negative.</param>
    /// <param name="initialComponents">H₀, row-major <c>componentCount × matrix.ColumnCount</c> and non-negative.</param>
    /// <param name="options">The solver's settings, or null for scikit-learn's defaults.</param>
    /// <exception cref="ArgumentNullException">An argument is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="matrix"/> holds a negative value, or the two blocks do not agree on a component count, do not fit the matrix, or hold a negative number.</exception>
    /// <exception cref="ArgumentOutOfRangeException">An option is out of range.</exception>
    public static Nmf Fit(
        CsrMatrix matrix, double[] initialWeights, double[] initialComponents,
        NmfOptions? options = null)
    {
        Guard.NotNull(matrix);
        Guard.NotNull(initialWeights);
        Guard.NotNull(initialComponents);
        NmfOptions settings = options ?? new NmfOptions();
        Validate(settings);

        int features = matrix.ColumnCount;
        int componentCount = ComponentCountOf(matrix, initialWeights, initialComponents, features);
        RequireNonNegativeMatrix(matrix);
        RequireNonNegative(initialWeights, nameof(initialWeights));
        RequireNonNegative(initialComponents, nameof(initialComponents));

        double[] w = (double[])initialWeights.Clone();
        double[] h = (double[])initialComponents.Clone();

        double initial = BetaDivergence.Compute(matrix, w, h, componentCount, settings.BetaLoss);
        double previous = initial;
        int iteration = 0;
        while (iteration < settings.MaxIterations)
        {
            iteration++;
            MultiplicativeUpdates.UpdateWeights(matrix, w, h, componentCount, settings.BetaLoss);
            MultiplicativeUpdates.UpdateComponents(matrix, w, h, componentCount, settings.BetaLoss);

            // scikit-learn checks every tenth iteration, never on the others: checking more
            // often would stop earlier, on the same data, for no reason a caller can see.
            if (settings.Tolerance > 0 && iteration % 10 == 0)
            {
                double error = BetaDivergence.Compute(
                    matrix, w, h, componentCount, settings.BetaLoss);
                if ((previous - error) / initial < settings.Tolerance)
                {
                    break;
                }
                previous = error;
            }
        }

        double final = BetaDivergence.Compute(matrix, w, h, componentCount, settings.BetaLoss);
        return new Nmf(features, componentCount, w, h, iteration, final);
    }

    /// <summary>The two settings the loop itself cannot survive, refused before it starts.</summary>
    /// <remarks>
    /// The parameter is named <c>options</c> rather than <c>settings</c> so that every
    /// <see cref="ArgumentException.ParamName"/> it produces names a parameter of the
    /// overload the caller wrote, which is the only signature they can read.
    /// </remarks>
    private static void Validate(NmfOptions options)
    {
        if (options.MaxIterations < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(options), options.MaxIterations, "MaxIterations is at least one.");
        }
        if (options.Tolerance < 0 || double.IsNaN(options.Tolerance))
        {
            throw new ArgumentOutOfRangeException(
                nameof(options), options.Tolerance, "Tolerance is not negative.");
        }
    }

    private static int ComponentCountOf(
        CsrMatrix matrix, double[] initialWeights, double[] initialComponents, int features)
    {
        if (initialWeights.Length % matrix.RowCount != 0)
        {
            throw new ArgumentException(
                $"W is {initialWeights.Length} long, which is not a multiple of {matrix.RowCount} rows.",
                nameof(initialWeights));
        }
        int componentCount = initialWeights.Length / matrix.RowCount;
        if (componentCount < 1 || initialComponents.Length != (long)componentCount * features)
        {
            throw new ArgumentException(
                $"W implies {componentCount} components, so H must be {componentCount} × {features}; " +
                $"it is {initialComponents.Length} long.",
                nameof(initialComponents));
        }
        return componentCount;
    }

    private static void RequireNonNegative(double[] block, string name)
    {
        int index = FirstNegative(block);
        if (index >= 0)
        {
            throw new ArgumentException(
                $"A non-negative factorization cannot start from {block[index]}.", name);
        }
    }

    private static void RequireNonNegativeMatrix(CsrMatrix matrix)
    {
        int index = FirstNegative(matrix.Values);
        if (index >= 0)
        {
            throw new ArgumentException(
                $"A non-negative factorization needs a non-negative matrix, and this one holds {matrix.Values[index]}.",
                nameof(matrix));
        }
    }

    /// <summary>Where the first value a non-negative factorization cannot use sits, or -1.</summary>
    private static int FirstNegative(double[] block) =>
        Array.FindIndex(block, value => double.IsNaN(value) || value < 0);
}
