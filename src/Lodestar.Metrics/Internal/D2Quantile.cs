namespace Lodestar.Metrics.Internal;

/// <summary>
/// The pinball D², which <c>d2_pinball_score</c> and <c>d2_absolute_error_score</c>
/// are the general and the half-quantile forms of.
/// </summary>
/// <remarks>
/// One minus the model's pinball loss over the loss of predicting a constant — the
/// weighted quantile of the truth at the same alpha. Unlike the Tweedie D², a
/// constant truth answers 0 rather than raising: the reference masks that
/// denominator here and does not there.
/// </remarks>
internal static class D2Quantile
{
    /// <summary>One score per output column.</summary>
    public static double[] PerOutput(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        double alpha,
        int outputCount,
        ReadOnlySpan<double> sampleWeight,
        ZeroDivision zeroDivision)
    {
        int samples = Outputs.Validate(yTrue, yPred, outputCount, sampleWeight, default);
        var scores = new double[outputCount];

        if (samples < 2)
        {
            for (int col = 0; col < outputCount; col++)
            {
                scores[col] = Prf.Undefined(zeroDivision, "D² pinball");
            }

            return scores;
        }

        double[] numerators = Outputs.WeightedMean(
            yTrue, yPred, outputCount, sampleWeight, samples, new Pinball(alpha));

        for (int col = 0; col < outputCount; col++)
        {
            scores[col] = Resolve(numerators[col], Denominator(yTrue, alpha, outputCount, sampleWeight, samples, col));
        }

        return scores;
    }

    /// <summary>The loss of the best constant prediction for one column.</summary>
    private static double Denominator(
        ReadOnlySpan<double> yTrue,
        double alpha,
        int outputCount,
        ReadOnlySpan<double> sampleWeight,
        int samples,
        int col)
    {
        double[] column = new double[samples];
        for (int row = 0; row < samples; row++)
        {
            column[row] = yTrue[(row * outputCount) + col];
        }

        // Quantile sorts in place and reorders the weights alongside, so both
        // arrays are copies the caller never sees again.
        double[] weights = sampleWeight.IsEmpty ? [] : sampleWeight.ToArray();
        double constant = WeightedPercentile.Quantile(column, weights, alpha);

        var kernel = new Pinball(alpha);
        CompensatedSum sum = default;
        double total = 0.0;
        for (int row = 0; row < samples; row++)
        {
            double weight = sampleWeight.IsEmpty ? 1.0 : sampleWeight[row];
            sum.Add(weight * kernel.Apply(yTrue[(row * outputCount) + col], constant));
            total += weight;
        }

        return sum.Value / total;
    }

    private static double Resolve(double numerator, double denominator)
    {
        // S1244: whether the constant model already scored perfectly, which is the
        // case scikit-learn masks and answers 0 for rather than dividing.
#pragma warning disable S1244
        return denominator == 0.0 ? 0.0 : 1.0 - (numerator / denominator);
#pragma warning restore S1244
    }

    /// <summary>The pinball loss at one quantile, the same kernel <c>PinballLoss</c> averages.</summary>
    private readonly struct Pinball(double alpha) : IResidualKernel
    {
        public double Apply(double truth, double prediction)
        {
            double residual = truth - prediction;
            double under = alpha * residual;
            double over = (alpha - 1.0) * residual;
            return under > over ? under : over;
        }
    }
}
