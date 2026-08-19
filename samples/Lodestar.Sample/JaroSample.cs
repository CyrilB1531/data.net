using Lodestar.Text;
using Lodestar.Text.Distances;

namespace Lodestar.Sample;

/// <summary>A similarity built on matches and transpositions, not on edits.</summary>
internal static class JaroSample
{
    public static void Run()
    {
        const string Martha = "martha";
        const string Marhta = "marhta";

        Console.WriteLine($"  Jaro(martha, marhta)                = {Inv.F4(Jaro.Similarity(Martha, Marhta))}");
        Console.WriteLine($"  Jaro as a distance                  = {Inv.F4(Jaro.Distance(Martha, Marhta))}");
    }
}
