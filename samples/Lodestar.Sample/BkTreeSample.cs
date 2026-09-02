using Lodestar.Text;
using Lodestar.Text.Distances;
using Lodestar.Text.Indexing;

namespace Lodestar.Sample;

/// <summary>
/// A spelling corrector's lookup: a small dictionary indexed once, then queried by radius
/// rather than scanned.
/// </summary>
internal static class BkTreeSample
{
    private static readonly string[] Dictionary =
        ["book", "books", "boo", "cook", "cake", "boon", "cape", "back", "bookkeeper"];

    public static void Run()
    {
        BkTree tree = BkTree.OverLevenshtein();
        tree.AddRange(Dictionary);
        Console.WriteLine($"  indexed               = {tree.Count} distinct words");
        Console.WriteLine($"  adding a duplicate    = {tree.Add("book")}");

        IReadOnlyList<BkTreeMatch> close = tree.WithinDistance("bok", 1);
        Console.WriteLine($"  within 1 of 'bok'     = {string.Join(", ", close.Select(h => h.Item))}");

        IReadOnlyList<BkTreeMatch> capped = tree.WithinDistance("bok", 3, limit: 2);
        Console.WriteLine($"  ... nearest two of 3  = {string.Join(", ", capped.Select(h => h.Item))}");

        IReadOnlyList<BkTreeMatch> nearest = tree.Nearest("bok", 3);
        Console.WriteLine($"  three nearest         = {string.Join(", ", nearest.Select(h => h.Item))}");

        // A metric the four factories do not offer: the constructor is what reaches it -- and,
        // on purpose, one that breaks "return 0 only for equal inputs": "Book" and "book" fold to 0.
        BkTree caseFolded = new((a, b) => Levenshtein.Distance(a.ToUpperInvariant(), b.ToUpperInvariant()));
        caseFolded.AddRange(["Book", "cook"]);
        Console.WriteLine($"  custom ctor, 1 of 'BOOK' = {caseFolded.WithinDistance("BOOK", 1).Count}");

        // The other three metrics index the same words differently: a transposition is one
        // edit for Damerau and two for Levenshtein, and a substitution is two for Indel.
        BkTree damerau = BkTree.OverDamerauLevenshtein(TextElement.CodePoint);
        damerau.AddRange(["form", "from"]);
        Console.WriteLine($"  damerau, 1 of 'form'  = {damerau.WithinDistance("form", 1).Count}");

        BkTree indel = BkTree.OverIndel();
        indel.AddRange(Dictionary);
        Console.WriteLine($"  indel, 1 of 'book'    = {indel.WithinDistance("book", 1).Count}");

        BkTree hamming = BkTree.OverHamming();
        hamming.AddRange(["1010", "1011", "0000"]);
        Console.WriteLine($"  hamming, 1 of '1010'  = {hamming.WithinDistance("1010", 1).Count}");
        Console.WriteLine();
    }
}
