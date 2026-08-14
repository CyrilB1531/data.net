using Xunit;

namespace DataNet.Metrics.Tests;

/// <summary>
/// The accumulation itself, on the shape issue #127 measured: a large offset over
/// a small spread, where a sequential sum loses the spread in the offset's low
/// bits. Asserted through <see cref="R2"/>, since <c>DataNet.Metrics</c> exposes
/// no internals to test directly. The reference is computed in
/// <see cref="decimal"/> — 28 significant digits against <see cref="double"/>'s 17
/// — so the expected value comes from genuinely more precise arithmetic, not a
/// second implementation of the thing under test.
/// </summary>
public sealed class CompensatedSumTests
{
    private const int Samples = 200_000;
    private const double Offset = 1e9;
    private const double Spread = 1e-2;
    private const int MagnitudeDisparitySamples = 1_000_000;

    /// <summary>
    /// The measured shape: a ramp of 83 887 distinct values over an offset that
    /// swamps it, perturbed by a multiple of 1e-6 — not smaller, since the ULP at
    /// 1e9 is about 1.19e-7 (<c>Math.BitIncrement(1e9) - 1e9</c>) and a smaller
    /// perturbation rounds back onto the target, scoring a perfect R² and proving
    /// nothing. The ramp's own step is 5e-8, below the ULP and deliberately so:
    /// quantizing it onto ULP multiples is what makes the target ill-conditioned
    /// in the first place.
    /// </summary>
    private static (double[] YTrue, double[] YPred) IllConditioned()
    {
        double step = Spread / Samples;
        double[] yTrue = new double[Samples];
        double[] yPred = new double[Samples];
        for (int i = 0; i < Samples; i++)
        {
            yTrue[i] = Offset + (i * step);
            yPred[i] = yTrue[i] + (((i % 7) - 3) * 1e-6);
        }
        return (yTrue, yPred);
    }

    /// <summary>
    /// R² of the <see cref="IllConditioned"/> fixture, checked against a decimal
    /// reference computed on the same residual and centred values <see cref="R2.Score"/>
    /// itself works from — not a closed form, since the perturbation cycles through
    /// seven distinct values (<c>((i % 7) - 3) * 1e-6</c>) rather than shifting every
    /// prediction by one constant, so there is no single <c>c</c> to square and
    /// multiply by <c>n</c>.
    /// </summary>
    [Fact]
    public void R2_matches_a_decimal_reference_on_an_ill_conditioned_target()
    {
        (double[] yTrue, double[] yPred) = IllConditioned();

        double expected = (double)ExactR2(yTrue, yPred);
        double actual = R2.Score(yTrue, yPred);

        Assert.Equal(expected, actual, 10);
    }

    /// <summary>
    /// The same data through a deliberately sequential sum, so that the test above is
    /// known to discriminate: if this one ever agreed with the decimal reference to
    /// twelve places, the first test would be passing for free.
    /// </summary>
    [Fact]
    public void A_sequential_sum_does_not_match_that_reference()
    {
        (double[] yTrue, double[] yPred) = IllConditioned();

        double sequential = SequentialR2(yTrue, yPred);
        double expected = (double)ExactR2(yTrue, yPred);

        Assert.NotEqual(expected, sequential, 10);
    }

    /// <summary>
    /// R² in <see cref="decimal"/> (28 significant digits against
    /// <see cref="double"/>'s 17), computed on <c>value - Offset</c> rather than on
    /// <paramref name="yTrue"/>/<paramref name="yPred"/> directly:
    /// <c>(decimal)double</c> rounds to 15 significant digits, and at this array's
    /// ~1e9 magnitude that budget is spent entirely on the offset, discarding the
    /// sub-1e-5 detail this test exists to see. Subtracting <see cref="Offset"/>
    /// first is exact (Sterbenz's lemma) and R² is shift-invariant, so the shifted
    /// array's R² is the unshifted one's, exactly.
    /// </summary>
    private static decimal ExactR2(double[] yTrue, double[] yPred)
    {
        decimal mean = 0m;
        foreach (double value in yTrue)
        {
            mean += (decimal)(value - Offset);
        }
        mean /= yTrue.Length;

        decimal numerator = 0m;
        decimal denominator = 0m;
        for (int i = 0; i < yTrue.Length; i++)
        {
            decimal residual = (decimal)(yTrue[i] - Offset) - (decimal)(yPred[i] - Offset);
            decimal centred = (decimal)(yTrue[i] - Offset) - mean;
            numerator += residual * residual;
            denominator += centred * centred;
        }
        return 1m - (numerator / denominator);
    }

