namespace Lodestar.Decomposition;

/// <summary>What a power iteration does to its block between the two products.</summary>
/// <remarks>
/// A power iteration sharpens the spectrum and, left alone, collapses every column onto the
/// leading singular vector — in double precision, within a handful of iterations. The normalizer
/// is what stops that, and which one is used changes the answer, so it is frozen in the corpus
/// rather than chosen by the implementation.
/// </remarks>
public enum PowerIterationNormalizer
{
    /// <summary><see cref="None"/> below three power iterations, <see cref="Lu"/> at or above — scikit-learn's rule.</summary>
    Auto = 0,

    /// <summary>Nothing between the products. Cheapest, and adequate only for one or two iterations.</summary>
    None = 1,

    /// <summary>An economic QR. The most accurate, and the most expensive.</summary>
    Qr = 2,

    /// <summary>LU with partial pivoting. What <see cref="Auto"/> resolves to at scikit-learn's own default of five iterations.</summary>
    Lu = 3,
}
