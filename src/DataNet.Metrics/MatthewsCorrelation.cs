using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// The correlation between predicted and true labels — the equivalent of
/// <c>sklearn.metrics.matthews_corrcoef</c>.
/// </summary>
public static class MatthewsCorrelation
{
    /// <summary>Matthews correlation read off an existing matrix (<c>matthews_corrcoef</c>).</summary>
    /// <param name="cm">The matrix to read.</param>
    /// <param name="zeroDivision">What to return when the correlation is undefined.</param>
    /// <remarks>
    /// Computed from the matrix's row sums <c>t</c>, column sums <c>p</c>, total
    /// <c>n</c> and trace <c>c</c>: <c>(c*n - dot(t, p)) / sqrt((n² - dot(t, t)) *
    /// (n² - dot(p, p)))</c>. The denominator collapses whenever either side of
    /// the matrix holds a single label — scikit-learn hard-codes <c>0.0</c> there
    /// and warns; this reads <paramref name="zeroDivision"/> instead.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="cm"/> is null.</exception>
    /// <exception cref="UndefinedMetricException"><paramref name="zeroDivision"/> is <see cref="ZeroDivision.Throw"/> and the correlation is undefined.</exception>
    public static double Score(ConfusionMatrix cm, ZeroDivision zeroDivision = ZeroDivision.Zero)
    {
        Guard.NotNull(cm);

        int k = cm.Size;
        double[] rowSums = new double[k];
        double[] colSums = new double[k];
        MatrixSums.Compute(cm, rowSums, colSums, out double trace, out double total);

        double crossed = 0.0;
        double trueSquares = 0.0;
        double predSquares = 0.0;
        for (int i = 0; i < k; i++)
        {
            crossed += rowSums[i] * colSums[i];
            trueSquares += rowSums[i] * rowSums[i];
            predSquares += colSums[i] * colSums[i];
        }

        double numerator = (trace * total) - crossed;
        double denominator = Math.Sqrt(((total * total) - trueSquares) * ((total * total) - predSquares));

        // S1244: this asks whether the denominator collapsed at all, not whether
        // two computed quantities are close. A tolerance would refuse legitimate
        // inputs whose weights are merely small, and scikit-learn tests the same
        // quantity against exact zero.
#pragma warning disable S1244
        if (denominator == 0.0)
#pragma warning restore S1244
        {
            return Prf.Undefined(zeroDivision, "Matthews correlation");
        }

        return numerator / denominator;
    }

    /// <summary>Matthews correlation straight from the labels, counting the matrix on the way.</summary>
    /// <param name="yTrue">The true labels.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="zeroDivision">What to return when the correlation is undefined.</param>
    /// <param name="labels">The label set and its order. Omit for the sorted union of both inputs.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <exception cref="ArgumentException">The inputs disagree in length or are empty.</exception>
    /// <exception cref="UndefinedMetricException"><paramref name="zeroDivision"/> is <see cref="ZeroDivision.Throw"/> and the correlation is undefined.</exception>
    public static double Score(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        ZeroDivision zeroDivision = ZeroDivision.Zero,
        ReadOnlySpan<int> labels = default,
        ReadOnlySpan<double> sampleWeight = default) =>
        Score(ConfusionMatrix.Compute(yTrue, yPred, labels, sampleWeight), zeroDivision);
}