    /// <summary>The accumulation this change replaces, kept here as the control.</summary>
    private static double SequentialR2(double[] yTrue, double[] yPred)
    {
        double mean = 0.0;
        foreach (double value in yTrue)
        {
            mean += value;
        }
        mean /= yTrue.Length;

        double numerator = 0.0;
        double denominator = 0.0;
        for (int i = 0; i < yTrue.Length; i++)
        {
            double residual = yTrue[i] - yPred[i];
            double centred = yTrue[i] - mean;
            numerator += residual * residual;
            denominator += centred * centred;
        }
        return 1.0 - (numerator / denominator);
    }

    /// <summary>
    /// Explained variance subtracts the mean residual before squaring, so it carries a
    /// second accumulation R² does not have. Same shape, same decimal reference.
    /// </summary>
    [Fact]
    public void ExplainedVariance_matches_a_decimal_reference_on_an_ill_conditioned_target()
    {
        (double[] yTrue, double[] yPred) = IllConditioned();

        double expected = (double)ExactExplainedVariance(yTrue, yPred);
        double actual = ExplainedVariance.Score(yTrue, yPred);

        Assert.Equal(expected, actual, 10);
    }

    /// <summary>
    /// Explained variance in <see cref="decimal"/>: 1 − Var(y − ŷ) ⁄ Var(y), computed
    /// on <c>value - Offset</c> for the same reason <see cref="ExactR2"/> is: explained
    /// variance is shift-invariant on the same grounds R² is — its numerator and
    /// denominator are both differences a common shift cancels out of before the
    /// square ever sees it — so the shifted array's score is the unshifted one's,
    /// exactly, and <c>value - Offset</c> stays exact by the same Sterbenz's-lemma
    /// argument <see cref="ExactR2"/> documents.
    /// </summary>
    private static decimal ExactExplainedVariance(double[] yTrue, double[] yPred)
    {
        decimal mean = 0m;
        decimal meanResidual = 0m;
        for (int i = 0; i < yTrue.Length; i++)
        {
            mean += (decimal)(yTrue[i] - Offset);
            meanResidual += (decimal)(yTrue[i] - Offset) - (decimal)(yPred[i] - Offset);
        }
        mean /= yTrue.Length;
        meanResidual /= yTrue.Length;

        decimal numerator = 0m;
        decimal denominator = 0m;
        for (int i = 0; i < yTrue.Length; i++)
        {
            decimal residual = (decimal)(yTrue[i] - Offset) - (decimal)(yPred[i] - Offset) - meanResidual;
            decimal centred = (decimal)(yTrue[i] - Offset) - mean;
            numerator += residual * residual;
            denominator += centred * centred;
        }
        return 1m - (numerator / denominator);
    }

    /// <summary>
    /// The shared walk accumulates the total weight as well as the values, over as many
    /// terms as there are samples. A million equal weights must still divide the sum by
    /// exactly what they add up to.
    /// </summary>
    [Fact]
    public void A_uniform_weight_changes_no_shared_mean()
    {
        const int Rows = 1_000_000;
        double[] yTrue = new double[Rows];
        double[] yPred = new double[Rows];
        double[] weights = new double[Rows];
        for (int i = 0; i < Rows; i++)
        {
            yTrue[i] = 1.0 + (i % 17);
            yPred[i] = yTrue[i] - 0.25;
            weights[i] = 0.1;
        }

        double unweighted = MeanSquaredError.Score(yTrue, yPred);
        double weighted = MeanSquaredError.Score(yTrue, yPred, sampleWeight: weights);

        Assert.Equal(unweighted, weighted, 15);
    }

