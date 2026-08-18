using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// How two labellings pair the samples up: for every ordered pair, whether each
/// labelling put the two together — the equivalent of
/// <c>sklearn.metrics.cluster.pair_confusion_matrix</c>.
/// </summary>
/// <remarks>
/// Not a <see cref="ConfusionMatrix"/>, which counts labels. This counts <b>ordered
/// pairs of samples</b>, so the four values sum to <c>n²</c> and reach about
/// <c>5·10⁹</c> at a hundred thousand samples — which is why they are
/// <see cref="long"/>.
/// </remarks>
/// <param name="DifferentInBoth">Pairs both labellings split apart.</param>
/// <param name="SameInPredictedOnly">Pairs only <c>labelsPred</c> put together.</param>
/// <param name="SameInTrueOnly">Pairs only <c>labelsTrue</c> put together.</param>
/// <param name="SameInBoth">Pairs both labellings put together.</param>
public readonly record struct PairConfusionMatrix(
    long DifferentInBoth,
    long SameInPredictedOnly,
    long SameInTrueOnly,
    long SameInBoth)
{
    /// <summary>Counts the pairs two labellings agree and disagree about — <c>sklearn.metrics.cluster.pair_confusion_matrix(labels_true, labels_pred)</c>.</summary>
    /// <param name="labelsTrue">The reference labelling.</param>
    /// <param name="labelsPred">The labelling to score, same length as <paramref name="labelsTrue"/>.</param>
    /// <returns>The four ordered-pair counts, which sum to the square of the sample count.</returns>
    /// <exception cref="ArgumentException">The two labellings disagree in length.</exception>
    public static PairConfusionMatrix Compute(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
    {
        Contingency.Validate(labelsTrue, labelsPred);
        Contingency table = Contingency.Build(labelsTrue, labelsPred);

        long samples = table.Samples;
        long cellSquares = SumOfSquares(table.Cells.Values);
        long trueSquares = SumOfSquares(table.Rows);
        long predSquares = SumOfSquares(table.Columns);

        long sameInBoth = cellSquares - samples;
        long sameInPredictedOnly = predSquares - cellSquares;
        long sameInTrueOnly = trueSquares - cellSquares;
        long differentInBoth = (samples * samples) - sameInPredictedOnly - sameInTrueOnly - cellSquares;

        return new PairConfusionMatrix(differentInBoth, sameInPredictedOnly, sameInTrueOnly, sameInBoth);
    }

    /// <summary>The same four counts as a 2×2 array, in scikit-learn's own order.</summary>
    /// <returns><c>[[DifferentInBoth, SameInPredictedOnly], [SameInTrueOnly, SameInBoth]]</c>.</returns>
    /// <remarks>
    /// For reading ported code side by side, where the indices are what the source
    /// says. The named properties are the ones to prefer when writing new code:
    /// <c>[0,1]</c> and <c>[1,0]</c> are easy to swap and impossible to swap by name.
    /// </remarks>
    // CA1814 (prefer jagged arrays): the shape is rectangular and fixed at 2x2, and
    // the point of this overload is to match numpy's, which is not ragged.
#pragma warning disable CA1814
    public long[,] ToArray() => new[,]
    {
        { DifferentInBoth, SameInPredictedOnly },
        { SameInTrueOnly, SameInBoth },
    };
#pragma warning restore CA1814

    private static long SumOfSquares(IEnumerable<int> counts)
    {
        long total = 0;
        foreach (int count in counts)
        {
            total += (long)count * count;
        }

        return total;
    }
}
