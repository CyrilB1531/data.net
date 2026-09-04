using Lodestar.Text.Keywords;

namespace Lodestar.Sample;

/// <summary>What one extracted phrase carries, and why its score is not comparable across extractors.</summary>
internal static class KeywordMatchSample
{
    public static void Run()
    {
        Rake rake = new();
        KeywordMatch top = rake.Extract(KeywordsCorpus.Document)[0];
        Console.WriteLine($"  phrase                = {top.Phrase}");
        Console.WriteLine($"  score                 = {Inv.F4(top.Score)}");

        // A record struct, so equality is by value: the same document, extracted again,
        // returns a distinct instance that still compares equal.
        Console.WriteLine($"  equal by value        = {top == rake.Extract(KeywordsCorpus.Document)[0]}");
        Console.WriteLine();
    }
}
