using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>Kruskal-Wallis — the rank-based ANOVA, which assumes no shape at all.</summary>
internal static class KruskalWallisSample
{
    private static readonly double[] Morning = [12.0, 14.0, 11.0, 13.0, 15.0];
    private static readonly double[] Afternoon = [16.0, 15.0, 18.0, 17.0, 14.0];
    private static readonly double[] Evening = [21.0, 19.0, 22.0, 20.0, 23.0];

    public static void Run()
    {
        Console.WriteLine("Kruskal-Wallis");

        TestResult result = KruskalWallis.Test(Morning, Afternoon, Evening);

        Console.WriteLine($"  H                     = {Inv.F3(result.Statistic)}");
        Console.WriteLine($"  p-value               = {Inv.E3(result.PValue)}");
    }
}
