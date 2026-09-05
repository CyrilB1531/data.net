using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>
/// <see cref="TestResult"/> — the shape eight of the ten families return, and
/// the only two numbers most of them have to give.
/// </summary>
internal static class TestResultSample
{
    private static readonly double[] Left = [7.0, 3.0, 6.0, 2.0, 8.0, 5.0];
    private static readonly double[] Right = [9.0, 12.0, 8.0, 11.0, 15.0, 10.0];

    public static void Run()
    {
        Console.WriteLine("TestResult");

        TestResult result = MannWhitney.Test(Left, Right);

        Console.WriteLine($"  statistic             = {Inv.F3(result.Statistic)}");
        Console.WriteLine($"  p-value               = {Inv.F3(result.PValue)}");
    }
}
