using System.Buffers;
using Lodestar.Metrics.Internal;

namespace Lodestar.Metrics;

/// <summary>
/// The reliability curve as plot data — the equivalent of
/// <c>sklearn.calibration.calibration_curve</c>.
/// </summary>
/// <remarks>
/// <c>sklearn.calibration</c>, not <c>sklearn.metrics</c>: the one member of the family
/// that lives in the other module. Both arrays share a length, and it is <b>not</b>
/// <c>nBins</c> — an empty bin is dropped, so it follows the data (#286).
/// </remarks>
public sealed class CalibrationCurve
{
    private CalibrationCurve(double[] probTrue, double[] probPred)
    {
        ProbTrue = probTrue;
        ProbPred = probPred;
    }

    /// <summary>The share of positives in each non-empty bin.</summary>
    public IReadOnlyList<double> ProbTrue { get; }

    /// <summary>The mean predicted probability in each non-empty bin, same length as <see cref="ProbTrue"/>.</summary>
    public IReadOnlyList<double> ProbPred { get; }

    /// <summary>Draws the curve — <c>calibration_curve(y_true, y_prob, pos_label=…, n_bins=…, strategy=…)</c>.</summary>
    /// <param name="yTrue">The true labels, one per sample; at most two distinct values.</param>
    /// <param name="yProb">A probability per sample, each within <c>[0, 1]</c>.</param>
    /// <param name="posLabel">The label counted as positive. Explicit here where the reference infers it.</param>
    /// <param name="nBins">How many bins to cut <c>[0, 1]</c> into. The result may be shorter.</param>
    /// <param name="strategy">Where the bin edges come from.</param>
    /// <exception cref="ArgumentException">The inputs disagree in length, are empty, carry a probability outside <c>[0, 1]</c>, or name more than two classes.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nBins"/> is below 1.</exception>
    public static CalibrationCurve Compute(
        ReadOnlySpan<int> yTrue,
        ReadOnlySpan<double> yProb,
        int posLabel = 1,
        int nBins = 5,
        BinStrategy strategy = BinStrategy.Uniform)
    {
        Validate(yTrue, yProb, nBins);

        double[] edges = Edges(yProb, nBins, strategy);
        double[] sums = new double[nBins];
        double[] positives = new double[nBins];
        int[] totals = new int[nBins];

        for (int i = 0; i < yProb.Length; i++)
        {
            // searchsorted over the interior edges, which puts an exact edge in the
            // lower bin and 1.0 in the last.
            int bin = LeftmostAtLeast(edges, yProb[i], nBins);
            sums[bin] += yProb[i];
            positives[bin] += yTrue[i] == posLabel ? 1.0 : 0.0;
            totals[bin]++;
        }

        int kept = totals.Count(total => total != 0);
        var probTrue = new double[kept];
        var probPred = new double[kept];
        int at = 0;
        for (int bin = 0; bin < nBins; bin++)
        {
            if (totals[bin] == 0)
            {
                continue;
            }
            probTrue[at] = positives[bin] / totals[bin];
            probPred[at] = sums[bin] / totals[bin];
            at++;
        }

        return new CalibrationCurve(probTrue, probPred);
    }

    private static void Validate(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yProb, int nBins)
    {
        if (nBins < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(nBins), nBins, "nBins must be >= 1.");
        }
        if (yTrue.Length != yProb.Length || yTrue.Length == 0)
        {
            throw new ArgumentException(
                $"y_true and y_prob must be the same non-empty length; got {yTrue.Length} and {yProb.Length}.");
        }

        foreach (double probability in yProb)
        {
            if (!(probability >= 0.0 && probability <= 1.0))
            {
                throw new ArgumentException("y_prob has values outside [0, 1].");
            }
        }

        int first = yTrue[0];
        int second = first;
        foreach (int label in yTrue)
        {
            if (label != first && label != second)
            {
                if (second != first)
                {
                    throw new ArgumentException(
                        "Only binary classification is supported: y_true names more than two labels.");
                }
                second = label;
            }
        }
    }

    /// <summary>The <c>nBins + 1</c> edges, from the interval or from the data.</summary>
    private static double[] Edges(ReadOnlySpan<double> yProb, int nBins, BinStrategy strategy)
    {
        var edges = new double[nBins + 1];
        if (strategy == BinStrategy.Uniform)
        {
            for (int i = 0; i <= nBins; i++)
            {
                edges[i] = (double)i / nBins;
            }

            return edges;
        }

        double[] rented = ArrayPool<double>.Shared.Rent(yProb.Length);
        try
        {
            Span<double> sorted = rented.AsSpan(0, yProb.Length);
            yProb.CopyTo(sorted);

            // Array.Sort over the rented range rather than Span<T>.Sort, which
            // netstandard2.0 does not carry.
            Array.Sort(rented, 0, yProb.Length);
            for (int i = 0; i <= nBins; i++)
            {
                edges[i] = Percentile(sorted, (double)i / nBins);
            }

            return edges;
        }
        finally
        {
            ArrayPool<double>.Shared.Return(rented);
        }
    }

    /// <summary>The linear-interpolation percentile <c>np.percentile</c> computes by default.</summary>
    /// <remarks>
    /// Not <see cref="WeightedPercentile"/>: the reference reaches for <c>np.percentile</c>
    /// here, whose rule is a linear interpolation between the two neighbouring order
    /// statistics, where the weighted one this package already carries interpolates
    /// differently (decision 0024). Reusing it would disagree in the third decimal.
    /// </remarks>
    private static double Percentile(ReadOnlySpan<double> sorted, double fraction)
    {
        double position = fraction * (sorted.Length - 1);
        int below = (int)Math.Floor(position);
        int above = (int)Math.Ceiling(position);
        return below == above ? sorted[below] : sorted[below] + ((position - below) * (sorted[above] - sorted[below]));
    }

    /// <summary>The bin an interior-edge <c>searchsorted</c> puts a probability in.</summary>
    private static int LeftmostAtLeast(double[] edges, double probability, int nBins)
    {
        // The reference searches edges[1..^1], so the answer is bounded by nBins - 1 and
        // 1.0 lands in the last bin rather than off the end.
        for (int bin = 1; bin < nBins; bin++)
        {
            if (edges[bin] >= probability)
            {
                return bin - 1;
            }
        }

        return nBins - 1;
    }
}
