using Lodestar.Stats;

namespace Lodestar.Sample;

/// <summary>Pearson's chi-square: goodness of fit, and a contingency table.</summary>
internal static class ChiSquareSample
{
    private static readonly double[] Rolls = [16.0, 18.0, 16.0, 14.0, 12.0, 12.0];
    private static readonly double[][] Preference =
    [
        [30.0, 20.0],
        [15.0, 35.0],
    ];

    public static void Run()
    {
        Console.WriteLine("chi-square");

        TestResult fit = ChiSquare.GoodnessOfFit(Rolls);
        Console.WriteLine($"  fair-die statistic    = {Inv.F3(fit.Statistic)}");
        Console.WriteLine($"  fair-die p            = {Inv.F3(fit.PValue)}");

        Chi2ContingencyResult table = ChiSquare.Contingency(Preference);
        Console.WriteLine($"  contingency statistic = {Inv.F3(table.Statistic)}");
        Console.WriteLine($"  degrees of freedom    = {table.Dof}");
    }
}
