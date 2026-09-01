using Lodestar.Conformal;

namespace Lodestar.Sample;

/// <summary>
/// Split conformal prediction — a point prediction becomes an interval, a class becomes a
/// set, and both carry a coverage guarantee that assumes exchangeability.
/// </summary>
internal static class SplitConformalSample
{
    // Nine calibration points: what actually happened, and what the model had said.
    private static readonly double[] Observed = [12.1, 9.4, 15.0, 11.2, 8.8, 13.9, 10.5, 14.2, 9.9];
    private static readonly double[] Predicted = [11.8, 10.1, 14.2, 11.9, 8.2, 13.1, 10.9, 13.4, 10.6];

    // Four classes, four calibration rows and their true labels, then two rows to score.
    private static readonly double[] CalibrationProbabilities =
    [
        0.80, 0.10, 0.05, 0.05,
        0.10, 0.70, 0.15, 0.05,
        0.05, 0.15, 0.75, 0.05,
        0.20, 0.20, 0.10, 0.50,
    ];
    private static readonly int[] CalibrationLabels = [0, 1, 2, 3];
    private static readonly double[] Unseen = [0.62, 0.20, 0.12, 0.06];
    private static readonly double[] Undecided = [0.28, 0.26, 0.24, 0.22];

    public static void Run()
    {
        Console.WriteLine("split conformal prediction");

        double[] residuals = SplitConformal.AbsoluteResiduals(Observed, Predicted);
        double quantile = SplitConformal.Quantile(residuals, alpha: 0.2);
        (double Lower, double Upper) interval = SplitConformal.Interval(11.0, quantile);
        Console.WriteLine($"  residuals             = {Inv.List(residuals)}");
        Console.WriteLine($"  calibrated quantile   = {Inv.F3(quantile)}");
        Console.WriteLine($"  11.0 becomes          = [{Inv.F3(interval.Lower)}, {Inv.F3(interval.Upper)}]");

        double[] scores = SplitConformal.LeastAmbiguousScores(
            CalibrationProbabilities, CalibrationLabels, classCount: 4);
        double classQuantile = SplitConformal.Quantile(scores, alpha: 0.25);
        Console.WriteLine($"  LAC scores            = {Inv.List(scores)}");
        Console.WriteLine($"  LAC quantile          = {Inv.F3(classQuantile)}");
        Console.WriteLine($"  a clear row           = {Describe(Unseen, classQuantile)}");
        // The set is allowed to be empty, and this row is what that looks like.
        Console.WriteLine($"  an undecided row      = {Describe(Undecided, classQuantile)}");

        // The one thing the numbers above cannot say. See docs/guides/conformal.md.
        Console.WriteLine("  coverage holds only if calibration and test data are exchangeable");
        Console.WriteLine();
    }

    private static string Describe(double[] probabilities, double quantile)
    {
        bool[] set = SplitConformal.PredictionSet(probabilities, quantile);
        string classes = string.Join(", ", Enumerable.Range(0, set.Length).Where(i => set[i]));
        return classes.Length == 0 ? "{} (empty, and that is the answer)" : $"{{{classes}}}";
    }
}
