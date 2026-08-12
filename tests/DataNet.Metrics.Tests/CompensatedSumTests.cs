using Xunit;

namespace DataNet.Metrics.Tests;

/// <summary>
/// The accumulation itself, on the shape issue #127 measured: a large offset over a
/// small spread, where a sequential sum loses the spread in the offset's low bits.
/// </summary>
/// <remarks>
/// Asserted through <see cref="R2"/> rather than against the internal accumulator,
/// because <c>DataNet.Metrics</c> exposes no internals to its tests and adding that
/// exposure for one test would be a larger change than the fix. The reference is
/// computed in <see cref="decimal"/> — 28 significant digits against
/// <see cref="double"/>'s 17 — so the expected value comes from a genuinely more
/// precise arithmetic rather than from a second implementation of the thing under
/// test.
/// </remarks>
public sealed class CompensatedSumTests
{
    private const int Samples = 200_000;
    private const double Offset = 1e9;
    private const double Spread = 1e-2;

    /// <summary>
    /// The measured shape: a ramp of 83 887 distinct values over an offset that swamps
    /// it, and a prediction perturbed by a multiple of 1e-6.
    /// </summary>
    /// <remarks>
    /// 1e-6 and not smaller: the ULP at 1e9 is about 1.19e-7
    /// (<c>Math.BitIncrement(1e9) - 1e9</c>), so a perturbation below half of that
    /// rounds back onto the target and every residual becomes exactly zero — which
    /// scores a perfect R² and proves nothing. The ramp's own step is 5e-8, below
    /// the ULP and deliberately so: quantizing it onto ULP multiples is what makes
    /// the target ill-conditioned in the first place.
    /// </remarks>
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
    /// R² in <see cref="decimal"/>, which carries eleven more digits than <see cref="double"/> —
    /// computed on <c>value - Offset</c> rather than on <paramref name="yTrue"/> and
    /// <paramref name="yPred"/> directly.
    /// </summary>
    /// <remarks>
    /// <c>(decimal)double</c> is not the higher-precision widening it looks like: the .NET
    /// conversion rounds to 15 significant digits, and at this array's magnitude — around
    /// <c>1e9</c> — that budget is spent entirely on the offset, discarding exactly the
    /// sub-1e-5 detail this test exists to see. Subtracting <see cref="Offset"/> first fixes
    /// that, and does so exactly rather than approximately, for two independent reasons:
    /// <list type="bullet">
    /// <item><c>value - Offset</c> is exact in <see cref="double"/>. <see cref="Offset"/> is a
    /// power of ten small enough to be exactly representable, and every <c>value</c> here is
    /// within a factor of two of it, so Sterbenz's lemma applies: the subtraction rounds
    /// nowhere and the shifted double carries the full information the original held.</item>
    /// <item>R² is shift-invariant. Its numerator is <c>Σ(y − ŷ)²</c>, a difference a common
    /// shift cancels out of before the square ever sees it; its denominator is
    /// <c>Σ(y − ȳ)²</c>, a variance, equally blind to where the data sits on the number line.
    /// So the R² of the shifted array is not an approximation of the unshifted one's R² — it
    /// is the same number.</item>
    /// </list>
    /// </remarks>
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
}
