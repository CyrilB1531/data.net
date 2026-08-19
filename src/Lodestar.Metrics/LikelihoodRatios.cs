using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// The two class likelihood ratios — the equivalent of
/// <c>sklearn.metrics.class_likelihood_ratios</c>.
/// </summary>
/// <remarks>
/// How much a prediction should move a belief, independently of how common the class
/// is: a positive result multiplies the prior odds by <see cref="Positive"/> and a
/// negative one by <see cref="Negative"/>. That prevalence-independence is what
/// separates them from precision, which moves with the base rate.
/// </remarks>
public sealed class LikelihoodRatios
{
    private LikelihoodRatios(double positive, double negative)
    {
        Positive = positive;
        Negative = negative;
    }

    /// <summary>The positive ratio, <c>LR+</c>: sensitivity over one minus specificity. Above <c>1</c> when a positive prediction is evidence for the class.</summary>
    public double Positive { get; }

    /// <summary>The negative ratio, <c>LR-</c>: one minus sensitivity, over specificity. Below <c>1</c> when a negative prediction is evidence against the class.</summary>
    public double Negative { get; }

    /// <summary>Both ratios — <c>class_likelihood_ratios(y_true, y_pred, sample_weight=…, replace_undefined_by=…)</c>.</summary>
    /// <param name="yTrue">The true labels, one per sample. Exactly two distinct values may occur.</param>
    /// <param name="yPred">The predicted labels, same length as <paramref name="yTrue"/>.</param>
    /// <param name="posLabel">The label counted as positive. scikit-learn takes the greater of the two through <c>labels</c>; this asks.</param>
    /// <param name="undefinedPositive">What <see cref="Positive"/> answers when it has no value. The default reproduces <c>replace_undefined_by=nan</c>.</param>
    /// <param name="undefinedNegative">What <see cref="Negative"/> answers when it has no value.</param>
    /// <param name="sampleWeight">A weight per sample. Omit to weight every sample by 1.</param>
    /// <returns>
    /// Both ratios. Either is replaced when it has no value: <see cref="Positive"/>
    /// when no sample was predicted into the class wrongly, <see cref="Negative"/>
    /// when none was predicted out of it rightly, and **both** when the truth carries
    /// only one of the two classes.
    /// </returns>
    /// <exception cref="ArgumentException">The inputs disagree in length, are empty, the weights do not match, or more than two distinct labels occur.</exception>
    public static LikelihoodRatios Compute(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<int> yPred,
        int posLabel = 1,
        double undefinedPositive = double.NaN,
        double undefinedNegative = double.NaN,
        ReadOnlySpan<double> sampleWeight = default)
    {
        Inputs.Validate(yTrue, yPred, sampleWeight);
        RequireBinary(yTrue, yPred);

        double truePositive = 0.0;
        double falseNegative = 0.0;
        double falsePositive = 0.0;
        double trueNegative = 0.0;

        for (int i = 0; i < yTrue.Length; i++)
        {
            double weight = sampleWeight.IsEmpty ? 1.0 : sampleWeight[i];
            bool actual = yTrue[i] == posLabel;
            bool guessed = yPred[i] == posLabel;

            if (actual)
            {
                if (guessed)
                {
                    truePositive += weight;
                }
                else
                {
                    falseNegative += weight;
                }
            }
            else if (guessed)
            {
                falsePositive += weight;
            }
            else
            {
                trueNegative += weight;
            }
        }

        double positives = truePositive + falseNegative;
        double negatives = trueNegative + falsePositive;

        // S1244: whether a class is absent, which is what the reference tests before
        // dividing. The two absences do not answer alike: with no positive sample
        // there is no sensitivity to build either ratio from, and the reference
        // returns nan *without* substituting -- measured, replace_undefined_by=1
        // leaves (nan, nan) there and gives (1, 1) when the negatives are missing.
#pragma warning disable S1244
        if (positives == 0.0)
        {
            return new LikelihoodRatios(double.NaN, double.NaN);
        }

        if (negatives == 0.0)
        {
            return new LikelihoodRatios(undefinedPositive, undefinedNegative);
        }
#pragma warning restore S1244

        double sensitivity = truePositive / positives;
        double specificity = trueNegative / negatives;

        return new LikelihoodRatios(
            Ratio(sensitivity, 1.0 - specificity, undefinedPositive),
            Ratio(1.0 - sensitivity, specificity, undefinedNegative));
    }

    // S1244: whether the denominator vanished, which is the reference's own test --
    // it warns that the ratio is ill-defined and substitutes rather than dividing.
#pragma warning disable S1244
    private static double Ratio(double numerator, double denominator, double undefined) =>
        denominator == 0.0 ? undefined : numerator / denominator;
#pragma warning restore S1244

    /// <summary>Refuses more than two classes, as the reference does.</summary>
    private static void RequireBinary(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred)
    {
        var seen = new SortedSet<int>();
        for (int i = 0; i < yTrue.Length; i++)
        {
            seen.Add(yTrue[i]);
            seen.Add(yPred[i]);
            if (seen.Count > 2)
            {
                throw new ArgumentException(
                    "class_likelihood_ratios only supports binary classification problems.",
                    nameof(yTrue));
            }
        }
    }
}
