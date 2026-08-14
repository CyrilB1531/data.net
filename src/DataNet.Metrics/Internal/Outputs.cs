namespace DataNet.Metrics.Internal;

/// <summary>
/// What one metric does to one <c>(truth, prediction)</c> pair, and nothing
/// else — the only part of a weighted mean that differs between five of the
/// eleven regression metrics.
/// </summary>
/// <remarks>
/// Implemented by <see langword="struct"/>s, not a
/// <see cref="Func{T1, T2, TResult}"/>: specialized per kernel and resolved
/// statically, where a delegate would cost an indirect call per element.
/// </remarks>
internal interface IResidualKernel
{
    /// <summary>The metric's own per-pair quantity, before weighting.</summary>
    /// <param name="truth">One true value.</param>
    /// <param name="prediction">The prediction for the same sample and output.</param>
    double Apply(double truth, double prediction);
}

/// <summary>
/// What the regression metrics in this package share: agreeing that a flat span
/// really is <c>n × outputCount</c>, walking it once under a per-pair kernel,
/// and reducing the per-output array to a scalar.
/// </summary>
/// <remarks>
/// Five kernels differ in one expression and share the rest; what is not the
/// same keeps its own code — the median sorts, R² and explained variance need
/// two passes, and <see cref="MaxError"/> takes no weights.
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
        if (!outputWeights.IsEmpty)
        {
            if (outputWeights.Length != outputCount)
            {
                throw new ArgumentException(
                    $"outputWeights has {outputWeights.Length} entries but there are {outputCount} outputs.",
                    nameof(outputWeights));
            }

            RequireNormalizable(outputWeights);
        }

        return samples;
    }

    /// <summary>
    /// Reproduces <c>numpy.average</c>'s refusal of weights it cannot normalize,
    /// with its message — the error scikit-learn surfaces for
    /// <c>multioutput=[0, 0]</c>.
    /// </summary>
    /// <remarks>
    /// The test is the <em>sum</em>, not all-zero, unlike the sample weight's
    /// own check: <c>[1, -1]</c> is refused here though not all zero, while
    /// <c>[-1, -1]</c> scores, since its sum normalizes fine.
    /// </remarks>
    private static void RequireNormalizable(ReadOnlySpan<double> outputWeights)
    {
        double total = 0.0;
        foreach (double weight in outputWeights)
        {
            total += weight;
        }

        // S1244: the question is whether the sum normalizes at all, which is a
        // division by exactly zero and nothing else. A tolerance would refuse a
        // legitimately tiny sum that numpy divides by without complaint.
#pragma warning disable S1244
        if (total == 0.0)
#pragma warning restore S1244
        {
            throw new ArgumentException(
                "Weights sum to zero, can't be normalized.", nameof(outputWeights));
        }
    }

    /// <summary>
    /// The whole of a weighted-mean metric's <c>Score</c>: validate, walk under
    /// <typeparamref name="TKernel"/>, reduce.
    /// </summary>
    /// <typeparam name="TKernel">The per-pair quantity this metric averages.</typeparam>
    /// <param name="yTrue">The true values, row-major.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <param name="outputCount">How many outputs each row holds.</param>
    /// <param name="sampleWeight">A weight per sample, or empty.</param>
    /// <param name="outputWeights">A weight per output, or empty for a plain mean.</param>
    /// <param name="kernel">The kernel instance, for a kernel that carries state such as an <c>alpha</c>.</param>
    public static double Score<TKernel>(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int outputCount,
        ReadOnlySpan<double> sampleWeight,
        ReadOnlySpan<double> outputWeights,
        TKernel kernel = default)
        where TKernel : struct, IResidualKernel
    {
        int samples = Validate(yTrue, yPred, outputCount, sampleWeight, outputWeights);
        return Reduce(WeightedMean(yTrue, yPred, outputCount, sampleWeight, samples, kernel), outputWeights);
    }

    /// <summary>The same walk without the reduction — <c>multioutput="raw_values"</c>.</summary>
    /// <typeparam name="TKernel">The per-pair quantity this metric averages.</typeparam>
    /// <param name="yTrue">The true values, row-major.</param>
    /// <param name="yPred">The predicted values, same length as <paramref name="yTrue"/>.</param>
    /// <param name="outputCount">How many outputs each row holds.</param>
    /// <param name="sampleWeight">A weight per sample, or empty.</param>
    /// <param name="kernel">The kernel instance, for a kernel that carries state.</param>
    public static double[] PerOutput<TKernel>(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int outputCount,
        ReadOnlySpan<double> sampleWeight,
        TKernel kernel = default)
        where TKernel : struct, IResidualKernel
    {
        int samples = Validate(yTrue, yPred, outputCount, sampleWeight, default);
        return WeightedMean(yTrue, yPred, outputCount, sampleWeight, samples, kernel);
    }

    /// <summary>
    /// The weighted mean of <typeparamref name="TKernel"/> over each output
    /// column, on input already validated.
    /// </summary>
    /// <typeparam name="TKernel">The per-pair quantity to average.</typeparam>
    /// <param name="yTrue">The true values, row-major.</param>
    /// <param name="yPred">The predicted values, row-major.</param>
    /// <param name="outputCount">How many outputs each row holds.</param>
    /// <param name="sampleWeight">A weight per sample, or empty for weight 1 each.</param>
    /// <param name="samples">The sample count <see cref="Validate"/> returned.</param>
    /// <param name="kernel">The kernel instance.</param>
    /// <returns>A fresh array of <paramref name="outputCount"/> entries, in column order.</returns>
    /// <remarks>
    /// Separate from <see cref="Score{TKernel}"/> and
    /// <see cref="PerOutput{TKernel}"/> so that a metric with a validation rule
    /// of its own — <see cref="MeanSquaredLogError"/> refuses a target at or
    /// below −1 — can run it between the shared check and the walk, rather than
    /// paying for a second validation pass to get in front of it.
    /// </remarks>
    public static double[] WeightedMean<TKernel>(
        ReadOnlySpan<double> yTrue,
        ReadOnlySpan<double> yPred,
        int outputCount,
        ReadOnlySpan<double> sampleWeight,
        int samples,
        TKernel kernel = default)
        where TKernel : struct, IResidualKernel
    {
        CompensatedSum[] sums = new CompensatedSum[outputCount];
        double total;

        if (sampleWeight.IsEmpty)
        {
            for (int row = 0; row < samples; row++)
            {
                int offset = row * outputCount;
                for (int col = 0; col < outputCount; col++)
                {
                    sums[col].Add(kernel.Apply(yTrue[offset + col], yPred[offset + col]));
                }
            }

            // Exact: n additions of 1.0 land on n for every n below 2^53, and this
            // repository's array-length guard is far below that.
            total = samples;
        }
        else
        {
            CompensatedSum totalWeight = default;
            for (int row = 0; row < samples; row++)
            {
                double weight = sampleWeight[row];
                totalWeight.Add(weight);
                int offset = row * outputCount;
                for (int col = 0; col < outputCount; col++)
                {
                    sums[col].Add(weight * kernel.Apply(yTrue[offset + col], yPred[offset + col]));
                }
            }
            total = totalWeight.Value;
        }

        double[] result = new double[outputCount];
        for (int col = 0; col < outputCount; col++)
        {
            result[col] = sums[col].Value / total;
        }

        return result;
    }

    /// <summary>Roots every entry in place, and hands the same array back.</summary>
    /// <param name="perOutput">One value per output, replaced by its square root.</param>
    /// <remarks>
    /// In place because the caller has just allocated it: the two rooted metrics
    /// each ask their squared counterpart for a fresh array and own it outright,
    /// so rooting a copy would allocate a second array for nothing.
    /// </remarks>
    public static double[] SquareRoots(double[] perOutput)
    {
        for (int i = 0; i < perOutput.Length; i++)
        {
            perOutput[i] = Math.Sqrt(perOutput[i]);
        }

        return perOutput;
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
    /// scikit-learn falls back to the plain mean rather than dividing by zero:
    /// <c>if not xp.any(nonzero_denominator): avg_weights = None</c>,
    /// <c>sklearn/metrics/_regression.py:982-986</c> (scikit-learn 1.9.0).
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
