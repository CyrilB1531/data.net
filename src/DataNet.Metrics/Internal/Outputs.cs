namespace DataNet.Metrics.Internal;

/// <summary>
/// The two things every regression metric in this package shares: agreeing that
/// a flat span really is <c>n × outputCount</c>, and reducing a per-output
/// array to a scalar.
/// </summary>
/// <remarks>
/// Nothing else is shared. Each metric owns its own pass over the data, because
/// what they compute per output has almost nothing in common — a squared mean, a
/// sorted median, a clamped ratio and a quantile loss are four different loops
/// wearing the same signature.
/// </remarks>
internal static class Outputs
{
    /// <summary>Checks the shape and returns the sample count.</summary>
    /// <param name="yTrue">The true values, row-major.</param>
    /// <param name="yPred">The predicted values, row-major.</param>
    /// <param name="outputCount">How many outputs each row holds.</param>
    /// <param name="sampleWeight">A weight per sample, or empty.</param>
    /// <param name="outputWeights">A weight per output, or empty for a plain mean.</param>
    /// <returns>The number of samples, <c>yTrue.Length / outputCount</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="outputCount"/> is below one.</exception>
    /// <exception cref="ArgumentException">A length disagrees with the shape.</exception>
    public static int Validate(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int outputCount,
        ReadOnlySpan<double> sampleWeight,
        ReadOnlySpan<double> outputWeights)
    {
        Inputs.Validate(yTrue, yPred, sampleWeight);
        Guard.NotLessThan(outputCount, 1);

        if (yTrue.Length % outputCount != 0)
        {
            throw new ArgumentException(
                $"yTrue holds {yTrue.Length} values, which is not a whole number of rows of {outputCount} outputs.",
                nameof(outputCount));
        }

        int samples = yTrue.Length / outputCount;
        if (!sampleWeight.IsEmpty && sampleWeight.Length != samples)
        {
            throw new ArgumentException(
                $"sampleWeight has {sampleWeight.Length} entries but there are {samples} samples.",
                nameof(sampleWeight));
        }
        if (!outputWeights.IsEmpty && outputWeights.Length != outputCount)
        {
            throw new ArgumentException(
                $"outputWeights has {outputWeights.Length} entries but there are {outputCount} outputs.",
                nameof(outputWeights));
        }

        return samples;
    }

    /// <summary>
    /// <c>multioutput="uniform_average"</c> when <paramref name="outputWeights"/>
    /// is empty, and the weighted average scikit-learn computes for an array
    /// otherwise.
    /// </summary>
    /// <param name="perOutput">One value per output.</param>
    /// <param name="outputWeights">One weight per output, or empty.</param>
    public static double Reduce(double[] perOutput, ReadOnlySpan<double> outputWeights)
    {
        if (outputWeights.IsEmpty)
        {
            double sum = 0.0;
            foreach (double value in perOutput)
            {
                sum += value;
            }
            return sum / perOutput.Length;
        }

        double weighted = 0.0;
        double total = 0.0;
        for (int i = 0; i < perOutput.Length; i++)
        {
            weighted += perOutput[i] * outputWeights[i];
            total += outputWeights[i];
        }
        return weighted / total;
    }

    /// <summary>
    /// <c>multioutput="variance_weighted"</c>: each output counted in proportion
    /// to the variance of its own truth.
    /// </summary>
    /// <param name="perOutput">One score per output.</param>
    /// <param name="variances">The weighted variance of the truth, per output.</param>
    /// <remarks>
    /// When every variance is zero there is nothing to weight by, and
    /// scikit-learn falls back to the plain mean rather than dividing by zero.
    /// </remarks>
    public static double ReduceByVariance(double[] perOutput, double[] variances)
    {
        double total = 0.0;
        foreach (double variance in variances)
        {
            total += variance;
        }

        // S1244: the question is whether any variance accumulated at all, not
        // whether two computed quantities are close. A tolerance would reroute a
        // legitimately tiny variance into the fallback and change the value.
#pragma warning disable S1244
        if (total == 0.0)
#pragma warning restore S1244
        {
            return Reduce(perOutput, default);
        }

        double weighted = 0.0;
        for (int i = 0; i < perOutput.Length; i++)
        {
            weighted += perOutput[i] * variances[i];
        }
        return weighted / total;
    }
}
