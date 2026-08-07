namespace DataNet.Metrics;

/// <summary>
/// Thrown when a metric is undefined and <see cref="ZeroDivision.Throw"/> was
/// requested — the counterpart of scikit-learn's <c>UndefinedMetricWarning</c>.
/// </summary>
public sealed class UndefinedMetricException : InvalidOperationException
{
    /// <summary>Creates the exception with a default message.</summary>
    public UndefinedMetricException()
        : base("The metric is undefined: its denominator is zero.")
    {
    }

    /// <summary>Creates the exception with the given message.</summary>
    /// <param name="message">A message describing which metric is undefined.</param>
    public UndefinedMetricException(string message)
        : base(message)
    {
    }

    /// <summary>Creates the exception with the given message and inner exception.</summary>
    /// <param name="message">A message describing which metric is undefined.</param>
    /// <param name="innerException">The cause.</param>
    public UndefinedMetricException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
