using Lodestar.Text.Similarity;

namespace Lodestar.Sample;

/// <summary>Intersection over union, on q-gram multisets.</summary>
internal static class JaccardSample
{
    public static void Run()
    {
        const string A = "night";
        const string B = "nacht";

        Console.WriteLine($"  Jaccard(night, nacht)              = {Inv.F4(Jaccard.Similarity(A, B))}");
    }
}
