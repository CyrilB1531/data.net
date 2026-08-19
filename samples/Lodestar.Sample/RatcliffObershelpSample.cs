using Lodestar.Text;
using Lodestar.Text.Distances;

namespace Lodestar.Sample;

/// <summary>Matching blocks, recursively — difflib's ratio.</summary>
internal static class RatcliffObershelpSample
{
    public static void Run()
    {
        const string Pineapple = "pineapple";
        const string Pen = "pen";

        Console.WriteLine($"  RatcliffObershelp(pineapple, pen)   = {Inv.F4(RatcliffObershelp.Similarity(Pineapple, Pen))}");
        Console.WriteLine($"  RatcliffObershelp as a distance     = {Inv.F4(RatcliffObershelp.Distance(Pineapple, Pen))}");
    }
}
