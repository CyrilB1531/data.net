using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// Agreement between two raters corrected for chance — the equivalent of
/// <c>sklearn.metrics.cohen_kappa_score</c>.
/// </summary>
public static class CohenKappa
{
    /// <summary>Cohen's kappa read off an existing matrix (<c>cohen_kappa_score</c>).</summary>
    /// <param name="cm">The matrix to read.</param>
    /// <param name="weighting">How far apart two different classes count as being. Omit to weight every disagreement the same.</param>
    /// <param name="zeroDivision">What to return when the expected agreement is undefined.</param>
    /// <remarks>
    /// <para>
    /// Computed from the matrix's cells <c>C</c>, row sums <c>s1</c>, column
    /// sums <c>s0</c> and total <c>n</c>: an expected cell is
    /// <c>s0[row] * s1[col] / n</c> — scikit-learn's <c>outer(s0, s1) / n</c>,
    /// the transpose of the intuitive <c>outer(s1, s0)</c>. Every
    /// <see cref="KappaWeighting"/> is symmetric in <c>row</c> and <c>col</c>, so
    /// the sum over the matrix is the same either way; written scikit-learn's way
    /// on purpose, so nobody "corrects" the orientation later and changes nothing
    /// but the comment.
    /// </para>
    /// <para>
    /// <see cref="KappaWeighting.Linear"/> and <see cref="KappaWeighting.Quadratic"/>
    /// measure a distance between class <em>positions</em> in <see cref="ConfusionMatrix.Labels"/>,
    /// not between the class values themselves — so the weighted result depends
    /// on that order. A full reversal of the label order preserves every
    /// position distance and gives back the same kappa; any other permutation
    /// does not. <see cref="KappaWeighting.None"/> only asks whether two
    /// positions are equal, so it does not depend on the order at all.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="cm"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="weighting"/> is not one of the three defined values.</exception>
    /// <exception cref="UndefinedMetricException"><paramref name="zeroDivision"/> is <see cref="ZeroDivision.Throw"/> and the expected agreement is undefined.</exception>
    public static double Score(
        ConfusionMatrix cm, KappaWeighting weighting = KappaWeighting.None, ZeroDivision zeroDivision = ZeroDivision.NaN)
    {
        Guard.NotNull(cm);

        int k = cm.Size;
        int stride = cm.Stride;
        ReadOnlySpan<double> cells = cm.Cells;
        double[] rowSums = new double[k];
        double[] colSums = new double[k];
        MatrixSums.Compute(cm, rowSums, colSums, out _, out double total);

        double observed = 0.0;
        double expected = 0.0;
        for (int row = 0; row < k; row++)
        {
            for (int col = 0; col < k; col++)
            {
                // Weight throws on the first out-of-range cell rather than after
                // a full pass over the matrix — which only matters when k > 0,
                // and Inputs.Validate (run inside ConfusionMatrix.Compute) already
                // refuses an empty input before a matrix with k == 0 could exist.
                double weight = Weight(weighting, row, col);

                // SonarLint S1244 warns against comparing floating point for
                // exact equality, which is right for arithmetic and wrong
                // here: Weight never produces anything but a small integer
                // value cast to double — 0, 1, abs(row - col) or its square —
                // so this is a discrete "is this cell excluded" test, not a
                // tolerance question, and every one of those integers is
                // exactly representable.
#pragma warning disable S1244
                if (weight == 0.0)
#pragma warning restore S1244
                {
                    continue;
                }

                observed += weight * cells[(row * stride) + col];

                // scikit-learn's expected matrix is outer(columnSums, rowSums) / n,
                // which is the transpose of the intuitive order. Every weighting
                // above is symmetric, so the two sums agree — written this way so
                // it matches the reference rather than to be corrected later.
                expected += weight * (colSums[row] * rowSums[col] / total);
            }
        }

        // S1244: whether the expected disagreement collapsed at all, not whether
        // two computed quantities are close. It collapses exactly when one input
        // holds a single label, which is the case scikit-learn calls undefined.
#pragma warning disable S1244
        if (expected == 0.0)
#pragma warning restore S1244
        {
            return Prf.Undefined(zeroDivision, "Cohen's kappa");
        }

        return 1.0 - (observed / expected);
    }

    /// <summary>Cohen's kappa straight from the labels, counting the matrix on the way.</summary>
    /// <param name="yTrue">The true labels.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="weighting">How far apart two different classes count as being. Omit to weight every disagreement the same.</param>
    /// <param name="zeroDivision">What to return when the expected agreement is undefined.</param>
    /// <param name="labels">The label set and its order. Omit for the sorted union of both inputs.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <remarks>
    /// With <paramref name="weighting"/> other than <see cref="KappaWeighting.None"/>,
    /// the result depends on the order of <paramref name="labels"/> — see the
    /// matrix overload's remarks. Omitting <paramref name="labels"/> uses the
    /// sorted union of both inputs, the same order scikit-learn's own default
    /// resolves to.
    /// </remarks>
    /// <exception cref="ArgumentException">The inputs disagree in length or are empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="weighting"/> is not one of the three defined values.</exception>
    /// <exception cref="UndefinedMetricException"><paramref name="zeroDivision"/> is <see cref="ZeroDivision.Throw"/> and the expected agreement is undefined.</exception>
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        KappaWeighting weighting = KappaWeighting.None,
        ZeroDivision zeroDivision = ZeroDivision.NaN,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default) =>
        Score(ConfusionMatrix.Compute(yTrue, yPred, labels, sampleWeight), weighting, zeroDivision);

    private static double Weight(KappaWeighting weighting, int row, int col) => weighting switch
    {
        KappaWeighting.None => row == col ? 0.0 : 1.0,
        KappaWeighting.Linear => Math.Abs(row - col),
        KappaWeighting.Quadratic => (row - col) * (double)(row - col),
        _ => throw new ArgumentOutOfRangeException(
            nameof(weighting), weighting, "Not one of the three kappa weightings."),
    };
}
