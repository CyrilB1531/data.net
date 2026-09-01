namespace Lodestar.Conformal;

/// <summary>
/// Split conformal prediction: turns a point prediction into an interval, or a class
/// into a prediction set, with a finite-sample coverage guarantee.
/// </summary>
/// <remarks>
/// <para>
/// Reproduces <c>mapie.regression.SplitConformalRegressor</c> and
/// <c>mapie.classification.SplitConformalClassifier</c> with <c>conformity_score="lac"</c>,
/// both <c>prefit</c>. Post-hoc arithmetic over scores and labels: no model, no training
/// loop, nothing to serialize. All members are stateless and thread-safe.
/// </para>
/// <para>
/// <b>The guarantee assumes exchangeability.</b> Coverage holds when the calibration and
/// test data are exchangeable. It does <b>not</b> hold for time series, for data with
/// drift, or for any split that leaks — the intervals still come out, they simply do not
/// cover, and nothing in the output says so. See <c>docs/guides/conformal.md</c>.
/// </para>
/// </remarks>
public static class SplitConformal
{
    /// <summary>
    /// The calibrated quantile: the score a new point must not exceed to fall inside the
    /// prediction, at miscoverage level <paramref name="alpha"/>.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <c>k</c>-th smallest score with <c>k = ceil((n + 1) · (1 − alpha))</c>, which is
    /// what MAPIE computes as <c>numpy.quantile(scores, (1 − alpha)(n + 1)/n,
    /// method="higher")</c>. The ceiling form is implemented because it says what it means.
    /// </para>
    /// <para>
    /// When <c>k</c> exceeds <paramref name="scores"/>'s length the rule asks for a score
    /// that does not exist — the calibration set is too small for the level — and the honest
    /// answer is <see cref="double.PositiveInfinity"/>: a trivial prediction, with real
    /// coverage. <see cref="Interval"/> and <see cref="PredictionSet"/> both carry it through.
    /// </para>
    /// <para><b>The guarantee assumes exchangeability</b> — see the type's remarks.</para>
    /// </remarks>
    /// <param name="scores">The calibration scores; not modified.</param>
    /// <param name="alpha">Miscoverage level in <c>(0, 1)</c>: 0.1 asks for 90 % coverage.</param>
    /// <exception cref="ArgumentException"><paramref name="scores"/> is empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="alpha"/> is not in <c>(0, 1)</c>.</exception>
    public static double Quantile(ReadOnlySpan<double> scores, double alpha)
    {
        if (scores.Length == 0)
        {
            throw new ArgumentException("Conformal calibration needs at least one score.", nameof(scores));
        }
        // NaN spelled out rather than left to a negated comparison: `!(a > 0 && a < 1)`
        // rejects it and `a <= 0 || a >= 1` accepts it, and the reader should not have to know.
        if (double.IsNaN(alpha) || alpha <= 0.0 || alpha >= 1.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(alpha), alpha, "The miscoverage level must lie strictly between 0 and 1.");
        }

        int n = scores.Length;
        int k = (int)Math.Ceiling((n + 1) * (1.0 - alpha));
        if (k > n)
        {
            return double.PositiveInfinity;
        }

