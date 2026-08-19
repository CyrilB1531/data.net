namespace Lodestar.Metrics;

/// <summary>How <see cref="CalibrationCurve"/> cuts <c>[0, 1]</c> into bins.</summary>
public enum BinStrategy
{
    /// <summary>Equal-width bins over <c>[0, 1]</c>, whatever the data does — the reference's default.</summary>
    Uniform,

    /// <summary>Edges read off the probabilities themselves, so each bin holds about as many samples.</summary>
    /// <remarks>
    /// Repeated probabilities collapse edges onto each other, which empties bins rather
    /// than balancing them: the strategy equalises rank, not count.
    /// </remarks>
    Quantile,
}
