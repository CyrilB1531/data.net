using Lodestar.Text;
using Lodestar.Text.Distances;

namespace Lodestar.Sample;

/// <summary>Position-by-position disagreement, for strings of one length.</summary>
internal static class HammingSample
{
    public static void Run()
    {
        const string Karolin = "karolin";
        const string Kathrin = "kathrin";

        Console.WriteLine($"  Hamming(karolin, kathrin)           = {Hamming.Distance(Karolin, Kathrin)}");
        Console.WriteLine($"  Hamming normalized similarity       = {Inv.F4(Hamming.NormalizedSimilarity(Karolin, Kathrin))}");
    }
}
