#if NET5_0_OR_GREATER
using System.Numerics;
#endif

namespace DataNet.Metrics.Internal;

/// <summary>
/// A running sum that keeps the low-order bits a sequential <c>+=</c> discards —
/// Neumaier's variant of compensated summation.
/// </summary>
/// <remarks>
/// numpy sums pairwise; a sequential loop can drift past the oracle's 1e-9
/// tolerance, and Neumaier's branch — not Kahan's — removes that failure mode
/// unconditionally. Measured, with the Kahan comparison, in
/// <c>docs/decisions/0033-compensated-sum-is-neumaiers-variant.md</c> (issue #127).
/// </remarks>
internal struct CompensatedSum
{
    private double _sum;
    private double _compensation;

    /// <summary>Adds one term, keeping what the addition rounded off.</summary>
    /// <param name="value">The term to add.</param>
    public void Add(double value)
    {
        double total = _sum + value;
        _compensation += Math.Abs(_sum) >= Math.Abs(value)
            ? (_sum - total) + value
            : (value - total) + _sum;
        _sum = total;
    }

    /// <summary>The sum, with the accumulated rounding folded back in.</summary>
    public readonly double Value => _sum + _compensation;
}

#if NET5_0_OR_GREATER
/// <summary>
/// <see cref="CompensatedSum"/> per SIMD lane — <see cref="Vector{T}"/> on
/// <c>net10.0</c> only; see <c>docs/decisions/0001-target-framework.md</c>.
/// </summary>
/// <remarks>
/// Each lane is Neumaier-exact on its own terms; <see cref="Reduce"/> combines
/// lanes in a different order than a scalar loop, so the two are not
/// guaranteed bit-identical — both pass the oracle's 1e-9 comparison. See
/// <c>docs/decisions/0033-compensated-sum-is-neumaiers-variant.md</c>.
/// </remarks>
internal struct VectorCompensatedSum
{
    private Vector<double> _sum;
    private Vector<double> _compensation;

    /// <summary>Adds one term per lane, keeping what each lane's addition rounded off.</summary>
    /// <param name="value">One term per lane.</param>
    public void Add(Vector<double> value)
    {
        Vector<double> total = _sum + value;
        Vector<double> useSum = Vector.GreaterThanOrEqual<double>(Vector.Abs(_sum), Vector.Abs(value));
        Vector<double> viaSum = (_sum - total) + value;
        Vector<double> viaValue = (value - total) + _sum;
        _compensation += Vector.ConditionalSelect(useSum, viaSum, viaValue);
        _sum = total;
    }

    /// <summary>
    /// Rounds each lane's own <c>_sum + _compensation</c> to one <see cref="double"/>
    /// first, then Neumaier-adds those <see cref="Vector{T}.Count"/> doubles into
    /// one <see cref="CompensatedSum"/> — a second, separate compensated
    /// combination on top of what each lane already did for itself, not a
    /// continuation of it.
    /// </summary>
    public readonly CompensatedSum Reduce()
    {
        CompensatedSum result = default;
        for (int lane = 0; lane < Vector<double>.Count; lane++)
        {
            result.Add(_sum[lane] + _compensation[lane]);
        }
        return result;
    }
}
#endif
