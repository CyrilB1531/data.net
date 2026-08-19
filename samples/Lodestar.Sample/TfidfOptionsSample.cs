using Lodestar.Text.Vectorization;

namespace Lodestar.Sample;

/// <summary>What the weighting does with a term seen everywhere, and with one seen once.</summary>
internal static class TfidfOptionsSample
{
    public static void Run()
    {
        var options = new TfidfOptions { Norm = SparseNorm.L2, SmoothIdf = true, SublinearTf = false, UseIdf = true };

        Console.WriteLine($"  TfidfOptions     : norm={options.Norm}, smoothIdf={options.SmoothIdf}, "
            + $"sublinearTf={options.SublinearTf}, useIdf={options.UseIdf}");
    }
}
