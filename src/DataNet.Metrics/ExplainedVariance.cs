using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// The share of the truth's variance the prediction accounts for — the
/// equivalent of <c>sklearn.metrics.explained_variance_score</c>.
/// </summary>
/// <remarks>
/// <para>
/// One term separates this from <see cref="R2"/>: the residuals are centred on
/// their own mean before being squared, so a prediction that is wrong by the
/// same constant everywhere still explains all of the variance and scores 1,
/// where <see cref="R2"/> pays for the bias.
/// </para>
/// <para>
/// That term is also why this metric takes no <see cref="ZeroDivision"/>, and
/// the asymmetry is measured rather than assumed:
/// <c>explained_variance_score([3], [5])</c> is <c>1.0</c>, because a single
/// residual has zero variance and the metric is genuinely 1 by its own
/// definition. There is no undefined case here to route — the single-sample
/// call falls into the zero-denominator branch and <c>forceFinite</c> answers
/// it, exactly as scikit-learn does.
/// </para>
/// </remarks>
public static class ExplainedVariance
{
    /// <summary>
    /// One number for the whole prediction —
    /// <c>explained_variance_score(y_true, y_pred, sample_weight=…, multioutput=…, force_finite=…)</c>.
    /// </summary>
    /// <param name="yTrue">The true values, row-major when there is more than one output.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <param name="outputCount">How many outputs each row holds. One, the default, is the ordinary case.</param>
    /// <param name="sampleWeight">A weight per sample — per <em>row</em>, not per value. Omit to weight every sample by 1.</param>
    /// <param name="outputWeights">A weight per output (<c>multioutput=[…]</c>). Omit for <c>multioutput="uniform_average"</c>.</param>
    /// <param name="forceFinite">
    /// scikit-learn's <c>force_finite</c>, which answers a truth of zero
    /// variance: 1 when the residuals had no variance either and 0 otherwise.
    /// Pass <see langword="false"/> for the unclamped <c>nan</c> and <c>-inf</c>.
    /// </param>
    /// <exception cref="ArgumentException">A length disagrees with the shape, the input is empty, or it holds a non-finite value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputCount"/> is below one.</exception>
    public static double Score(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int outputCount = 1,
        ReadOnlySpan<double> sampleWeight = default,
        ReadOnlySpan<double> outputWeights = default,
        bool forceFinite = true)
    {
        int samples = Outputs.Validate(yTrue, yPred, outputCount, sampleWeight, outputWeights);
        (double[] scores, _) = Compute(yTrue, yPred, outputCount, sampleWeight, samples, forceFinite);
        return Outputs.Reduce(scores, outputWeights);
    }

    /// <summary>
    /// One number per output — <c>multioutput="raw_values"</c>.
    /// </summary>
    /// <param name="yTrue">The true values, row-major.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <param name="outputCount">How many outputs each row holds.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <param name="forceFinite">scikit-learn's <c>force_finite</c>. See <see cref="Score"/>.</param>
    /// <returns>A fresh array of <paramref name="outputCount"/> entries, in column order.</returns>
    /// <exception cref="ArgumentException">A length disagrees with the shape, the input is empty, or it holds a non-finite value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputCount"/> is below one.</exception>
    public static double[] PerOutput(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int outputCount = 1,
        ReadOnlySpan<double> sampleWeight = default,
        bool forceFinite = true)
    {
        int samples = Outputs.Validate(yTrue, yPred, outputCount, sampleWeight, default);
        return Compute(yTrue, yPred, outputCount, sampleWeight, samples, forceFinite).Scores;
    }

