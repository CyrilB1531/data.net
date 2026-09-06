using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>
/// <see cref="KsResult"/> — the distance, the p-value, and where and in which
/// direction the two empirical distributions were furthest apart.
/// </summary>
internal static class KsResultSample
{
    private static readonly double[] Left = [1.0, 2.0, 3.0, 4.0, 5.0];
    private static readonly double[] Right = [3.0, 4.0, 5.0, 6.0, 7.0];

    public static void Run()
    {
        Console.WriteLine("KsResult");

        KsResult result = KolmogorovSmirnov.TwoSample(Left, Right);

        Console.WriteLine($"  statistic             = {Inv.F3(result.Statistic)}");
        Console.WriteLine($"  p-value               = {Inv.F3(result.PValue)}");
        Console.WriteLine($"  statistic location    = {Inv.F3(result.StatisticLocation)}");
        Console.WriteLine($"  statistic sign        = {result.StatisticSign}");
    }
}
