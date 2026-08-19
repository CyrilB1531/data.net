using Lodestar.Text;
using Lodestar.Text.Distances;

namespace Lodestar.Sample;

/// <summary>The restricted transposition: no substring is edited twice.</summary>
internal static class OsaSample
{
    public static void Run()
    {
        const string Ca = "ca";
        const string Abc = "abc";

        Console.WriteLine($"  Osa(ca, abc)                        = {Osa.Distance(Ca, Abc)}");
        Console.WriteLine($"  Osa normalized                      = {Inv.F4(Osa.NormalizedDistance(Ca, Abc))} distance, "
            + $"{Inv.F4(Osa.NormalizedSimilarity(Ca, Abc))} similarity");
    }
}
