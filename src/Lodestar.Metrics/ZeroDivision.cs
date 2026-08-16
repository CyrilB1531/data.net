namespace Lodestar.Metrics;

/// <summary>
/// What a metric returns when its denominator is zero — the equivalent of
/// scikit-learn's <c>zero_division=</c> parameter.
/// </summary>
/// <remarks>
/// scikit-learn's default returns <c>0.0</c> <em>and</em> emits an
/// <c>UndefinedMetricWarning</c>. <see cref="Zero"/> reproduces the value, which
/// is what parity requires; <see cref="Throw"/> is the opt-in equivalent of the
/// warning, for callers who would rather be told than get a silent zero.
/// </remarks>
public enum ZeroDivision
{
    /// <summary>Return <c>0.0</c> — scikit-learn's default value.</summary>
    Zero,

    /// <summary>Return <c>1.0</c> (<c>zero_division=1</c>).</summary>
    One,

    /// <summary>Return <see cref="double.NaN"/> (<c>zero_division=np.nan</c>).</summary>
    NaN,

    /// <summary>Throw <see cref="UndefinedMetricException"/>. No scikit-learn equivalent.</summary>
    Throw,
}
