namespace DataNet.Metrics.Internal;

/// <summary>
/// The binary ROC curve and the area under it — the mechanics of
/// scikit-learn's <c>_binary_clf_curve</c> followed by <c>auc</c>.
/// </summary>
/// <remarks>
/// Samples are sorted by descending score and equal scores are consumed as one
/// group, which is what makes ties come out the same as scikit-learn's. The
/// trapezoid is accumulated on unnormalised counts and divided once at the end:
/// fewer roundings, and the same number.
/// </remarks>
internal static class BinaryRoc
{
    private struct Point
    {
        public double Weight;
        public double PositiveWeight;
    }

    public static double Score(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int posLabel, ReadOnlySpan<double> sampleWeight)
    {
        int n = Validate(yTrue, yScore, sampleWeight);

        // Negated scores, so an ascending sort walks the curve from the highest
        // score down — and Array.Sort compares doubles natively rather than
        // through a delegate.
        double[] keys = new double[n];
        Point[] points = new Point[n];
        BuildPoints(yTrue, yScore, posLabel, sampleWeight, keys, points);

        Array.Sort(keys, points);

        return Accumulate(keys, points);
    }

    private static int Validate(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, ReadOnlySpan<double> sampleWeight)
    {
        int n = yTrue.Length;
        if (yScore.Length != n)
        {
            throw new ArgumentException(
                $"yTrue has {n} entries and yScore has {yScore.Length}; they must agree.", nameof(yScore));
        }
        if (n == 0)
        {
            throw new ArgumentException("yTrue and yScore are empty; there is nothing to score.", nameof(yTrue));
        }
        if (!sampleWeight.IsEmpty && sampleWeight.Length != n)
        {
            throw new ArgumentException(
                $"sampleWeight has {sampleWeight.Length} entries but there are {n} samples.",
                nameof(sampleWeight));
        }

        return n;
    }

    private static void BuildPoints(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int posLabel, ReadOnlySpan<double> sampleWeight,
        double[] keys, Point[] points)
    {
        bool weighted = !sampleWeight.IsEmpty;

        for (int i = 0; i < yTrue.Length; i++)
        {
            double score = yScore[i];
            if (double.IsNaN(score))
            {
                throw new ArgumentException($"yScore[{i}] is NaN; scores must be numbers.", nameof(yScore));
            }

            double weight = weighted ? sampleWeight[i] : 1.0;
            keys[i] = -score;
            points[i].Weight = weight;
            points[i].PositiveWeight = yTrue[i] == posLabel ? weight : 0.0;
        }
    }

    private static double Accumulate(double[] keys, Point[] points)
    {
        int n = keys.Length;
        double truePositives = 0.0;
        double falsePositives = 0.0;
        double previousTrue = 0.0;
        double previousFalse = 0.0;
        double area = 0.0;

        for (int i = 0; i < n; i++)
        {
            truePositives += points[i].PositiveWeight;
            falsePositives += points[i].Weight - points[i].PositiveWeight;

            if (!IsLastOfGroup(keys, i))
            {
                continue;
            }

            area += (falsePositives - previousFalse) * (truePositives + previousTrue) * 0.5;
            previousTrue = truePositives;
            previousFalse = falsePositives;
        }

        RequireBothClassesPresent(truePositives, falsePositives);

        return area / (truePositives * falsePositives);
    }

    private static bool IsLastOfGroup(double[] keys, int i)
    {
        // SonarLint S1244 warns against comparing floating point for exact
        // equality, which is right for arithmetic and wrong here: ties in a
        // score column are bit-identical doubles, and grouping them is the
        // whole point. scikit-learn's _binary_clf_curve locates its own
        // thresholds the same way, with np.diff(y_score) != 0. A tolerance
        // would merge scores that are genuinely distinct and change the
        // curve — the approximate version is the wrong answer here, not a
        // safer one.
#pragma warning disable S1244
        return i == keys.Length - 1 || keys[i] != keys[i + 1];
#pragma warning restore S1244
    }

    private static void RequireBothClassesPresent(double truePositives, double falsePositives)
    {
        // SonarLint S1244 warns against comparing floating point for exact
        // equality, which is right for arithmetic and wrong here: this asks
        // whether anything accumulated at all, not whether two computed
        // quantities are close. Zero true positives or zero false positives
        // means one class is absent from yTrue, which is exactly the case
        // scikit-learn refuses. A tolerance would reject legitimate inputs
        // whose weights are merely small.
#pragma warning disable S1244
        if (truePositives == 0.0 || falsePositives == 0.0)
        {
#pragma warning restore S1244
            // SonarLint S3928 wants this paramName to be nameof()'d against a
            // parameter of the enclosing method, which is right in general and
            // wrong here specifically: yTrue isn't a parameter of this
            // extracted helper, so nameof(yTrue) isn't available, but the
            // literal is not a made-up name either — it is the actual
            // parameter of the public RocAuc.Score/RocAuc.MultiClass call this
            // exception reports back to, exactly as it was before Score was
            // split into Validate/BuildPoints/Accumulate/this method. Dropping
            // ParamName instead would change ArgumentException.Message itself
            // (it appends "(Parameter 'yTrue')" whenever ParamName is set),
            // which is the regression this comment exists to prevent.
// CA2208 reads the same literal the comment above defends and reaches the same
// verdict from the helper's own signature, where yTrue is genuinely not a
// parameter. It is disabled here for the reason spelled out above, not waived.
#pragma warning disable S3928, CA2208
            throw new ArgumentException("Only one class is present in yTrue; ROC AUC is undefined for it.", "yTrue");
#pragma warning restore S3928, CA2208
        }
    }
}
