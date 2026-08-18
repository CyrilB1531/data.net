namespace Lodestar.Metrics.Internal;

/// <summary>
/// What the two calibration metrics share: agreeing that a probability really is one,
/// and reading a class column out of a row-major block.
/// </summary>
internal static class Probabilities
{
    /// <summary>
    /// numpy's machine epsilon, <c>np.finfo(np.float64).eps</c> — what
    /// <c>log_loss</c> clips a probability to before taking its logarithm.
    /// </summary>
    /// <remarks>
    /// Measured rather than assumed, the bound having moved across versions: two
    /// samples of <c>[0.0, 0.5]</c> against <c>[1, 0]</c> score 18.36840028483855,
    /// which is <c>(-log(eps) - log(0.5)) / 2</c> exactly. Not
    /// <see cref="double.Epsilon"/>, 292 orders of magnitude smaller.
    /// </remarks>
    public const double Epsilon = 2.220446049250313e-16;

    /// <summary>Clips into <c>[eps, 1 - eps]</c>, so no logarithm is taken of zero.</summary>
    public static double Clip(double probability)
    {
        if (probability < Epsilon)
        {
            return Epsilon;
        }

        return probability > 1.0 - Epsilon ? 1.0 - Epsilon : probability;
    }

    /// <summary>Refuses a value outside <c>[0, 1]</c>, in the reference's own words.</summary>
    /// <param name="probabilities">The values to check.</param>
    /// <param name="lowerWord">"lower than" for <c>log_loss</c>, "less than" for <c>brier_score_loss</c> — the two word it differently and each is reproduced.</param>
    /// <exception cref="ArgumentException">A value falls outside <c>[0, 1]</c>.</exception>
    public static void RequireProbabilities(ReadOnlySpan<double> probabilities, string lowerWord)
    {
        for (int i = 0; i < probabilities.Length; i++)
        {
            double p = probabilities[i];
            if (p > 1.0)
            {
                throw new ArgumentException(
                    $"y_prob contains values greater than 1: {Format(p)}", nameof(probabilities));
            }

            if (p < 0.0 || double.IsNaN(p))
            {
                throw new ArgumentException(
                    $"y_prob contains values {lowerWord} 0: {Format(p)}", nameof(probabilities));
            }
        }
    }

    /// <summary>Checks a labels-and-probabilities pair, the binary shape both metrics take.</summary>
    /// <exception cref="ArgumentException">The lengths disagree, the input is empty, or the weights do not match.</exception>
    public static void ValidateBinary(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yProba, ReadOnlySpan<double> sampleWeight)
    {
        if (yTrue.Length != yProba.Length)
        {
            throw new ArgumentException(
                $"yTrue has {yTrue.Length} entries and yProba has {yProba.Length}; they must agree.",
                nameof(yProba));
        }

        if (yTrue.Length == 0)
        {
            throw new ArgumentException("yTrue and yProba are empty; there is nothing to score.", nameof(yTrue));
        }

        if (!sampleWeight.IsEmpty && sampleWeight.Length != yTrue.Length)
        {
            throw new ArgumentException(
                $"sampleWeight holds {sampleWeight.Length} values for {yTrue.Length} samples.",
                nameof(sampleWeight));
        }
    }

    /// <summary>Checks a row-major probability block and returns the sample count.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="classCount"/> is below two.</exception>
    /// <exception cref="ArgumentException">The block is not <c>yTrue.Length × classCount</c>, or a label is outside <c>[0, classCount)</c>.</exception>
    public static int Samples(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yProba, int classCount, ReadOnlySpan<double> sampleWeight)
    {
        if (classCount < 2)
        {
            throw new ArgumentOutOfRangeException(
                nameof(classCount), classCount, "A probability matrix needs at least two classes.");
        }

        if (yProba.Length != yTrue.Length * classCount)
        {
            throw new ArgumentException(
                $"yProba holds {yProba.Length} values, which is not {yTrue.Length} samples of {classCount}.",
                nameof(yProba));
        }

        for (int i = 0; i < yTrue.Length; i++)
        {
            if (yTrue[i] < 0 || yTrue[i] >= classCount)
            {
                throw new ArgumentException(
                    $"yTrue[{i}] is {yTrue[i]}, which is not a class index below {classCount}.",
                    nameof(yTrue));
            }
        }

        if (!sampleWeight.IsEmpty && sampleWeight.Length != yTrue.Length)
        {
            throw new ArgumentException(
                $"sampleWeight holds {sampleWeight.Length} values for {yTrue.Length} samples.",
                nameof(sampleWeight));
        }

        return yTrue.Length;
    }

    private static string Format(double value) =>
        value.ToString("0.###############", System.Globalization.CultureInfo.InvariantCulture);
}
