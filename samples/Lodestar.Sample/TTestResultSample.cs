using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>
/// <see cref="TTestResult"/> — a t-test's statistic, p-value, degrees of freedom
/// and the confidence interval it can produce for the difference it measured.
/// </summary>
internal static class TTestResultSample
{
    private static readonly double[] Sample = [12.1, 9.4, 15.0, 11.2, 8.8, 13.9, 10.5];

    public static void Run()
    {
        Console.WriteLine("TTestResult");

        TTestResult result = TTest.OneSample(Sample, populationMean: 10.0);
        (double low, double high) = result.ConfidenceInterval(0.95);

        Console.WriteLine($"  statistic             = {Inv.F3(result.Statistic)}");
        Console.WriteLine($"  p-value               = {Inv.F3(result.PValue)}");
        Console.WriteLine($"  degrees of freedom    = {Inv.F3(result.Df)}");
        Console.WriteLine($"  95 % interval         = [{Inv.F3(low)}, {Inv.F3(high)}]");

        // A one-sided test spends its whole error budget on one side, so the
        // other bound is infinite rather than merely larger.
        (double oneLow, double oneHigh) = TTest
            .OneSample(Sample, populationMean: 10.0, Alternative.Greater)
            .ConfidenceInterval(0.95);
        Console.WriteLine($"  one-sided interval    = [{Inv.F3(oneLow)}, {oneHigh}]");
    }
}