        // Sorted rather than selected: n is the calibration size, which is small next to the
        // predictions the quantile is then applied to, and a copy keeps the caller's span intact.
        double[] sorted = scores.ToArray();
        Array.Sort(sorted);
        return sorted[k - 1];
    }

    /// <summary>The absolute-residual calibration scores of a regressor, <c>|y − ŷ|</c>.</summary>
    /// <remarks>
    /// MAPIE's <c>AbsoluteConformityScore</c>, which is what <c>SplitConformalRegressor</c>
    /// uses by default. Hand this to <see cref="Quantile"/>.
    /// </remarks>
    /// <param name="yTrue">The observed values.</param>
    /// <param name="yPredicted">The model's predictions, same length as <paramref name="yTrue"/>.</param>
    /// <exception cref="ArgumentException">The two spans have different lengths.</exception>
    public static double[] AbsoluteResiduals(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPredicted)
    {
        if (yTrue.Length != yPredicted.Length)
        {
            throw new ArgumentException(
                $"There are {yTrue.Length} observed values but {yPredicted.Length} predictions.",
                nameof(yPredicted));
        }

        double[] residuals = new double[yTrue.Length];
        for (int i = 0; i < yTrue.Length; i++)
        {
            residuals[i] = Math.Abs(yTrue[i] - yPredicted[i]);
        }
        return residuals;
    }

    /// <summary>The prediction interval <c>[ŷ − q, ŷ + q]</c> around a point prediction.</summary>
    /// <remarks>
    /// An infinite <paramref name="quantile"/> — see <see cref="Quantile"/> — yields the whole
    /// line, which is the trivial prediction the calibration size forced.
    /// <b>The guarantee assumes exchangeability</b>; see the type's remarks.
    /// </remarks>
    /// <param name="prediction">The model's point prediction.</param>
    /// <param name="quantile">The calibrated quantile from <see cref="Quantile"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="quantile"/> is negative or NaN.</exception>
    public static (double Lower, double Upper) Interval(double prediction, double quantile)
    {
        if (double.IsNaN(quantile) || quantile < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantile), quantile, "A calibrated quantile is a non-negative score.");
        }

        return (prediction - quantile, prediction + quantile);
    }

    /// <summary>The LAC calibration scores of a classifier, <c>1 − p̂(true class)</c>.</summary>
    /// <remarks>
    /// MAPIE's <c>conformity_score="lac"</c>. <paramref name="probabilities"/> is row-major,
    /// one row per calibration sample and <paramref name="classCount"/> values each, in the
    /// same class order <see cref="PredictionSet"/> will be given.
    /// </remarks>
    /// <param name="probabilities">The predicted probabilities, row-major.</param>
    /// <param name="labels">The index of each sample's true class.</param>
    /// <param name="classCount">How many classes each row holds.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="classCount"/> is not positive.</exception>
    /// <exception cref="ArgumentException">The shapes disagree, or a label is outside the class range.</exception>
    public static double[] LeastAmbiguousScores(
        ReadOnlySpan<double> probabilities, ReadOnlySpan<int> labels, int classCount)
    {
        if (classCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(classCount), classCount, "A classifier has at least one class.");
        }
        if (probabilities.Length != labels.Length * classCount)
        {
            throw new ArgumentException(
                $"{labels.Length} samples of {classCount} classes need {labels.Length * classCount} "
                    + $"probabilities, not {probabilities.Length}.",
                nameof(probabilities));
        }

        double[] scores = new double[labels.Length];
        for (int i = 0; i < labels.Length; i++)
        {
            int label = labels[i];
            if (label < 0 || label >= classCount)
            {
                throw new ArgumentException(
                    $"Sample {i} has class {label}, outside [0, {classCount}).", nameof(labels));
            }
            scores[i] = 1.0 - probabilities[(i * classCount) + label];
        }
        return scores;
    }

    /// <summary>The prediction set: every class whose probability clears <c>1 − q</c>.</summary>
    /// <remarks>
    /// <para>
    /// MAPIE's LAC rule, reproduced including its edges. <b>The set can be empty</b>, when no
    /// class clears the threshold; substituting the most likely class there would return
    /// something with no coverage guarantee under a name that promises one. An infinite
    /// <paramref name="quantile"/> returns every class, which is the trivial prediction.
    /// </para>
    /// <para><b>The guarantee assumes exchangeability</b>; see the type's remarks.</para>
    /// </remarks>
    /// <param name="probabilities">One sample's predicted probabilities, in calibration order.</param>
    /// <param name="quantile">The calibrated quantile from <see cref="Quantile"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="quantile"/> is negative or NaN.</exception>
    public static bool[] PredictionSet(ReadOnlySpan<double> probabilities, double quantile)
    {
        if (double.IsNaN(quantile) || quantile < 0.0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantile), quantile, "A calibrated quantile is a non-negative score.");
        }

        double threshold = 1.0 - quantile;
        bool[] included = new bool[probabilities.Length];
        for (int i = 0; i < probabilities.Length; i++)
        {
            included[i] = probabilities[i] >= threshold;
        }
        return included;
    }
}
