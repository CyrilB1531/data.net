namespace Lodestar.Metrics.Internal;

/// <summary>
/// The label-matrix shape the classification losses share: two boolean blocks of the
/// same size, and one weight per row rather than per value.
/// </summary>
internal static class Multilabel
{
    /// <summary>Checks the shape and returns the row count.</summary>
    /// <exception cref="ArgumentException">The shapes disagree, the input is empty, or the weights do not match the row count.</exception>
    public static int Validate(
        ReadOnlySpan<bool> yTrue, ReadOnlySpan<bool> yPred, int labelCount, ReadOnlySpan<double> sampleWeight)
    {
        if (labelCount < 1)
        {
            throw new ArgumentException(
                $"yTrue holds {labelCount} labels; a label matrix needs at least 1.", nameof(labelCount));
        }

        if (yTrue.Length != yPred.Length)
        {
            throw new ArgumentException(
                $"yTrue holds {yTrue.Length} values and yPred holds {yPred.Length}; they must agree.",
                nameof(yPred));
        }

        if (yTrue.Length == 0 || yTrue.Length % labelCount != 0)
        {
            throw new ArgumentException(
                $"yTrue holds {yTrue.Length} values, which is not a whole number of rows of {labelCount}.",
                nameof(yTrue));
        }

        int rows = yTrue.Length / labelCount;
        if (!sampleWeight.IsEmpty && sampleWeight.Length != rows)
        {
            throw new ArgumentException(
                $"sampleWeight holds {sampleWeight.Length} values for {rows} samples; they must agree.",
                nameof(sampleWeight));
        }

        return rows;
    }
}
