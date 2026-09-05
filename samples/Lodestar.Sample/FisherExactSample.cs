using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>Fisher's exact test — right at any sample size, where chi-square needs large cells.</summary>
internal static class FisherExactSample
{
    // Fisher's own tea-tasting table: four cups poured each way, and the taster
    // placed three of each correctly.
    private static readonly int[][] TeaTasting = [[3, 1], [1, 3]];

    public static void Run()
    {
        Console.WriteLine("Fisher's exact test");

        TestResult twoSided = FisherExact.Test(TeaTasting);
        TestResult greater = FisherExact.Test(TeaTasting, Alternative.Greater);

        Console.WriteLine($"  odds ratio            = {Inv.F3(twoSided.Statistic)}");
        Console.WriteLine($"  two-sided p           = {Inv.F3(twoSided.PValue)}");
        Console.WriteLine($"  one-sided p           = {Inv.F3(greater.PValue)}");
    }
}