    /// <summary>
    /// The shape that catches <c>Outputs.WeightedMean</c>'s compensation: one huge
    /// residual (<c>1e20</c>) among a million small ones (<c>10000</c> each), with
    /// no shared offset to cancel — unlike <see cref="IllConditioned"/>, measured
    /// bit-identical either way and so unable to discriminate. The large residual
    /// sits first, so a sequential sum accumulates the small terms on top of it
    /// (Neumaier's magnitude-compared branch): measured, a plain <c>+=</c> loop
    /// lands 6.38e9 from the exact sum of 100000000009999990000, while
    /// <see cref="CompensatedSum"/> lands 784 away — inside one ULP.
    /// </summary>
    private static (double[] YTrue, double[] YPred) MagnitudeDisparity()
    {
        double[] yTrue = new double[MagnitudeDisparitySamples];
        double[] yPred = new double[MagnitudeDisparitySamples];
        yTrue[0] = 1e10;
        for (int i = 1; i < MagnitudeDisparitySamples; i++)
        {
            yTrue[i] = 100.0;
        }
        return (yTrue, yPred);
    }

    /// <summary>
    /// Mean squared error in <see cref="decimal"/>, computed directly on
    /// <paramref name="yTrue"/> and <paramref name="yPred"/> — unlike <see cref="ExactR2"/>, no
    /// <c>value - Offset</c> shift is needed here: every value <see cref="MagnitudeDisparity"/>
    /// produces is a small integer (<c>0</c>, <c>100</c> or <c>1e10</c>), so
    /// <c>(decimal)double</c>'s 15-significant-digit rounding budget is never spent on digits
    /// this fixture does not have, the way it would be spent entirely on a ~1e9 offset.
    /// </summary>
    private static decimal ExactMse(double[] yTrue, double[] yPred)
    {
        decimal sum = 0m;
        for (int i = 0; i < yTrue.Length; i++)
        {
            decimal residual = (decimal)yTrue[i] - (decimal)yPred[i];
            sum += residual * residual;
        }
        return sum / yTrue.Length;
    }

    /// <summary>The shared walk's original accumulation, kept here as the control.</summary>
    private static double SequentialMse(double[] yTrue, double[] yPred)
    {
        double sum = 0.0;
        for (int i = 0; i < yTrue.Length; i++)
        {
            double residual = yTrue[i] - yPred[i];
            sum += residual * residual;
        }
        return sum / yTrue.Length;
    }

    /// <summary>
    /// <see cref="MeanSquaredError"/> walks through <c>Outputs.WeightedMean</c> on the
    /// <see cref="MagnitudeDisparity"/> shape, which — unlike the two existing fixtures in this
    /// class — actually discriminates a compensated walk from a reverted one. See the
    /// remarks on <see cref="MagnitudeDisparity"/> for why.
    /// </summary>
    [Fact]
    public void MeanSquaredError_matches_a_decimal_reference_on_a_magnitude_disparity_target()
    {
        (double[] yTrue, double[] yPred) = MagnitudeDisparity();

        double expected = (double)ExactMse(yTrue, yPred);
        double actual = MeanSquaredError.Score(yTrue, yPred);

        Assert.Equal(expected, actual, 0);
    }

    /// <summary>
    /// The same data through a deliberately sequential sum, so the test above is known to
    /// discriminate: if this one ever agreed with the decimal reference, the first test would
    /// be passing for free.
    /// </summary>
    [Fact]
    public void A_sequential_sum_does_not_match_that_reference_on_magnitude_disparity()
    {
        (double[] yTrue, double[] yPred) = MagnitudeDisparity();

        double sequential = SequentialMse(yTrue, yPred);
        double expected = (double)ExactMse(yTrue, yPred);

        Assert.NotEqual(expected, sequential, 0);
    }
}
