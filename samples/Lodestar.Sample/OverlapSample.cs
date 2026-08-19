using Lodestar.Text.Similarity;

namespace Lodestar.Sample;

/// <summary>Intersection over the smaller of the two.</summary>
internal static class OverlapSample
{
    public static void Run()
    {
        const string A = "night";
        const string B = "nacht";

        Console.WriteLine($"  Overlap(night, nacht)              = {Inv.F4(Overlap.Similarity(A, B))}");
    }
}
