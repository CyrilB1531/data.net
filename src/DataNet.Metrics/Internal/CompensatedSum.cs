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
