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
    /// position distance and always gives back the same kappa; a permutation that
    /// is not a reversal generally changes it, though a sufficiently symmetric
    /// matrix can happen to be invariant under one.
    /// <see cref="KappaWeighting.None"/> only asks whether two
    /// positions are equal, so it does not depend on the order at all.
    /// </para>
    /// <para>
    /// This overload scores exactly the classes <paramref name="cm"/> holds: cells,
    /// sums and total all come from the <see cref="ConfusionMatrix.Labels"/>-sized
    /// view, so a matrix built with an explicit label subset contributes none of
    /// the samples it dropped. <c>cohen_kappa_score</c> does take a <c>labels</c>
    /// argument, so a reference value exists for such a matrix — and on the
    /// fixture the tests pin, restricting to <c>[1, 2]</c> gives <c>1.0</c> from
    /// both. That is agreement measured on one case, not a general guarantee: the
    /// rule here is "what the matrix holds is what is scored", stated without
    /// reference to what any scikit-learn call over the full label set would say.
    /// </para>
    /// <para>
    /// If that view carries no weight at all — every sample's true or predicted
    /// label fell outside the label subset, or every <c>sampleWeight</c> was zero
    /// — there is nothing to correct for chance and
    /// <paramref name="zeroDivision"/> decides the answer, as it does when the
    /// expected agreement itself collapses.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="cm"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="weighting"/> is not one of the three defined values.</exception>
    /// <exception cref="UndefinedMetricException"><paramref name="zeroDivision"/> is <see cref="ZeroDivision.Throw"/> and the expected agreement is undefined.</exception>
    public static double Score(
        ConfusionMatrix cm, KappaWeighting weighting = KappaWeighting.None, ZeroDivision zeroDivision = ZeroDivision.NaN)
    {
        Guard.NotNull(cm);

        // Validated before the degenerate-total shortcut below, so an out-of-range
        // weighting is refused on every input rather than only on the ones whose
        // matrix carries weight — scikit-learn validates its parameters before it
        // looks at the data. Weight throws on the first value it cannot map and
        // reads no cell, so asking it for (0, 0) is the whole check.
        _ = Weight(weighting, 0, 0);

        int k = cm.Size;
        int stride = cm.Stride;
        ReadOnlySpan<double> cells = cm.Cells;
        double[] rowSums = new double[k];
        double[] colSums = new double[k];
        MatrixSums.Compute(cm, rowSums, colSums, out _, out double total);

        // scikit-learn tests this sum before it builds the expected matrix, and
        // returns replace_undefined_by if it is zero. Without the same guard here
        // every expected cell would be 0.0 / 0.0, expected would be NaN, the
        // expected == 0.0 test below would never fire, and the method would
        // return NaN whatever zeroDivision said — including under Throw. The
        // Size × Size view carries no weight whenever a label subset dropped
        // every sample, or every sampleWeight was zero.
        //
        // S1244: whether the view holds any weight at all, not whether two
        // computed quantities are close.
#pragma warning disable S1244
        if (total == 0.0)
#pragma warning restore S1244
        {
            return Prf.Undefined(zeroDivision, "Cohen's kappa");
        }

        double observed = 0.0;
        double expected = 0.0;
        for (int row = 0; row < k; row++)
        {
            for (int col = 0; col < k; col++)
            {
                // Already validated above, so this cannot throw; called per cell
                // rather than tabulated because the switch inlines and a k × k
                // weight table would allocate.
                double weight = Weight(weighting, row, col);

                observed += weight * cells[(row * stride) + col];

                // scikit-learn's expected matrix is outer(columnSums, rowSums) / n,
                // which is the transpose of the intuitive order. Every weighting
                // above is symmetric, so the two sums agree — written this way so
                // it matches the reference rather than to be corrected later.
                expected += weight * (colSums[row] * rowSums[col] / total);
            }
        }

        // A different undefined case from the total == 0.0 one above, and still
        // needed: with weight in the view, the expected disagreement collapses
        // exactly when one input holds a single label, which is what
        // scikit-learn calls undefined.
        //
        // S1244: whether that quantity collapsed at all, not whether two
        // computed quantities are close.
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
