namespace Lodestar.Metrics;

/// <summary>
/// The share of sample pairs two labellings agree about, uncorrected for chance —
/// the equivalent of <c>sklearn.metrics.rand_score</c>.
/// </summary>
public static class RandIndex
{
    /// <summary>Scores the fraction of sample pairs the two labellings treat the same way — <c>sklearn.metrics.rand_score(labels_true, labels_pred)</c>.</summary>
    /// <param name="labelsTrue">The reference labelling.</param>
    /// <param name="labelsPred">The labelling to score, same length as <paramref name="labelsTrue"/>.</param>
    /// <returns><c>double</c> in <c>[0, 1]</c>: the pairs agreed about, over all pairs.</returns>
    /// <remarks>
    /// This is <see cref="AdjustedRand"/> before the correction for chance, and the gap between
    /// the two is the correction made legible: on <c>[0,0,0,1,1,1]</c> against
    /// <c>[0,0,1,2,2,2]</c> this scores <c>0.867</c> where <see cref="AdjustedRand"/> scores
    /// <c>0.706</c>. Because it is uncorrected, two independent labellings score well above zero
    /// here, which is what makes it the wrong choice for comparing across cluster counts.
    /// </remarks>
    /// <exception cref="ArgumentException">The two labellings disagree in length.</exception>
    public static double Score(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
    {
        PairConfusionMatrix pairs = PairConfusionMatrix.Compute(labelsTrue, labelsPred);

        long agreed = pairs.DifferentInBoth + pairs.SameInBoth;
        long total = agreed + pairs.SameInPredictedOnly + pairs.SameInTrueOnly;

        // scikit-learn answers 1.0 for both, rather than dividing: a labelling that
        // disagrees nowhere agrees everywhere, and no pairs at all is not a failure.
        return agreed == total || total == 0 ? 1.0 : agreed / (double)total;
    }
}
