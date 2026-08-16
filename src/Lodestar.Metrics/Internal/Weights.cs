namespace Lodestar.Metrics.Internal;

/// <summary>A <c>sample_weight</c>, validated and applied, for every metric that takes one.</summary>
/// <remarks>
/// Shared rather than written per family: <c>numpy.average</c> is what the reference calls
/// throughout, so the zero-sum refusal and its sentence have to be one thing. The ranking
/// metrics reach it through <see cref="Ranking"/> and the label-matrix ones through
/// <see cref="LabelRanking"/>; <c>average_precision_score</c> will want it too (#210).
/// </remarks>
internal static class Weights
{
    /// <summary>Refuses a weight vector that does not carry one value per sample.</summary>
    /// <exception cref="ArgumentException"><paramref name="sampleWeight"/> is neither empty nor <paramref name="rows"/> long.</exception>
    public static void Validate(ReadOnlySpan<double> sampleWeight, int rows, string name)
    {
        if (sampleWeight.Length != 0 && sampleWeight.Length != rows)
        {
            throw new ArgumentException(
                $"{name} holds {sampleWeight.Length} values for {rows} samples; " +
                "they must agree.",
                name);
        }
    }

    /// <summary>The mean of the per-sample values, weighted when weights are given.</summary>
    /// <remarks>
    /// A weight vector summing to zero raises, in <c>numpy.average</c>'s own sentence, where
    /// the reference raises <c>ZeroDivisionError</c> from the same call. A negative weight is
    /// accepted on both sides and takes the result outside the range its page promises.
    /// </remarks>
    /// <exception cref="ArgumentException"><paramref name="sampleWeight"/> sums to zero.</exception>
    public static double Mean(ReadOnlySpan<double> perSample, ReadOnlySpan<double> sampleWeight)
    {
        if (sampleWeight.Length == 0)
        {
            double plain = 0.0;
            foreach (double value in perSample)
            {
                plain += value;
            }

            return plain / perSample.Length;
        }

        return Total(perSample, sampleWeight) / Sum(sampleWeight, throwOnZero: true);
    }

    /// <summary>The weighted sum of the per-sample values, which never divides.</summary>
    /// <remarks>
    /// What <c>top_k_accuracy_score(normalize=False)</c> returns: the weights of the hits
    /// added up rather than counted. Measured, <c>7.0</c> where the unweighted count is
    /// <c>3.0</c>. Because it does not divide, a zero-sum vector gives <c>0</c> here where
    /// <see cref="Mean"/> raises — a divergence of the reference's, reproduced.
    /// </remarks>
    public static double Total(ReadOnlySpan<double> perSample, ReadOnlySpan<double> sampleWeight)
    {
        double total = 0.0;
        for (int sample = 0; sample < perSample.Length; sample++)
        {
            total += perSample[sample] * (sampleWeight.Length == 0 ? 1.0 : sampleWeight[sample]);
        }

        return total;
    }

    /// <summary>The sum of the weights, refusing zero when the caller is about to divide.</summary>
    public static double Sum(ReadOnlySpan<double> sampleWeight, bool throwOnZero)
    {
        double weights = 0.0;
        foreach (double weight in sampleWeight)
        {
            weights += weight;
        }

        // S1244: the reference compares the sum to zero exactly, and a tolerance would
        // refuse weights numpy accepts. Its own message is reproduced below.
#pragma warning disable S1244
        if (throwOnZero && weights == 0.0)
#pragma warning restore S1244
        {
            throw new ArgumentException(
                "Weights sum to zero, can't be normalized.", nameof(sampleWeight));
        }

        return weights;
    }
}
