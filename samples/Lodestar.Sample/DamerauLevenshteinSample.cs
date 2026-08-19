using Lodestar.Text;
using Lodestar.Text.Distances;

namespace Lodestar.Sample;

/// <summary>Levenshtein plus the transposition, which is the typo it does not charge twice.</summary>
internal static class DamerauLevenshteinSample
{
    public static void Run()
    {
        const string Ca = "ca";
        const string Abc = "abc";

        Console.WriteLine($"  DamerauLevenshtein(ca, abc)         = {DamerauLevenshtein.Distance(Ca, Abc)}");
        Console.WriteLine($"  DamerauLevenshtein normalized       = {Inv.F4(DamerauLevenshtein.NormalizedDistance(Ca, Abc))} distance, "
            + $"{Inv.F4(DamerauLevenshtein.NormalizedSimilarity(Ca, Abc))} similarity");
    }
}
