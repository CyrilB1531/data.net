using Lodestar.Text;
using Lodestar.Text.Distances;

namespace Lodestar.Sample;

/// <summary>Insertions, deletions and substitutions — and the unit they are counted in.</summary>
internal static class LevenshteinSample
{
    public static void Run()
    {
        const string Kitten = "kitten";
        const string Sitting = "sitting";

        Console.WriteLine($"  Levenshtein(kitten, sitting)        = {Levenshtein.Distance(Kitten, Sitting)}");
        Console.WriteLine($"  Levenshtein normalized              = {Inv.F4(Levenshtein.NormalizedSimilarity(Kitten, Sitting))}");
        Console.WriteLine($"  Levenshtein normalized              = {Inv.F4(Levenshtein.NormalizedDistance(Kitten, Sitting))} distance");

        // TextElement is demonstrated here rather than in a file of its own: it is a
        // parameter, and an enum on its own has no call to show.
        Console.WriteLine($"  Levenshtein(a<emoji>, a) code points = {Levenshtein.Distance("a\U0001F600", "a", TextElement.CodePoint)}");
    }
}
