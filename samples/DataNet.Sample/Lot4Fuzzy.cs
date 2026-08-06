using DataNet.Fuzzy;

namespace DataNet.Sample;

// SonarLint S1192: "apple pie" and its misspelling are the data of the
// demonstration, not a magic value. Each call shows the exact pair being scored,
// and hoisting them into constants would make a reader open two definitions to
// find out what is being compared to what — for a file whose only job is to be
// read.
#pragma warning disable S1192

/// <summary>
/// Lot 4 — DataNet.Fuzzy, the rapidfuzz-compatible surface.
/// </summary>
internal static class Lot4Fuzzy
{
    private static readonly string[] Candidates = ["apple pie", "banana bread", "cherry tart", "apple tart"];

    public static void Run()
    {
        Console.WriteLine("lot 4 — fuzzy matching");

        // The seven scorers. Each is a rapidfuzz `fuzz.*` function by the same name.
        Console.WriteLine($"  Ratio                 = {Fuzz.Ratio("apple pie", "appel pie"):F1}");
        Console.WriteLine($"  PartialRatio          = {Fuzz.PartialRatio("apple", "an apple a day"):F1}");
        Console.WriteLine($"  TokenSortRatio        = {Fuzz.TokenSortRatio("pie apple", "apple pie"):F1}");
        Console.WriteLine($"  TokenSetRatio         = {Fuzz.TokenSetRatio("apple pie apple", "apple pie"):F1}");
        Console.WriteLine($"  PartialTokenSortRatio = {Fuzz.PartialTokenSortRatio("pie apple", "the apple pie"):F1}");
        Console.WriteLine($"  PartialTokenSetRatio  = {Fuzz.PartialTokenSetRatio("apple pie", "the apple pie today"):F1}");
        Console.WriteLine($"  WRatio                = {Fuzz.WRatio("apple pie", "appel pie"):F1}");

        // Process: the same scorers applied across a collection of choices.
        ExtractResult? best = Process.ExtractOne("appel pie", Candidates, scorer: Fuzz.WRatio, scoreCutoff: 0);
        Console.WriteLine($"  ExtractOne            = {best?.Choice} ({best?.Score:F1}) at index {best?.Index}");
        IReadOnlyList<ExtractResult> top = Process.Extract("appel pie", Candidates, scorer: Fuzz.WRatio, limit: 2, scoreCutoff: 0);
        foreach (ExtractResult hit in top)
        {
            Console.WriteLine($"    Extract #{hit.Index} {hit.Choice} ({hit.Score:F1})");
        }

        // Deduplicator: blocked pairwise clustering, so the scorer never sees the
        // full cross product.
        string[] records = ["apple pie", "appel pie", "banana bread", "banana bred"];
        IReadOnlyList<IReadOnlyList<int>> clusters = Deduplicator.FindClusters(
            records,
            blockingKey: static record => record[..1],
            similarity: static (a, b) => Fuzz.Ratio(a, b),
            threshold: 80);
        Console.WriteLine($"  FindClusters          = {clusters.Count} clusters: "
            + string.Join(" | ", clusters.Select(c => "{" + string.Join(",", c) + "}")));
        Console.WriteLine();
    }
}