    /// <summary>
    /// One number, each output counted in proportion to the variance of its own
    /// truth — <c>multioutput="variance_weighted"</c>.
    /// </summary>
    /// <param name="yTrue">The true values, row-major.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <param name="outputCount">How many outputs each row holds.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <param name="forceFinite">scikit-learn's <c>force_finite</c>. See <see cref="Score"/>.</param>
    /// <exception cref="ArgumentException">A length disagrees with the shape, the input is empty, or it holds a non-finite value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputCount"/> is below one.</exception>
    /// <remarks>
    /// A method rather than a member of an averaging enum, because the weights
    /// are the per-output denominators of this very computation: they come out
    /// of the same pass that produced the scores and cannot be recovered from
    /// the scores alone.
    /// </remarks>
    public static double VarianceWeighted(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int outputCount,
        ReadOnlySpan<double> sampleWeight = default,
        bool forceFinite = true)
    {
        int samples = Outputs.Validate(yTrue, yPred, outputCount, sampleWeight, default);
        (double[] scores, double[] denominators) =
            Compute(yTrue, yPred, outputCount, sampleWeight, samples, forceFinite);
        return Outputs.ReduceByVariance(scores, denominators);
    }

    // Shaped like R2.Compute, and returning the denominators for the same
    // reason: VarianceWeighted weights each output by the variance of its own
    // truth, which only this pass knows. The differences from R2 are the mean
    // residual accumulated in the first pass and subtracted in the second, and
    // a Resolve with no sample-count branch.
    private static (double[] Scores, double[] Denominators) Compute(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int outputCount,
        ReadOnlySpan<double> sampleWeight,
        int samples,
        bool forceFinite)
    {
        double[] scores = new double[outputCount];
        double[] denominators = new double[outputCount];
        double[] numerators = new double[outputCount];
        double[] means = new double[outputCount];
        double[] meanResiduals = new double[outputCount];
        bool weighted = !sampleWeight.IsEmpty;
        double totalWeight = 0.0;

        for (int row = 0; row < samples; row++)
        {
            double weight = weighted ? sampleWeight[row] : 1.0;
            totalWeight += weight;
            int offset = row * outputCount;
            for (int col = 0; col < outputCount; col++)
            {
                means[col] += weight * yTrue[offset + col];
                meanResiduals[col] += weight * (yTrue[offset + col] - yPred[offset + col]);
            }
        }
        for (int col = 0; col < outputCount; col++)
        {
            means[col] /= totalWeight;
            meanResiduals[col] /= totalWeight;
        }

        for (int row = 0; row < samples; row++)
        {
            double weight = weighted ? sampleWeight[row] : 1.0;
            int offset = row * outputCount;
            for (int col = 0; col < outputCount; col++)
            {
                // Explained variance subtracts the mean residual before
                // squaring; R² does not. That single term is the whole
                // difference between the two metrics, and it is why a biased
                // prediction scores lower on R².
                double residual = yTrue[offset + col] - yPred[offset + col] - meanResiduals[col];
                double centred = yTrue[offset + col] - means[col];
                numerators[col] += weight * residual * residual;
                denominators[col] += weight * centred * centred;
            }
        }

        for (int col = 0; col < outputCount; col++)
        {
            scores[col] = Resolve(numerators[col], denominators[col], forceFinite);
        }
        return (scores, denominators);
    }

    /// <summary>
    /// The one undefined case: a truth with no variance to explain.
    /// </summary>
    /// <remarks>
    /// There is no sample-count branch here, and its absence is measured rather
    /// than assumed — <c>explained_variance_score([3], [5])</c> is <c>1.0</c>,
    /// because the lone residual has no variance either. A single sample
    /// therefore lands in this branch like any other zero denominator, and
    /// <paramref name="forceFinite"/> answers it.
    /// </remarks>
    private static double Resolve(double numerator, double denominator, bool forceFinite)
    {
        // S1244: whether the variance collapsed at all, not whether two computed
        // quantities are close. scikit-learn tests the same quantity against
        // exact zero, and a tolerance would reroute a legitimately tiny variance.
#pragma warning disable S1244
        if (denominator != 0.0)
        {
#pragma warning restore S1244
            return 1.0 - (numerator / denominator);
        }

        // S1244: same question one line down — whether the residuals varied at
        // all, which is what separates scikit-learn's 1 from its 0 (and its nan
        // from its -inf). A tolerance would call a small-but-real spread flat.
#pragma warning disable S1244
        bool explained = numerator == 0.0;
#pragma warning restore S1244
        if (forceFinite)
        {
            return explained ? 1.0 : 0.0;
        }
        return explained ? double.NaN : double.NegativeInfinity;
    }
}
