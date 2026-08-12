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
/// Neumaier rather than Kahan: Kahan's correction is lost whenever the incoming term
/// is larger than the running total, which is exactly this shape — an accumulator
/// starting at zero taking terms near 1e9. The branch below is what fixes that, and
/// is the only difference between the two.
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
