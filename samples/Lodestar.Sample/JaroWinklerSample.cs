using Lodestar.Text;
using Lodestar.Text.Distances;

namespace Lodestar.Sample;

/// <summary>Jaro, with a common prefix weighted — the one for names.</summary>
internal static class JaroWinklerSample
{
    public static void Run()
    {
        const string Martha = "martha";
        const string Marhta = "marhta";

        Console.WriteLine($"  JaroWinkler(martha, marhta)         = {Inv.F4(JaroWinkler.Similarity(Martha, Marhta))}");
        Console.WriteLine($"  JaroWinkler as a distance           = {Inv.F4(JaroWinkler.Distance(Martha, Marhta))}");
    }
}
