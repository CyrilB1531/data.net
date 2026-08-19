using Lodestar.Text.Similarity;

namespace Lodestar.Sample;

/// <summary>The angle between two q-gram multisets.</summary>
internal static class CosineSample
{
    public static void Run()
    {
        const string A = "night";
        const string B = "nacht";

        Console.WriteLine($"  Cosine(night, nacht)               = {Inv.F4(Cosine.Similarity(A, B))}");
    }
}
