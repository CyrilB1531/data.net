namespace Lodestar.Decomposition;

/// <summary>What the factorization is asked to minimise.</summary>
/// <remarks>
/// The two are not interchangeable. Frobenius fits a Gaussian noise model and is what a
/// continuous matrix wants; Kullback–Leibler fits a Poisson one and is what counts want — a
/// term-document matrix included, which is why it is here at all.
/// </remarks>
public enum NmfBetaLoss
{
    /// <summary>Squared Frobenius norm, <c>β = 2</c>.</summary>
    Frobenius = 0,

    /// <summary>Generalised Kullback–Leibler divergence, <c>β = 1</c>.</summary>
    KullbackLeibler = 1,
}
