using Lodestar.Text;
using Lodestar.Text.Distances;

namespace Lodestar.Sample;

/// <summary>Insertions and deletions only — what fuzz.ratio is built on.</summary>
internal static class IndelSample
{
    public static void Run()
    {
        const string Kitten = "kitten";
        const string Sitting = "sitting";

        Console.WriteLine($"  Indel(kitten, sitting)              = {Indel.Distance(Kitten, Sitting)}");
        Console.WriteLine($"  Indel normalized                    = {Inv.F4(Indel.NormalizedDistance(Kitten, Sitting))} distance, "
            + $"{Inv.F4(Indel.NormalizedSimilarity(Kitten, Sitting))} similarity");
    }
}
