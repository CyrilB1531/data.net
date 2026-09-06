using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>The Mann-Whitney U test — the rank-based two-sample comparison.</summary>
internal static class MannWhitneySample
{
    private static readonly double[] Control = [7.0, 3.0, 6.0, 2.0, 8.0, 5.0];
    private static readonly double[] Treated = [9.0, 12.0, 8.0, 11.0, 15.0, 10.0];

    public static void Run()
    {
        Console.WriteLine("Mann-Whitney U");

        TestResult exact = MannWhitney.Test(
            Control, Treated, Alternative.Less, Continuity.Applied, ExactMethod.Exact);
        TestResult asymptotic = MannWhitney.Test(
            Control, Treated, Alternative.Less, Continuity.Applied, ExactMethod.Asymptotic);

        Console.WriteLine($"  U                     = {Inv.F3(exact.Statistic)}");
        Console.WriteLine($"  exact p               = {Inv.E3(exact.PValue)}");
        Console.WriteLine($"  asymptotic p          = {Inv.E3(asymptotic.PValue)}");
    }
}
