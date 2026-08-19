using Lodestar.Text;
using Lodestar.Text.Distances;

namespace Lodestar.Sample;

/// <summary>The two longest-common readings: order-preserving, and contiguous.</summary>
internal static class LcsSample
{
    public static void Run()
    {
        Console.WriteLine($"  Lcs subsequence(AGCAT, GAC)         = {Lcs.SubsequenceLength("AGCAT", "GAC")}");
        Console.WriteLine($"  Lcs substring(AGCAT, GAC)           = {Lcs.SubstringLength("AGCAT", "GAC")}");
    }
}
