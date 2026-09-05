using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>
/// Adjusting a family of p-values — twenty tests at five percent produce one
/// significant result by chance alone.
/// </summary>
internal static class MultipleComparisonsSample
{
    private static readonly double[] Family = [0.001, 0.008, 0.039, 0.041, 0.042];

    public static void Run()
    {
        Console.WriteLine("multiple comparisons");

        Console.WriteLine($"  raw                   = {Inv.List(Family)}");
        Console.WriteLine($"  Bonferroni            = {Inv.List(MultipleComparisons.Bonferroni(Family))}");
        Console.WriteLine($"  Benjamini-Hochberg    = {Inv.List(MultipleComparisons.BenjaminiHochberg(Family))}");
        Console.WriteLine($"  Benjamini-Yekutieli   = {Inv.List(MultipleComparisons.BenjaminiYekutieli(Family))}");
    }
}
