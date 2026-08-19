using Lodestar.Text.Similarity;

namespace Lodestar.Sample;

/// <summary>Jaccard with the two asymmetries as parameters.</summary>
internal static class TverskySample
{
    public static void Run()
    {
        const string A = "night";
        const string B = "nacht";

        Console.WriteLine($"  Tversky(night, nacht)              = {Inv.F4(Tversky.Similarity(A, B))}");
    }
}
