using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>The Wilcoxon signed-rank test, and what it does with a zero difference.</summary>
internal static class WilcoxonSample
{
    private static readonly double[] Before = [12.0, 9.0, 15.0, 11.0, 8.0, 14.0, 10.0];
    private static readonly double[] After = [10.0, 9.0, 12.0, 11.0, 6.0, 11.0, 7.0];

    public static void Run()
    {
        Console.WriteLine("Wilcoxon signed-rank");

        // Two of the seven pairs are unchanged, which is exactly what the three
        // zero methods disagree about.
        foreach (ZeroMethod zeroMethod in (ZeroMethod[])[ZeroMethod.Wilcox, ZeroMethod.Pratt, ZeroMethod.ZSplit])
        {
            TestResult result = Wilcoxon.Paired(Before, After, zeroMethod);
            Console.WriteLine(
                $"  {zeroMethod,-7} W = {Inv.F3(result.Statistic)}  p = {Inv.F3(result.PValue)}");
        }

        TestResult differences = Wilcoxon.OneSample([2.0, 0.0, 3.0, 0.0, 2.0, 3.0, 3.0]);
        Console.WriteLine($"  from differences  W   = {Inv.F3(differences.Statistic)}");
    }
}
