#if NET5_0_OR_GREATER
using System.Numerics;
#endif
using DataNet.Metrics.Internal;

namespace DataNet.Metrics;

/// <summary>
/// The coefficient of determination — the equivalent of
/// <c>sklearn.metrics.r2_score</c>.
/// </summary>
/// <remarks>
/// <c>forceFinite</c> and <see cref="ZeroDivision"/> answer two different
/// undefined cases and must not be merged into one. See
/// docs/decisions/0026.
/// </remarks>
public static class R2
{
    /// <summary>
    /// One number for the whole prediction —
    /// <c>r2_score(y_true, y_pred, sample_weight=…, multioutput=…, force_finite=…)</c>.
    /// </summary>
    /// <param name="yTrue">The true values, row-major when there is more than one output.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <param name="outputCount">How many outputs each row holds. One, the default, is the ordinary case.</param>
    /// <param name="sampleWeight">A weight per sample — per <em>row</em>, not per value. Omit to weight every sample by 1.</param>
    /// <param name="outputWeights">A weight per output (<c>multioutput=[…]</c>). Omit for <c>multioutput="uniform_average"</c>.</param>
    /// <param name="forceFinite">
    /// scikit-learn's <c>force_finite</c>, which answers a truth of zero variance
    /// over two or more samples and nothing else. Pass <see langword="false"/>
    /// for the unclamped <c>nan</c> and <c>-inf</c>.
    /// </param>
    /// <param name="zeroDivision">
    /// What to answer when there are fewer than two samples, which is the only
    /// case scikit-learn leaves undefined regardless of
    /// <paramref name="forceFinite"/>. The default reproduces its <c>nan</c>.
    /// </param>
    /// <exception cref="ArgumentException">A length disagrees with the shape, the input is empty, or it holds a non-finite value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputCount"/> is below one.</exception>
    /// <exception cref="UndefinedMetricException">
    /// There are fewer than two samples and <paramref name="zeroDivision"/> is
    /// <see cref="ZeroDivision.Throw"/>.
    /// </exception>
    public static double Score(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int outputCount = 1,
        ReadOnlySpan<double> sampleWeight = default,
        ReadOnlySpan<double> outputWeights = default,
        bool forceFinite = true,
        ZeroDivision zeroDivision = ZeroDivision.NaN)
    {
        int samples = Outputs.Validate(yTrue, yPred, outputCount, sampleWeight, outputWeights);
        (double[] scores, _) =
            Compute(yTrue, yPred, outputCount, sampleWeight, samples, forceFinite, zeroDivision);
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
    /// <param name="zeroDivision">The answer for fewer than two samples. See <see cref="Score"/>.</param>
    /// <returns>A fresh array of <paramref name="outputCount"/> entries, in column order.</returns>
    /// <exception cref="ArgumentException">A length disagrees with the shape, the input is empty, or it holds a non-finite value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputCount"/> is below one.</exception>
    /// <exception cref="UndefinedMetricException">
    /// There are fewer than two samples and <paramref name="zeroDivision"/> is
    /// <see cref="ZeroDivision.Throw"/>.
    /// </exception>
    public static double[] PerOutput(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int outputCount = 1,
        ReadOnlySpan<double> sampleWeight = default,
        bool forceFinite = true,
        ZeroDivision zeroDivision = ZeroDivision.NaN)
    {
        int samples = Outputs.Validate(yTrue, yPred, outputCount, sampleWeight, default);
        return Compute(yTrue, yPred, outputCount, sampleWeight, samples, forceFinite, zeroDivision).Scores;
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
    /// <param name="zeroDivision">The answer for fewer than two samples. See <see cref="Score"/>.</param>
    /// <exception cref="ArgumentException">A length disagrees with the shape, the input is empty, or it holds a non-finite value.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputCount"/> is below one.</exception>
    /// <exception cref="UndefinedMetricException">
    /// There are fewer than two samples and <paramref name="zeroDivision"/> is
    /// <see cref="ZeroDivision.Throw"/>.
    /// </exception>
    /// <remarks>
    /// A method rather than a member of an averaging enum. See
    /// docs/decisions/0021.
    /// </remarks>
    public static double VarianceWeighted(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int outputCount,
        ReadOnlySpan<double> sampleWeight = default,
        bool forceFinite = true,
        ZeroDivision zeroDivision = ZeroDivision.NaN)
    {
        int samples = Outputs.Validate(yTrue, yPred, outputCount, sampleWeight, default);
        (double[] scores, double[] denominators) =
            Compute(yTrue, yPred, outputCount, sampleWeight, samples, forceFinite, zeroDivision);
        return Outputs.ReduceByVariance(scores, denominators);
    }

    // One pass returns both arrays: VarianceWeighted needs the denominators,
    // which cannot be recovered from the scores alone.
    private static (double[] Scores, double[] Denominators) Compute(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int outputCount,
        ReadOnlySpan<double> sampleWeight,
        int samples,
        bool forceFinite,
        ZeroDivision zeroDivision)
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
            scores[col] = Resolve(numerators[col].Value, denominators[col], samples, forceFinite, zeroDivision);
        }
        return (scores, denominators);
    }

