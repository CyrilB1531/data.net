using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>One-way ANOVA — do three groups share one mean?</summary>
internal static class OneWayAnovaSample
{
    private static readonly double[] Morning = [12.0, 14.0, 11.0, 13.0, 15.0];
    private static readonly double[] Afternoon = [16.0, 15.0, 18.0, 17.0, 14.0];
    private static readonly double[] Evening = [21.0, 19.0, 22.0, 20.0, 23.0];

    public static void Run()
    {
        Console.WriteLine("one-way ANOVA");

        TestResult result = OneWayAnova.Test(Morning, Afternoon, Evening);

        Console.WriteLine($"  F                     = {Inv.F3(result.Statistic)}");
        Console.WriteLine($"  p-value               = {Inv.E3(result.PValue)}");
    }
}
