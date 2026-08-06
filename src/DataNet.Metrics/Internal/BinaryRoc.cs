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

        // Negated scores, so an ascending sort walks the curve from the highest
        // score down — and Array.Sort compares doubles natively rather than
        // through a delegate.
        double[] keys = new double[n];
        Point[] points = new Point[n];
        bool weighted = !sampleWeight.IsEmpty;

        for (int i = 0; i < n; i++)
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

        Array.Sort(keys, points);

        double truePositives = 0.0;
        double falsePositives = 0.0;
        double previousTrue = 0.0;
        double previousFalse = 0.0;
        double area = 0.0;

        for (int i = 0; i < n; i++)
        {
            truePositives += points[i].PositiveWeight;
            falsePositives += points[i].Weight - points[i].PositiveWeight;

            bool lastOfGroup = i == n - 1 || keys[i] != keys[i + 1];
            if (!lastOfGroup)
            {
                continue;
            }

            area += (falsePositives - previousFalse) * (truePositives + previousTrue) * 0.5;
            previousTrue = truePositives;
            previousFalse = falsePositives;
        }

        if (truePositives == 0.0 || falsePositives == 0.0)
        {
            throw new ArgumentException(
                "Only one class is present in yTrue; ROC AUC is undefined for it.", nameof(yTrue));
        }

        return area / (truePositives * falsePositives);
    }
}