    // meanSums stays local: Compute never sees it, so threading it through as
    // a parameter would only spend S107's budget for nothing.
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
        for (int row = 0; row < samples; row++)
        {
            int offset = row * outputCount;
            for (int col = 0; col < outputCount; col++)
            {
                meanSums[col].Add(yTrue[offset + col]);
            }
        }

        // No total to accumulate: the sum of n ones is exactly n below 2^53,
        // so the mean divides by samples directly.
        double[] means = new double[outputCount];
        for (int col = 0; col < outputCount; col++)
        {
            means[col] = meanSums[col].Value / samples;
        }

        for (int row = 0; row < samples; row++)
        {
            int offset = row * outputCount;
            for (int col = 0; col < outputCount; col++)
            {
                double residual = yTrue[offset + col] - yPred[offset + col];
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
        int i = 0;
        for (; i <= samples - width; i += width)
        {
            meanAcc.Add(new Vector<double>(yTrue.Slice(i, width)));
        }

        CompensatedSum meanSum = meanAcc.Reduce();
        for (; i < samples; i++)
        {
            meanSum.Add(yTrue[i]);
        }

        double mean = meanSum.Value / samples;
        var meanVec = new Vector<double>(mean);

        VectorCompensatedSum numeratorAcc = default;
        VectorCompensatedSum centredSquareAcc = default;
        i = 0;
        for (; i <= samples - width; i += width)
        {
            var truth = new Vector<double>(yTrue.Slice(i, width));
            var prediction = new Vector<double>(yPred.Slice(i, width));
            Vector<double> residual = truth - prediction;
            Vector<double> centred = truth - meanVec;
            numeratorAcc.Add(residual * residual);
            centredSquareAcc.Add(centred * centred);
        }

        CompensatedSum numerator = numeratorAcc.Reduce();
        CompensatedSum centredSquare = centredSquareAcc.Reduce();
        for (; i < samples; i++)
        {
            double residual = yTrue[i] - yPred[i];
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
        CompensatedSum totalWeight = default;
        for (int row = 0; row < samples; row++)
        {
            double weight = sampleWeight[row];
            totalWeight.Add(weight);
            int offset = row * outputCount;
            for (int col = 0; col < outputCount; col++)
            {
                meanSums[col].Add(weight * yTrue[offset + col]);
            }
        }

        double total = totalWeight.Value;
        double[] means = new double[outputCount];
        for (int col = 0; col < outputCount; col++)
        {
            means[col] = meanSums[col].Value / total;
        }

        for (int row = 0; row < samples; row++)
        {
            double weight = sampleWeight[row];
            int offset = row * outputCount;
            for (int col = 0; col < outputCount; col++)
            {
                double residual = yTrue[offset + col] - yPred[offset + col];
                double centred = yTrue[offset + col] - means[col];
                numerators[col].Add(weight * residual * residual);
                centredSquares[col].Add(weight * centred * centred);
            }
        }
    }

    /// <summary>
    /// The two undefined cases, which do not overlap and must not be merged.
    /// </summary>
    /// <remarks>
    /// Fewer than two samples is <c>nan</c> in scikit-learn under either setting
    /// of <c>force_finite</c>, so it is <see cref="ZeroDivision"/>'s case alone.
    /// A denominator of zero over two or more samples is
    /// <paramref name="forceFinite"/>'s alone: 1 when the numerator vanished
    /// too, 0 otherwise, or <c>nan</c> and <c>-inf</c> when the caller asked for
    /// the unclamped values.
    /// </remarks>
    private static double Resolve(
        double numerator, double denominator, int samples, bool forceFinite, ZeroDivision zeroDivision)
    {
        if (samples < 2)
        {
            return Prf.Undefined(zeroDivision, "R²");
        }

        // S1244: whether the variance collapsed at all, not whether two computed
        // quantities are close. scikit-learn tests the same quantity against
        // exact zero, and a tolerance would reroute a legitimately tiny variance.
#pragma warning disable S1244
        if (denominator != 0.0)
        {
#pragma warning restore S1244
            return 1.0 - (numerator / denominator);
        }

        // S1244: same question one line down — whether the residuals vanished
        // exactly, which is what separates scikit-learn's 1 from its 0 (and its
        // nan from its -inf). A tolerance would call a small-but-real error
        // perfect.
#pragma warning disable S1244
        bool perfect = numerator == 0.0;
#pragma warning restore S1244
        if (forceFinite)
        {
            return perfect ? 1.0 : 0.0;
        }
        return perfect ? double.NaN : double.NegativeInfinity;
    }
}
