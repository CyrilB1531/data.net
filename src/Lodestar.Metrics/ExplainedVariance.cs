#if NET5_0_OR_GREATER
using System.Numerics;
#endif
using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// The share of the truth's variance the prediction accounts for — the
/// equivalent of <c>sklearn.metrics.explained_variance_score</c>.
/// </summary>
/// <remarks>
/// One term separates this from <see cref="R2"/>: residuals are centred on
/// their own mean before squaring, so a uniform bias still scores 1 here,
/// where <see cref="R2"/> pays for it. That is also why this metric takes no
/// <see cref="ZeroDivision"/> — see docs/decisions/0026.
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

    // Shaped like R2.Compute, plus a mean residual accumulated in the first
    // pass and subtracted in the second — see AccumulateUnweighted below.
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
        CompensatedSum[] numerators = new CompensatedSum[outputCount];
        CompensatedSum[] centredSquares = new CompensatedSum[outputCount];

        if (sampleWeight.IsEmpty)
        {
            AccumulateUnweighted(yTrue, yPred, samples, numerators, centredSquares);
        }
        else
        {
            AccumulateWeighted(yTrue, yPred, sampleWeight, samples, numerators, centredSquares);
        }

        for (int col = 0; col < outputCount; col++)
        {
            denominators[col] = centredSquares[col].Value;
            scores[col] = Resolve(numerators[col].Value, denominators[col], forceFinite);
        }
        return (scores, denominators);
    }

    // meanSums/meanResidualSums stay local — the same simplification R2's
    // split applies, dropping the four-array struct the old signature needed.
    private static void AccumulateUnweighted(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int samples,
        CompensatedSum[] numerators,
        CompensatedSum[] centredSquares)
    {
        int outputCount = numerators.Length;

        // Vectorizes only for a single contiguous output; falls through to the
        // scalar loop below otherwise. See docs/decisions/0027.
#if NET5_0_OR_GREATER
        if (outputCount == 1 && Vector.IsHardwareAccelerated)
        {
            AccumulateUnweightedVectorized(yTrue, yPred, samples, numerators, centredSquares);
            return;
        }
#endif

        CompensatedSum[] meanSums = new CompensatedSum[outputCount];
        CompensatedSum[] meanResidualSums = new CompensatedSum[outputCount];
        for (int row = 0; row < samples; row++)
        {
            int offset = row * outputCount;
            for (int col = 0; col < outputCount; col++)
            {
                meanSums[col].Add(yTrue[offset + col]);
                meanResidualSums[col].Add(yTrue[offset + col] - yPred[offset + col]);
            }
        }

        // No total to accumulate: the sum of n ones is exactly n below 2^53,
        // so both means divide by samples directly.
        double[] means = new double[outputCount];
        double[] meanResiduals = new double[outputCount];
        for (int col = 0; col < outputCount; col++)
        {
            means[col] = meanSums[col].Value / samples;
            meanResiduals[col] = meanResidualSums[col].Value / samples;
        }

        for (int row = 0; row < samples; row++)
        {
            int offset = row * outputCount;
            for (int col = 0; col < outputCount; col++)
            {
                double residual = yTrue[offset + col] - yPred[offset + col] - meanResiduals[col];
                double centred = yTrue[offset + col] - means[col];
                numerators[col].Add(residual * residual);
                centredSquares[col].Add(centred * centred);
            }
        }
    }

#if NET5_0_OR_GREATER
    // Not guaranteed bit-identical with the scalar loop above — see
    // VectorCompensatedSum's remarks for why.
    private static void AccumulateUnweightedVectorized(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int samples,
        CompensatedSum[] numerators,
        CompensatedSum[] centredSquares)
    {
        int width = Vector<double>.Count;
        VectorCompensatedSum meanAcc = default;
        VectorCompensatedSum meanResidualAcc = default;
        int i = 0;
        for (; i <= samples - width; i += width)
        {
            var truth = new Vector<double>(yTrue.Slice(i, width));
            var prediction = new Vector<double>(yPred.Slice(i, width));
            meanAcc.Add(truth);
            meanResidualAcc.Add(truth - prediction);
        }

        CompensatedSum meanSum = meanAcc.Reduce();
        CompensatedSum meanResidualSum = meanResidualAcc.Reduce();
        for (; i < samples; i++)
        {
            meanSum.Add(yTrue[i]);
            meanResidualSum.Add(yTrue[i] - yPred[i]);
        }

        double mean = meanSum.Value / samples;
        double meanResidual = meanResidualSum.Value / samples;
        var meanVec = new Vector<double>(mean);
        var meanResidualVec = new Vector<double>(meanResidual);

        VectorCompensatedSum numeratorAcc = default;
        VectorCompensatedSum centredSquareAcc = default;
        i = 0;
        for (; i <= samples - width; i += width)
        {
            var truth = new Vector<double>(yTrue.Slice(i, width));
            var prediction = new Vector<double>(yPred.Slice(i, width));
            Vector<double> residual = truth - prediction - meanResidualVec;
            Vector<double> centred = truth - meanVec;
            numeratorAcc.Add(residual * residual);
            centredSquareAcc.Add(centred * centred);
        }

        CompensatedSum numerator = numeratorAcc.Reduce();
        CompensatedSum centredSquare = centredSquareAcc.Reduce();
        for (; i < samples; i++)
        {
            double residual = yTrue[i] - yPred[i] - meanResidual;
            double centred = yTrue[i] - mean;
            numerator.Add(residual * residual);
            centredSquare.Add(centred * centred);
        }
        numerators[0] = numerator;
        centredSquares[0] = centredSquare;
    }
#endif

    private static void AccumulateWeighted(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        ReadOnlySpan<double> sampleWeight,
        int samples,
        CompensatedSum[] numerators,
        CompensatedSum[] centredSquares)
    {
        int outputCount = numerators.Length;
        CompensatedSum[] meanSums = new CompensatedSum[outputCount];
        CompensatedSum[] meanResidualSums = new CompensatedSum[outputCount];
        CompensatedSum totalWeight = default;
        for (int row = 0; row < samples; row++)
        {
            double weight = sampleWeight[row];
            totalWeight.Add(weight);
            int offset = row * outputCount;
            for (int col = 0; col < outputCount; col++)
            {
                meanSums[col].Add(weight * yTrue[offset + col]);
                meanResidualSums[col].Add(weight * (yTrue[offset + col] - yPred[offset + col]));
            }
        }

        double total = totalWeight.Value;
        double[] means = new double[outputCount];
        double[] meanResiduals = new double[outputCount];
        for (int col = 0; col < outputCount; col++)
        {
            means[col] = meanSums[col].Value / total;
            meanResiduals[col] = meanResidualSums[col].Value / total;
        }

        for (int row = 0; row < samples; row++)
        {
            double weight = sampleWeight[row];
            int offset = row * outputCount;
            for (int col = 0; col < outputCount; col++)
            {
                double residual = yTrue[offset + col] - yPred[offset + col] - meanResiduals[col];
                double centred = yTrue[offset + col] - means[col];
                numerators[col].Add(weight * residual * residual);
                centredSquares[col].Add(weight * centred * centred);
            }
        }
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
