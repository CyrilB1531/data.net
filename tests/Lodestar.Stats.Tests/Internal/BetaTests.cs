using Lodestar.Stats.Internal;
using Xunit;

namespace Lodestar.Stats.Tests.Internal;

/// <summary>
/// The regularized incomplete beta and the two tails built on it, against
/// closed forms rather than against an oracle: an incomplete beta that is wrong
/// only in the far tail passes every corpus whose p-values sit near 0.05.
/// </summary>
public sealed class BetaTests
{
    private const double Tolerance = 1e-13;

    [Theory]
    // I_x(1, 1) = x: the uniform distribution's CDF.
    [InlineData(1.0, 1.0, 0.25, 0.25)]
    // I_x(1, b) = 1 - (1-x)^b.
    [InlineData(1.0, 3.0, 0.5, 0.875)]
    // I_x(a, 1) = x^a.
    [InlineData(3.0, 1.0, 0.5, 0.125)]
    // Symmetry at the midpoint: I_{1/2}(a, a) = 1/2 for every a.
    [InlineData(7.5, 7.5, 0.5, 0.5)]
    [InlineData(0.5, 0.5, 0.5, 0.5)]
    // The endpoints.
    [InlineData(2.0, 3.0, 0.0, 0.0)]
    [InlineData(2.0, 3.0, 1.0, 1.0)]
    public void RegularizedIncomplete_matches_the_closed_forms(
        double a, double b, double x, double expected)
    {
        Assert.Equal(expected, Beta.RegularizedIncomplete(a, b, x), Tolerance);
    }

    [Theory]
    // The fraction converges on one side of (a+1)/(a+b+2); the reflection
    // I_x(a,b) = 1 - I_{1-x}(b,a) carries the other, and the seam is where they can differ.
    [InlineData(4.0, 9.0, 0.3)]
    [InlineData(4.0, 9.0, 0.3333333333333333)]
    [InlineData(4.0, 9.0, 0.4)]
    [InlineData(60.0, 60.0, 0.51)]
    public void RegularizedIncomplete_is_complementary_across_the_seam(double a, double b, double x)
    {
        double left = Beta.RegularizedIncomplete(a, b, x);

        // Swapped (b, a) is the complement identity under test, not a mistake.
#pragma warning disable S2234
        double right = Beta.RegularizedIncomplete(b, a, 1.0 - x);
#pragma warning restore S2234

        Assert.Equal(1.0, left + right, 1e-14);
    }

    [Fact]
    public void RegularizedIncomplete_refuses_an_x_outside_the_unit_interval()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Beta.RegularizedIncomplete(2.0, 2.0, -0.1));
        Assert.Throws<ArgumentOutOfRangeException>(() => Beta.RegularizedIncomplete(2.0, 2.0, 1.1));
    }

    [Theory]
    // Student's t with 1 degree of freedom is the Cauchy distribution: its upper
    // tail is 1/2 - atan(t)/pi, a closed form the implementation cannot have been fitted to.
    [InlineData(0.0, 1.0, 0.5)]
    [InlineData(1.0, 1.0, 0.25)]
    [InlineData(-1.0, 1.0, 0.75)]
    [InlineData(10.0, 1.0, 0.03172551743055357)]
    // With 2 degrees of freedom the tail is (1 - t/sqrt(t^2+2))/2.
    [InlineData(2.0, 2.0, 0.09175170953613698)]
    public void StudentSf_matches_the_closed_forms(double t, double df, double expected)
    {
        Assert.Equal(expected, Beta.StudentSf(t, df), Tolerance);
    }

    [Fact]
    public void StudentSf_stays_accurate_in_the_far_tail()
    {
        // Relative, not absolute: at 1e-27 an absolute 1e-9 check would pass an
        // implementation that returns zero -- why this layer is tested directly, not only via a corpus.
        double actual = Beta.StudentSf(12.0, 30.0);

        Assert.Equal(1.0, actual / 2.7900927075996303e-13, 1e-9);
    }

    [Theory]
    // F(1, d) is the square of a t with d degrees of freedom, so the F upper
    // tail at f equals twice the t upper tail at sqrt(f).
    [InlineData(4.0, 1.0, 10.0)]
    [InlineData(0.5, 1.0, 25.0)]
    public void FisherSf_is_twice_the_student_tail_at_the_square_root(double f, double dfn, double dfd)
    {
        Assert.Equal(
            2.0 * Beta.StudentSf(Math.Sqrt(f), dfd),
            Beta.FisherSf(f, dfn, dfd),
            Tolerance);
    }

    [Fact]
    public void FisherSf_is_one_at_the_origin()
    {
        Assert.Equal(1.0, Beta.FisherSf(0.0, 3.0, 12.0));
    }
}
