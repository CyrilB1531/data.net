namespace Lodestar.Metrics.Internal;

/// <summary>
/// The cumulative counts every binary curve is built from — scikit-learn's
/// <c>_binary_clf_curve</c>.
/// </summary>
/// <remarks>
/// Samples descend by score and equal scores are consumed as one group, which is what
/// makes ties come out the same as the reference's. <see cref="BinaryRoc"/> walks the
/// same points and accumulates an area instead of keeping them.
/// </remarks>
internal static class ClassifierCurve
{
    /// <summary>One point per distinct score: the weight above that threshold, split by class.</summary>
    internal readonly struct Points(double[] truePositives, double[] falsePositives, double[] thresholds)
    {
        public double[] TruePositives { get; } = truePositives;

        public double[] FalsePositives { get; } = falsePositives;

        public double[] Thresholds { get; } = thresholds;

        public int Count => Thresholds.Length;

        public double PositiveTotal => TruePositives[^1];

        public double NegativeTotal => FalsePositives[^1];
    }

    /// <summary>Builds the points, descending by score.</summary>
    /// <exception cref="ArgumentException">The inputs disagree in length, are empty, or hold a NaN score.</exception>
    public static Points Build(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int posLabel, ReadOnlySpan<double> sampleWeight)
    {
        int n = Validate(yTrue, yScore, sampleWeight);

        var order = new int[n];
        var keys = new double[n];
        for (int i = 0; i < n; i++)
        {
            order[i] = i;
            keys[i] = -yScore[i];
        }

        Array.Sort(keys, order);

        var truePositives = new List<double>();
        var falsePositives = new List<double>();
        var thresholds = new List<double>();
        double tp = 0.0;
        double fp = 0.0;

        for (int i = 0; i < n; i++)
        {
            int at = order[i];
            double weight = sampleWeight.IsEmpty ? 1.0 : sampleWeight[at];
            if (yTrue[at] == posLabel)
            {
                tp += weight;
            }
            else
            {
                fp += weight;
            }

            // S1244: whether this is the last of a tied group, which is what decides
            // where a threshold sits. Equal scores are bit-identical, and a tolerance
            // would merge scores the reference keeps apart.
#pragma warning disable S1244
            if (i + 1 < n && keys[i] == keys[i + 1])
#pragma warning restore S1244
            {
                continue;
            }

            truePositives.Add(tp);
            falsePositives.Add(fp);
            thresholds.Add(yScore[at]);
        }

        return new Points([.. truePositives], [.. falsePositives], [.. thresholds]);
    }

    /// <summary>
    /// Which points survive <c>drop_intermediate</c>: the ends, and any point that
    /// turns the curve.
    /// </summary>
    /// <remarks>
    /// A run of collinear points draws the same curve as its two endpoints, so the
    /// reference drops the middle of one — <c>np.where(np.diff(…, 2))</c> over the two
    /// counts. Only points strictly inside the array are ever dropped.
    /// </remarks>
    public static bool[] Keep(double[] first, double[] second, bool dropIntermediate)
    {
        int n = first.Length;
        var keep = new bool[n];
        if (!dropIntermediate || n <= 2)
        {
            // Array.Fill is not on netstandard2.0, and a loop needs no polyfill.
            for (int i = 0; i < n; i++)
            {
                keep[i] = true;
            }

            return keep;
        }

        keep[0] = true;
        keep[n - 1] = true;
        for (int i = 1; i < n - 1; i++)
        {
            double firstBend = first[i + 1] - (2.0 * first[i]) + first[i - 1];
            double secondBend = second[i + 1] - (2.0 * second[i]) + second[i - 1];

            // S1244: whether the second difference vanished, which is exactly what
            // np.diff(…, 2) is tested against — a bend of zero means collinear.
#pragma warning disable S1244
            keep[i] = firstBend != 0.0 || secondBend != 0.0;
#pragma warning restore S1244
        }

        return keep;
    }

    /// <summary>
    /// The other <c>drop_intermediate</c>, which the precision-recall and detection
    /// curves share: drop a point whose true-positive count matches both neighbours.
    /// </summary>
    /// <remarks>
    /// Points with the same count share a recall, so they stack on one vertical line
    /// and only the first and last of a run are worth keeping. Not the same rule as
    /// <see cref="Keep"/>, which the ROC curve uses -- that one drops a point the curve
    /// does not bend at, in either coordinate.
    /// </remarks>
    public static bool[] KeepByCount(double[] counts, bool dropIntermediate)
    {
        int n = counts.Length;
        var keep = new bool[n];
        if (!dropIntermediate || n <= 2)
        {
            for (int i = 0; i < n; i++)
            {
                keep[i] = true;
            }

            return keep;
        }

        keep[0] = true;
        keep[n - 1] = true;
        for (int i = 1; i < n - 1; i++)
        {
            // S1244: whether the count moved at all, which is what np.diff is tested
            // against -- these are accumulated weights, compared for change, not
            // two computations compared for closeness.
#pragma warning disable S1244
            keep[i] = counts[i] != counts[i - 1] || counts[i + 1] != counts[i];
#pragma warning restore S1244
        }

        return keep;
    }

    /// <summary>Copies the kept entries of <paramref name="values"/>, in order.</summary>
    public static double[] Where(double[] values, bool[] keep)
    {
        int kept = 0;
        for (int i = 0; i < keep.Length; i++)
        {
            if (keep[i])
            {
                kept++;
            }
        }

        var result = new double[kept];
        int at = 0;
        for (int i = 0; i < keep.Length; i++)
        {
            if (keep[i])
            {
                result[at] = values[i];
                at++;
            }
        }

        return result;
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
            throw new ArgumentException("yTrue and yScore are empty; there is no curve to draw.", nameof(yTrue));
        }

        if (!sampleWeight.IsEmpty && sampleWeight.Length != n)
        {
            throw new ArgumentException(
                $"sampleWeight has {sampleWeight.Length} entries but there are {n} samples.",
                nameof(sampleWeight));
        }

        for (int i = 0; i < n; i++)
        {
            if (double.IsNaN(yScore[i]))
            {
                throw new ArgumentException($"yScore[{i}] is NaN; scores must be numbers.", nameof(yScore));
            }
        }

        return n;
    }
}
