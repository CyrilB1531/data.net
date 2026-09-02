using Lodestar.Text.Indexing;

namespace Lodestar.Sample;

/// <summary>What one hit carries, and why its distance is not a score.</summary>
internal static class BkTreeMatchSample
{
    public static void Run()
    {
        BkTree tree = BkTree.OverLevenshtein();
        tree.AddRange(["book", "cook"]);

        BkTreeMatch hit = tree.WithinDistance("book", 1)[0];
        Console.WriteLine($"  item                  = {hit.Item}");
        Console.WriteLine($"  distance              = {hit.Distance}");

        // A record struct, so equality is by value rather than by reference.
        Console.WriteLine($"  equal by value        = {hit == new BkTreeMatch(hit.Item, hit.Distance)}");
        Console.WriteLine();
    }
}
