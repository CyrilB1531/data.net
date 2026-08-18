using System.Globalization;

namespace Lodestar.Metrics.Internal;

/// <summary>
/// The Tweedie deviance and the five regimes its <c>power</c> selects.
/// </summary>
/// <remarks>
/// The formula and the domain of validity both change with the power, which is
/// the whole content of this family: which of the two arguments may be zero or
/// negative differs per regime, and each refusal has its own sentence in
/// scikit-learn that this reproduces.
/// </remarks>
internal static class Tweedie
{
    /// <summary>The per-pair deviance at <paramref name="power"/>.</summary>
    public static double Deviance(double truth, double prediction, double power)
    {
        // Exact comparisons: these select a formula rather than measure a
        // quantity, and the reference branches on the same three values.
#pragma warning disable S1244
        if (power == 0.0)
        {
            double residual = truth - prediction;
            return residual * residual;
        }

        if (power == 1.0)
        {
            // x*log(x/y) is taken as 0 at x = 0, which is its limit and numpy's xlogy.
            double term = truth == 0.0 ? 0.0 : truth * Math.Log(truth / prediction);
            return 2.0 * (term - truth + prediction);
        }

        if (power == 2.0)
        {
            return 2.0 * (Math.Log(prediction / truth) + (truth / prediction) - 1.0);
        }
#pragma warning restore S1244

        double positiveTruth = Math.Max(truth, 0.0);
        return 2.0 * ((Math.Pow(positiveTruth, 2.0 - power) / ((1.0 - power) * (2.0 - power)))
            - (truth * Math.Pow(prediction, 1.0 - power) / (1.0 - power))
            + (Math.Pow(prediction, 2.0 - power) / (2.0 - power)));
    }

    /// <summary>Refuses a power scikit-learn has no distribution for, and inputs outside the regime's domain.</summary>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="power"/> lies in the open interval (0, 1).</exception>
    /// <exception cref="ArgumentException">An operand is outside what the regime allows.</exception>
    public static void Require(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, double power)
    {
        RequirePower(power);

        if (power < 0.0)
        {
            RequireStrictlyPositive(yPred, power, "strictly positive y_pred");
            return;
        }

#pragma warning disable S1244
        if (power == 0.0)
        {
            return;
        }
#pragma warning restore S1244

        if (power < 2.0)
        {
            RequireNonNegative(yTrue, power, "non-negative y and strictly positive y_pred");
            RequireStrictlyPositive(yPred, power, "non-negative y and strictly positive y_pred");
            return;
        }

        RequireStrictlyPositive(yTrue, power, "strictly positive y and y_pred");
        RequireStrictlyPositive(yPred, power, "strictly positive y and y_pred");
    }

    /// <summary>The mean deviance of a constant prediction, which is every D² denominator.</summary>
    public static double DevianceAgainst(
        ReadOnlySpan<double> yTrue, double constant, double power, ReadOnlySpan<double> sampleWeight)
    {
        CompensatedSum sum = default;
        double total = 0.0;
        for (int i = 0; i < yTrue.Length; i++)
        {
            double weight = sampleWeight.IsEmpty ? 1.0 : sampleWeight[i];
            sum.Add(weight * Deviance(yTrue[i], constant, power));
            total += weight;
        }

        return sum.Value / total;
    }

    /// <summary>The weighted mean of <paramref name="values"/>, the constant a Tweedie D² compares against.</summary>
    public static double Mean(ReadOnlySpan<double> values, ReadOnlySpan<double> sampleWeight)
    {
        CompensatedSum sum = default;
        double total = 0.0;
        for (int i = 0; i < values.Length; i++)
        {
            double weight = sampleWeight.IsEmpty ? 1.0 : sampleWeight[i];
            sum.Add(weight * values[i]);
            total += weight;
        }

        return sum.Value / total;
    }

    private static void RequirePower(double power)
    {
        if (power > 0.0 && power < 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(power),
                power,
                "There is no distribution in the Tweedie family between the normal at 0 and "
                + "the Poisson at 1; the power must be at most 0 or at least 1.");
        }
    }

    private static void RequireNonNegative(ReadOnlySpan<double> values, double power, string allowed)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] < 0.0)
            {
                throw Refusal(power, allowed);
            }
        }
    }

    private static void RequireStrictlyPositive(ReadOnlySpan<double> values, double power, string allowed)
    {
        for (int i = 0; i < values.Length; i++)
        {
            if (values[i] <= 0.0)
            {
                throw Refusal(power, allowed);
            }
        }
    }

    private static ArgumentException Refusal(double power, string allowed) =>
        new($"Mean Tweedie deviance error with power={power.ToString(CultureInfo.InvariantCulture)} "
            + $"can only be used on {allowed}.");
}
