using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// How much one labelling tells you about the other, unnormalised and in nats —
/// the equivalent of <c>sklearn.metrics.mutual_info_score</c>.
/// </summary>
public static class MutualInformation
{
    /// <summary>Scores the information two labellings share, in nats — <c>sklearn.metrics.mutual_info_score(labels_true, labels_pred)</c>.</summary>
    /// <param name="labelsTrue">The reference labelling.</param>
    /// <param name="labelsPred">The labelling to score, same length as <paramref name="labelsTrue"/>.</param>
    /// <returns><c>double</c> in nats, never negative and <b>not bounded above</b>.</returns>
    /// <remarks>
    /// The raw form of <see cref="NormalizedMutualInformation"/>, in <b>nats</b> rather than bits
    /// (<c>0.693</c> is <c>ln 2</c>, one bit) and <b>not bounded above</b> — comparable only
    /// between labellings of the same data at the same sizes. Across datasets, reach for
    /// <see cref="NormalizedMutualInformation"/>, which scales into <c>[0, 1]</c>, or
    /// <see cref="AdjustedMutualInformation"/>, which also corrects for chance.
    /// </remarks>
    /// <exception cref="ArgumentException">The two labellings disagree in length.</exception>
    public static double Score(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
    {
        Contingency.Validate(labelsTrue, labelsPred);
        return Contingency.Build(labelsTrue, labelsPred).MutualInformation();
    }
}
