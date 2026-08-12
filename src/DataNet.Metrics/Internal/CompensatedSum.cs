#if NET5_0_OR_GREATER
using System.Numerics;
#endif

namespace DataNet.Metrics.Internal;

/// <summary>
/// A running sum that keeps the low-order bits a sequential <c>+=</c> discards —
/// Neumaier's variant of compensated summation.
/// </summary>
/// <remarks>
/// <para>
/// numpy sums pairwise, so on an ill-conditioned target — a large offset over a small
/// spread — a sequential loop and <c>numpy.mean</c> separate well past the 1e-9 the
/// oracle corpora compare at. Measured on n = 200 000 around 1e9: the sequential mean
/// lands 2.1e-3 away from the exact one, 21% of the range the data occupies, and R²
/// and explained variance centre on that mean before squaring. Issue #127.
/// </para>
/// <para>
/// Neumaier rather than Kahan: Kahan's correction is a known weakness of the
/// algorithm in general — it is lost whenever an incoming term is larger in
/// magnitude than the running total, because the correction is computed against the
/// sum's own scale and is swamped once a bigger term arrives. Neumaier's branch
/// below removes that failure mode unconditionally, by comparing magnitudes and
/// correcting against whichever operand is larger, and is the only difference
/// between the two algorithms. That failure mode is not, in fact, what happens on
/// the shape this type exists for: measured on <c>IllConditioned()</c> (offset 1e9,
/// spread 1e-2, n = 200 000), plain Kahan and Neumaier both land within about
/// 1e-16 relative of the decimal reference — because after the first addition the
/// running sum near 1e9 dominates every later term, and an incoming term never
/// again exceeds it. Neumaier is still the right choice: it costs nothing extra
/// over Kahan, and its correctness does not depend on the data staying shaped the
/// way it is today.
/// </para>
/// <para>
/// This is not fragile in the way it would be in C: .NET does not reassociate
/// floating-point arithmetic — there is no fast-math switch — so the compiler and the
/// JIT are both required to evaluate <c>(sum - total) + value</c> as written. The
/// compensation cannot be optimized away, and a reader arriving from a language where
/// it can should not "simplify" this.
/// </para>
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
/// <see cref="CompensatedSum"/>, one term per SIMD lane instead of one term
/// total — <see cref="Vector{T}"/> on <c>net10.0</c> only, the same split
/// <c>VectorMath.Dot</c> uses and <c>docs/decisions/0001-target-framework.md</c>
/// records, because the span-based <see cref="Vector{T}"/> constructor is
/// net-only.
/// </summary>
/// <remarks>
/// <para>
/// Each lane runs Neumaier's exact formula independently — <see cref="Add"/>
/// is <see cref="CompensatedSum.Add"/> with every scalar operation replaced by
/// its <see cref="Vector{T}"/> counterpart, <c>Vector.Abs</c> and
/// <c>Vector.ConditionalSelect</c> standing in for <c>Math.Abs</c> and the
/// ternary. No lane ever sees another lane's terms, so within a lane this is
/// exactly as exact as the scalar type it mirrors.
/// </para>
/// <para>
/// What is not the same is the <em>order</em> the terms arrive in: a caller
/// that batches <c>Vector{double}.Count</c> consecutive array elements per
/// <see cref="Add"/> call has lane 0 summing elements 0, W, 2W, … while lane 1
/// sums 1, W+1, 2W+1, … — a different association of the same terms than a
/// sequential scalar loop would use, and <see cref="Reduce"/> combines the
/// lanes in yet another step on top of that. Two mathematically valid
/// summation orders of the same finite-precision terms are not guaranteed to
/// round to the same <see cref="double"/>, so a metric computed this way and
/// the same metric computed by <see cref="CompensatedSum"/> are not
/// guaranteed to be bit-identical — both are Neumaier-compensated and both
/// are correct to the oracle corpus's 1e-9 comparison, but the guarantee of
/// an exact match, not the match itself, is what is withdrawn here. This
/// repository asserts bit-identity elsewhere (<c>Pooler.MeanPoolBatch</c>); it
/// is deliberately not asserted here.
/// </para>
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
