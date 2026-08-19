namespace Lodestar.Metrics;

/// <summary>
/// The area under a curve given as points — the equivalent of
/// <c>sklearn.metrics.auc</c>.
/// </summary>
/// <remarks>
/// Over <see cref="RocCurve"/>'s output it gives <see cref="RocAuc.Score"/> exactly, an
/// invariant a test holds rather than an oracle. Deliberately not how
/// <see cref="AveragePrecision"/> reads a precision-recall curve, where it reads high.
/// </remarks>
public static class Auc
{
    /// <summary>The area under the curve through <paramref name="x"/> and <paramref name="y"/> — <c>auc(x, y)</c>.</summary>
    /// <param name="x">The x coordinates, monotonic in either direction.</param>
    /// <param name="y">The y coordinates, one per x.</param>
    /// <returns>The signed area, taken left to right: a curve given right to left gives the same magnitude, as the reference's does.</returns>
    /// <exception cref="ArgumentException">The lengths disagree, fewer than two points are given, or <paramref name="x"/> is not monotonic.</exception>
    public static double Trapezoid(ReadOnlySpan<double> x, ReadOnlySpan<double> y)
    {
        if (x.Length != y.Length)
        {
            throw new ArgumentException(
                $"x has {x.Length} points and y has {y.Length}; they must agree.", nameof(y));
        }

        if (x.Length < 2)
        {
            throw new ArgumentException(
                $"At least 2 points are needed to compute the area, but x holds {x.Length}.", nameof(x));
        }

        int direction = Direction(x);
        double area = 0.0;
        for (int i = 1; i < x.Length; i++)
        {
            area += (x[i] - x[i - 1]) * (y[i] + y[i - 1]) * 0.5;
        }

        return direction * area;
    }

    /// <summary>Which way <paramref name="x"/> runs, refusing a sequence that turns.</summary>
    private static int Direction(ReadOnlySpan<double> x)
    {
        bool rising = true;
        bool falling = true;
        for (int i = 1; i < x.Length; i++)
        {
            if (x[i] < x[i - 1])
            {
                rising = false;
            }

            if (x[i] > x[i - 1])
            {
                falling = false;
            }
        }

        if (!rising && !falling)
        {
            throw new ArgumentException("x is neither increasing nor decreasing.", nameof(x));
        }

        return rising ? 1 : -1;
    }
}
