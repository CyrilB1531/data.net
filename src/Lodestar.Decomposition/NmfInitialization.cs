namespace Lodestar.Decomposition;

/// <summary>Where a non-negative matrix factorization starts from.</summary>
/// <remarks>
/// Multiplicative updates never introduce a non-zero where the initialisation put a zero, so the
/// initialisation decides the sparsity of the answer as much as the data does.
/// scikit-learn's <c>nndsvdar</c> is not offered: it fills its zeros from numpy's own normal
/// stream, which nothing here reproduces, so it could not be checked against the reference.
/// </remarks>
public enum NmfInitialization
{
    /// <summary>Non-negative double SVD. Leaves zeros in place, which keeps the factors sparse.</summary>
    NndSvd = 0,

    /// <summary>NNDSVD with the zeros filled by the matrix's mean. Denser, and it converges faster.</summary>
    NndSvda = 1,
}
