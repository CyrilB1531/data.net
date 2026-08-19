using Lodestar.Text.Similarity;

namespace Lodestar.Sample;

/// <summary>Twice the intersection over the sizes summed.</summary>
internal static class SorensenDiceSample
{
    public static void Run()
    {
        const string A = "night";
        const string B = "nacht";

        Console.WriteLine($"  SorensenDice(night, nacht)         = {Inv.F4(SorensenDice.Similarity(A, B))}");
    }
}
