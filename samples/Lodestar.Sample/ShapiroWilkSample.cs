using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>Shapiro-Wilk — could this sample have come from a normal distribution?</summary>
internal static class ShapiroWilkSample
{
    private static readonly double[] Symmetric =
        [-1.62, -1.10, -0.74, -0.47, -0.23, 0.0, 0.23, 0.47, 0.74, 1.10, 1.62];

    private static readonly double[] Skewed =
        [0.1, 0.2, 0.3, 0.4, 0.6, 0.9, 1.4, 2.2, 3.6, 6.1, 12.0];

    public static void Run()
    {
        Console.WriteLine("Shapiro-Wilk");

        TestResult symmetric = ShapiroWilk.Test(Symmetric);
        TestResult skewed = ShapiroWilk.Test(Skewed);

        Console.WriteLine($"  symmetric W           = {Inv.F3(symmetric.Statistic)}");
        Console.WriteLine($"  symmetric p           = {Inv.F3(symmetric.PValue)}");
        Console.WriteLine($"  skewed W              = {Inv.F3(skewed.Statistic)}");
        Console.WriteLine($"  skewed p              = {Inv.E3(skewed.PValue)}");
    }
}
