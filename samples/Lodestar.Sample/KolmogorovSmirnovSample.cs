using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>The two-sample Kolmogorov-Smirnov test — do these two samples share a distribution?</summary>
internal static class KolmogorovSmirnovSample
{
    private static readonly double[] Baseline = [0.1, 0.4, 0.6, 0.9, 1.3, 1.7, 2.2, 2.8];
    private static readonly double[] Candidate = [0.5, 1.1, 1.4, 2.0, 2.6, 3.1, 3.9, 4.4];

    public static void Run()
    {
        Console.WriteLine("Kolmogorov-Smirnov");

        KsResult result = KolmogorovSmirnov.TwoSample(Baseline, Candidate);

        Console.WriteLine($"  D                     = {Inv.F3(result.Statistic)}");
        Console.WriteLine($"  p-value               = {Inv.F3(result.PValue)}");
        Console.WriteLine($"  reached at            = {Inv.F3(result.StatisticLocation)}");
    }
}
