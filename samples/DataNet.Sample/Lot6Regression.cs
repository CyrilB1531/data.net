using System.Globalization;
using DataNet.Metrics;

namespace DataNet.Sample;

/// <summary>
/// Lot 6 — DataNet.Metrics, the regression half of the scikit-learn-compatible
/// evaluation surface (issue #92).
/// </summary>
/// <remarks>
/// Every number here is printed through <see cref="CultureInfo.InvariantCulture"/>.
/// A sample that prints <c>0,250</c> on a French machine and <c>0.250</c> on an
/// English one is not a sample of anything; the guides quote this output.
/// </remarks>
internal static class Lot6Regression
{
    // scikit-learn's own doctest values, so a reader can paste the Python call
    // beside the C# one and compare.
    private static readonly double[] Truth = [3.0, -0.5, 2.0, 7.0];
    private static readonly double[] Predicted = [2.5, 0.0, 2.0, 8.0];

    // The log family refuses a target at or below -1 on either side, exactly as
    // scikit-learn does, so it needs a target of its own.
    private static readonly double[] PositiveTruth = [3.0, 5.0, 2.5, 7.0];
    private static readonly double[] PositivePredicted = [2.5, 5.0, 4.0, 8.0];

    // Three samples, two outputs, row-major: output 0 then output 1, sample by
    // sample. `outputCount` declares the shape — a span can't carry a 2-D array.
    private static readonly double[] WideTruth = [0.5, 1.0, -1.0, 1.0, 7.0, -6.0];
    private static readonly double[] WidePredicted = [0.0, 2.0, -1.0, 2.0, 8.0, -5.0];

    public static void Run()
    {
        Console.WriteLine("lot 6 — regression metrics");

        Errors();
        LogErrors();
        Explained();
        MultiOutput();
    }

    /// <summary>The error family, all on one prediction so the numbers compare.</summary>
    private static void Errors()
    {
        Console.WriteLine($"  MeanSquaredError      = {F3(MeanSquaredError.Score(Truth, Predicted))}");
        Console.WriteLine($"  RootMeanSquaredError  = {F3(RootMeanSquaredError.Score(Truth, Predicted))}");
        Console.WriteLine($"  MeanAbsoluteError     = {F3(MeanAbsoluteError.Score(Truth, Predicted))}");
        Console.WriteLine($"  MedianAbsoluteError   = {F3(MedianAbsoluteError.Score(Truth, Predicted))}");
        Console.WriteLine($"  MeanAbsolutePctError  = {F3(MeanAbsolutePercentageError.Score(Truth, Predicted))}");

        // MaxError is the one metric in the lot with no sampleWeight and no
        // multioutput: max_error has neither, and a worst case is not an average.
        Console.WriteLine($"  MaxError              = {F3(MaxError.Score(Truth, Predicted))}");

        // alpha = 0.5 is half the mean absolute error, which is the identity that
        // ties the quantile loss to a metric already above it.
        Console.WriteLine($"  PinballLoss (α=0.5)   = {F3(PinballLoss.Score(Truth, Predicted))}"
            + $" — half of MAE, {F3(MeanAbsoluteError.Score(Truth, Predicted) / 2.0)}");
        Console.WriteLine($"  PinballLoss (α=0.9)   = {F3(PinballLoss.Score(Truth, Predicted, alpha: 0.9))}"
            + " — under-prediction now costs nine times more");

        // A uniform weight happens to agree with the ordinary median here; see
        // MedianAbsoluteError's own remarks for why that is not a rule.
        double[] uniform = [1.0, 1.0, 1.0, 1.0];
        Console.WriteLine($"  MedianAbsoluteError   = "
            + $"{F3(MedianAbsoluteError.Score(Truth, Predicted, sampleWeight: uniform))} under uniform weights");
        Console.WriteLine();
    }

    /// <summary>The two log-space errors, on a target every value of which is above −1.</summary>
    private static void LogErrors()
    {
        Console.WriteLine($"  MeanSquaredLogError   = {F5(MeanSquaredLogError.Score(PositiveTruth, PositivePredicted))}");
        Console.WriteLine($"  RootMeanSquaredLogErr = {F5(RootMeanSquaredLogError.Score(PositiveTruth, PositivePredicted))}");

        try
        {
            double[] belowFloor = [-1.0, 2.0, 3.0, 4.0];
            MeanSquaredLogError.Score(belowFloor, PositivePredicted);
            Console.WriteLine("  a target at -1        = <did not throw, which is a bug>");
        }
        catch (ArgumentException ex)
        {
            // scikit-learn raises ValueError here; the message additionally names
            // which of the two sides carried the offending value.
            Console.WriteLine($"  a target at -1        = {ex.Message}");
        }

        Console.WriteLine();
    }

