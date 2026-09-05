using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>
/// The t-tests — is the difference between these two groups more than noise?
/// </summary>
internal static class TTestSample
{
    // Response times in milliseconds, before and after a change, on two
    // independent sets of requests.
    private static readonly double[] Before = [102.0, 98.0, 110.0, 105.0, 99.0, 101.0, 108.0];
    private static readonly double[] After = [95.0, 92.0, 99.0, 91.0, 97.0, 90.0, 94.0, 96.0];

    // The same seven machines measured twice: paired, not independent.
    private static readonly double[] MachineBefore = [102.0, 98.0, 110.0, 105.0, 99.0, 101.0, 108.0];
    private static readonly double[] MachineAfter = [99.0, 96.0, 104.0, 103.0, 95.0, 99.0, 102.0];

    public static void Run()
    {
        Console.WriteLine("t-tests");

        TTestResult welch = TTest.Independent(Before, After);
        Console.WriteLine($"  Welch t               = {Inv.F3(welch.Statistic)}");
        Console.WriteLine($"  Welch p               = {Inv.E3(welch.PValue)}");
        Console.WriteLine($"  Welch df              = {Inv.F3(welch.Df)}");

        TTestResult student = TTest.Independent(
            Before, After, Alternative.TwoSided, Variance.Equal);
        Console.WriteLine($"  Student t             = {Inv.F3(student.Statistic)}");

        TTestResult paired = TTest.Paired(MachineBefore, MachineAfter);
        Console.WriteLine($"  paired t              = {Inv.F3(paired.Statistic)}");

        TTestResult oneSample = TTest.OneSample(After, populationMean: 100.0, Alternative.Less);
        Console.WriteLine($"  one-sample p (less)   = {Inv.E3(oneSample.PValue)}");
    }
}
