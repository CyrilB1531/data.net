using System.Buffers;

namespace Lodestar.Metrics.Internal;

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
    // The radix loses below ~8 000 and wins above (1.21x at 10 000, 1.32x at a
    // million); docs/guides/performance.md has the table and the machine.
    private const int RadixThreshold = 8_192;

    // 16-bit digits: four passes and a 64 K histogram beat eight passes and a
    // 256-entry one from 16 000 samples up, by 1.17x at a million.
    private const int RadixBits = 16;
    private const int RadixBuckets = 1 << RadixBits;
    private const ulong RadixMask = RadixBuckets - 1;
    private const ulong SignBit = 0x8000000000000000UL;

    private struct Point
    {
        public double Weight;
        public double PositiveWeight;
    }

    /// <summary>
    /// The four buffers one ROC curve needs, rented once and reused across
    /// curves. Going parallel means one of these per worker — never one per
    /// class, which is what the sequential loop's shared buffers already avoid,
    /// and never one per call, which is what <c>keys</c> and <c>points</c> used
    /// to be. At n=100 000 those two are 800 KB and 1.6 MB: large-object heap,
    /// whose allocation takes a lock that eight workers would queue on.
    /// </summary>
    internal sealed class Scratch
    {
        private readonly double[] _keys;
        private readonly Point[] _points;

        // Null below RadixThreshold, where Array.Sort is the faster call and these
        // would be 48 bytes per sample rented for nothing.
        private readonly ulong[]? _codes;
        private readonly ulong[]? _codesAlt;
        private readonly int[]? _order;
        private readonly int[]? _orderAlt;
        private readonly double[]? _sortedKeys;
        private readonly Point[]? _sortedPoints;

        private Scratch(int[] binary, double[] column, double[] keys, Point[] points, int radixLength)
        {
            Binary = binary;
            Column = column;
            _keys = keys;
            _points = points;

            if (radixLength == 0)
            {
                return;
            }

            _codes = ArrayPool<ulong>.Shared.Rent(radixLength);
            _codesAlt = ArrayPool<ulong>.Shared.Rent(radixLength);
            _order = ArrayPool<int>.Shared.Rent(radixLength);
            _orderAlt = ArrayPool<int>.Shared.Rent(radixLength);
            _sortedKeys = ArrayPool<double>.Shared.Rent(radixLength);
            _sortedPoints = ArrayPool<Point>.Shared.Rent(radixLength);
        }

        internal int[] Binary { get; }

        internal double[] Column { get; }

        internal static Scratch Rent(int minimumLength)
        {
            int length = Math.Max(1, minimumLength);
            return new Scratch(
                ArrayPool<int>.Shared.Rent(length),
                ArrayPool<double>.Shared.Rent(length),
                ArrayPool<double>.Shared.Rent(length),
                ArrayPool<Point>.Shared.Rent(length),
                length >= RadixThreshold ? length : 0);
        }

        internal void Return()
        {
            ArrayPool<int>.Shared.Return(Binary);
            ArrayPool<double>.Shared.Return(Column);
            ArrayPool<double>.Shared.Return(_keys);
            ArrayPool<Point>.Shared.Return(_points);

            if (_codes is null)
            {
                return;
            }

            ArrayPool<ulong>.Shared.Return(_codes);
            ArrayPool<ulong>.Shared.Return(_codesAlt!);
            ArrayPool<int>.Shared.Return(_order!);
            ArrayPool<int>.Shared.Return(_orderAlt!);
            ArrayPool<double>.Shared.Return(_sortedKeys!);
            ArrayPool<Point>.Shared.Return(_sortedPoints!);
        }

        // _keys/_points stay private (Point is private to BinaryRoc). Named
        // Compute, not Score, so it doesn't shadow BinaryRoc.Score (S3218).
        internal double Compute(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int posLabel, ReadOnlySpan<double> sampleWeight)
        {
            int n = Validate(yTrue, yScore, sampleWeight);
            BuildPoints(yTrue, yScore, posLabel, sampleWeight, _keys, _points);

            if (_codes is null || n < RadixThreshold)
            {
                Array.Sort(_keys, _points, 0, n);
                return Accumulate(_keys, _points, n);
            }

            RadixSort(n);
            return Accumulate(_sortedKeys!, _sortedPoints!, n);
        }

        /// <summary>
        /// Orders the first <paramref name="n"/> points by ascending key into
        /// <c>_sortedKeys</c>/<c>_sortedPoints</c>, by radix rather than by comparison.
        /// </summary>
        /// <remarks>
        /// Four LSD passes over 16-bit digits of the order-preserving encoding, carrying
        /// a position rather than the 16-byte point: the pairs moved are 12 bytes, and
        /// the points are gathered once at the end instead of on every pass.
        /// </remarks>
        private void RadixSort(int n)
        {
            ulong[] codes = _codes!;
            ulong[] codesAlt = _codesAlt!;
            int[] order = _order!;
            int[] orderAlt = _orderAlt!;

            for (int i = 0; i < n; i++)
            {
                codes[i] = Encode(_keys[i]);
                order[i] = i;
            }

            int[] histogram = ArrayPool<int>.Shared.Rent(RadixBuckets);
            try
            {
                for (int shift = 0; shift < 64; shift += RadixBits)
                {
                    if (!Pass(codes, order, codesAlt, orderAlt, n, shift, histogram))
                    {
                        continue;
                    }

                    (codes, codesAlt) = (codesAlt, codes);
                    (order, orderAlt) = (orderAlt, order);
                }
            }
            finally
            {
                ArrayPool<int>.Shared.Return(histogram);
            }

            double[] sortedKeys = _sortedKeys!;
            Point[] sortedPoints = _sortedPoints!;
            for (int i = 0; i < n; i++)
            {
                int source = order[i];
                sortedKeys[i] = _keys[source];
                sortedPoints[i] = _points[source];
            }
        }

        /// <summary>One counting pass; false when the digit is constant and the pass would only copy.</summary>
        private static bool Pass(
            ulong[] codes, int[] order, ulong[] codesOut, int[] orderOut, int n, int shift, int[] histogram)
        {
            Array.Clear(histogram, 0, RadixBuckets);
            for (int i = 0; i < n; i++)
            {
                histogram[(int)((codes[i] >> shift) & RadixMask)]++;
            }

            // Scores routinely share an exponent, which leaves whole digits constant.
            // Skipping those passes is most of what makes four passes cheap.
            if (histogram[(int)((codes[0] >> shift) & RadixMask)] == n)
            {
                return false;
            }

            int total = 0;
            for (int bucket = 0; bucket < RadixBuckets; bucket++)
            {
                int count = histogram[bucket];
                histogram[bucket] = total;
                total += count;
            }

            for (int i = 0; i < n; i++)
            {
                int destination = histogram[(int)((codes[i] >> shift) & RadixMask)]++;
                codesOut[destination] = codes[i];
                orderOut[destination] = order[i];
            }

            return true;
        }

        /// <summary>
        /// Maps a non-NaN <see cref="double"/> onto a <see cref="ulong"/> whose unsigned
        /// order is the double's own order.
        /// </summary>
        /// <remarks>
        /// Negatives invert entirely, positives flip only the sign bit — the standard
        /// transform. NaN is refused by <c>BuildPoints</c> before it can reach here, which
        /// is what makes a total order available at all.
        /// </remarks>
        private static ulong Encode(double value)
        {
            ulong bits = (ulong)BitConverter.DoubleToInt64Bits(value);
            return (bits & SignBit) != 0 ? ~bits : bits | SignBit;
        }

        // These five — Validate, BuildPoints, Accumulate, IsLastOfGroup,
        // RequireBothClassesPresent — are reachable only from Scratch (S3398).
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

        private static double Accumulate(double[] keys, Point[] points, int n)
        {
            double truePositives = 0.0;
            double falsePositives = 0.0;
            double previousTrue = 0.0;
            double previousFalse = 0.0;
            double area = 0.0;

            for (int i = 0; i < n; i++)
            {
                truePositives += points[i].PositiveWeight;
                falsePositives += points[i].Weight - points[i].PositiveWeight;

                if (!IsLastOfGroup(keys, i, n))
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

        private static bool IsLastOfGroup(double[] keys, int i, int n)
        {
            // SonarLint S1244 warns against comparing floating point for exact
            // equality, which is right for arithmetic and wrong here: ties in a
            // score column are bit-identical doubles, and grouping them is the
            // whole point. scikit-learn's _binary_clf_curve locates its own
            // thresholds the same way — nonzero(diff(y_score)) at
            // sklearn/metrics/_ranking.py:917, scikit-learn 1.9.0. A tolerance
            // would merge scores that are genuinely distinct and change the
            // curve — the approximate version is the wrong answer here, not a
            // safer one.
#pragma warning disable S1244
            return i == n - 1 || keys[i] != keys[i + 1];
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
                // CA2208 reads the same literal and reaches the same verdict from the
                // helper's own signature, where yTrue is genuinely not a parameter. It
                // is disabled for the reason spelled out above, not waived.
#pragma warning disable S3928, CA2208
                throw new ArgumentException("Only one class is present in yTrue; ROC AUC is undefined for it.", "yTrue");
#pragma warning restore S3928, CA2208
            }
        }
    }

    public static double Score(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int posLabel, ReadOnlySpan<double> sampleWeight)
    {
        Scratch scratch = Scratch.Rent(yTrue.Length);
        try
        {
            return scratch.Compute(yTrue, yScore, posLabel, sampleWeight);
        }
        finally
        {
            scratch.Return();
        }
    }

    public static double Score(
        ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int posLabel, ReadOnlySpan<double> sampleWeight,
        Scratch scratch) =>
        scratch.Compute(yTrue, yScore, posLabel, sampleWeight);
}