    /// <summary>The two goodness-of-fit scores, and the knob they share.</summary>
    private static void Explained()
    {
        Console.WriteLine($"  R2                    = {F3(R2.Score(Truth, Predicted))}");
        Console.WriteLine($"  ExplainedVariance     = {F3(ExplainedVariance.Score(Truth, Predicted))}");

        // A flat truth is force_finite's case: 1 when the prediction is perfect,
        // 0 otherwise, or unclamped nan/-inf when asked for.
        double[] flatTruth = [2.0, 2.0, 2.0];
        double[] exact = [2.0, 2.0, 2.0];
        double[] wrong = [1.0, 2.0, 3.0];
        Console.WriteLine($"  R2 on a flat truth    = {F3(R2.Score(flatTruth, exact))} perfect,"
            + $" {F3(R2.Score(flatTruth, wrong))} imperfect");
        Console.WriteLine($"  the same, unclamped   = {F3(R2.Score(flatTruth, exact, forceFinite: false))} perfect,"
            + $" {F3(R2.Score(flatTruth, wrong, forceFinite: false))} imperfect");

        // Fewer than two samples is a different question, and it has its own
        // knob: scikit-learn returns nan under either setting of force_finite.
        double[] single = [3.0];
        double[] guess = [5.0];
        Console.WriteLine($"  R2 on one sample      = {F3(R2.Score(single, guess))}"
            + $" (ZeroDivision.Zero gives {F3(R2.Score(single, guess, zeroDivision: ZeroDivision.Zero))})");

        // explained_variance_score has no such case to route, so it takes no
        // ZeroDivision at all — and answers 1.0 here rather than nan.
        Console.WriteLine($"  ExplainedVariance     = {F3(ExplainedVariance.Score(single, guess))} on that same sample");
        Console.WriteLine();
    }

    /// <summary>
    /// <c>multioutput</c> is the choice of method, not an enum: see
    /// <c>docs/decisions/0021</c>.
    /// </summary>
    private static void MultiOutput()
    {
        Console.WriteLine("  three samples, two outputs, row-major");
        Console.WriteLine($"    MSE uniform average = {F3(MeanSquaredError.Score(WideTruth, WidePredicted, outputCount: 2))}");
        Console.WriteLine($"    MSE raw values      = {Format(MeanSquaredError.PerOutput(WideTruth, WidePredicted, outputCount: 2))}");

        // multioutput=[…] is a span of weights rather than a member of an enum,
        // for the same reason raw_values is a method: neither fits one.
        double[] outputWeights = [0.3, 0.7];
        Console.WriteLine($"    MSE weighted [.3,.7]= "
            + $"{F3(MeanSquaredError.Score(WideTruth, WidePredicted, outputCount: 2, outputWeights: outputWeights))}");

        Console.WriteLine($"    MAE raw values      = {Format(MeanAbsoluteError.PerOutput(WideTruth, WidePredicted, outputCount: 2))}");
        Console.WriteLine($"    MedianAE raw values = {Format(MedianAbsoluteError.PerOutput(WideTruth, WidePredicted, outputCount: 2))}");
        Console.WriteLine($"    MAPE raw values     = {Format(MeanAbsolutePercentageError.PerOutput(WideTruth, WidePredicted, outputCount: 2))}");
        Console.WriteLine($"    Pinball raw values  = {Format(PinballLoss.PerOutput(WideTruth, WidePredicted, alpha: 0.9, outputCount: 2))}");
        Console.WriteLine($"    RMSE raw values     = {Format(RootMeanSquaredError.PerOutput(WideTruth, WidePredicted, outputCount: 2))}");
        Console.WriteLine($"    MSLE raw values     = {Format(MeanSquaredLogError.PerOutput(PositiveTruth, PositivePredicted, outputCount: 2))}");
        Console.WriteLine($"    RMSLE raw values    = {Format(RootMeanSquaredLogError.PerOutput(PositiveTruth, PositivePredicted, outputCount: 2))}");

        // variance_weighted applies to two of the eleven metrics, so it's a method
        // on just those two — an invalid call fails to compile, not at run time.
        Console.WriteLine($"    R2 raw values       = {Format(R2.PerOutput(WideTruth, WidePredicted, outputCount: 2))}");
        Console.WriteLine($"    R2 variance-weighted= {F3(R2.VarianceWeighted(WideTruth, WidePredicted, outputCount: 2))}");
        Console.WriteLine($"    EV raw values       = {Format(ExplainedVariance.PerOutput(WideTruth, WidePredicted, outputCount: 2))}");
        Console.WriteLine($"    EV variance-weighted= {F3(ExplainedVariance.VarianceWeighted(WideTruth, WidePredicted, outputCount: 2))}");
        Console.WriteLine();
    }

    private static string F3(double value) => value.ToString("F3", CultureInfo.InvariantCulture);

    private static string F5(double value) => value.ToString("F5", CultureInfo.InvariantCulture);

    private static string Format(double[] values) =>
        "[" + string.Join(", ", values.Select(F3)) + "]";
}
