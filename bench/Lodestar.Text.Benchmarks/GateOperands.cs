namespace Lodestar.Text.Benchmarks;

/// <summary>The two bands both gate benchmarks measure over, built once for the pair of them.</summary>
/// <remarks>
/// The classes below run the same experiment on two kernels: one band of Latin and one of CJK,
/// same length and same seed, so the alphabet is the only difference between them. Holding the
/// construction here is what keeps that true — two copies drift, and a drifted copy would read
/// as a difference between the kernels rather than between the corpora (#383).
/// </remarks>
public abstract class GateOperands
{
    /// <summary>The Latin band, whose characters index the kernels' dense equality table.</summary>
    protected string LatinA { get; private set; } = string.Empty;

    /// <inheritdoc cref="LatinA"/>
    protected string LatinB { get; private set; } = string.Empty;

    /// <summary>The CJK band, above U+00FF, whose characters take the side table beside it.</summary>
    protected string CjkA { get; private set; } = string.Empty;

    /// <inheritdoc cref="CjkA"/>
    protected string CjkB { get; private set; } = string.Empty;

    /// <summary>Builds both bands of <paramref name="band"/>, from <c>BandedPair.GateSeed</c>.</summary>
    protected void Build(int band)
    {
        (LatinA, LatinB) = BandedPair.Build(band);
        (CjkA, CjkB) = BandedPair.Build(band, alphabet: Alphabets.Cjk);
    }
}
