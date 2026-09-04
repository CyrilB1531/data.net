using Lodestar.Text.Keywords;

namespace Lodestar.Sample;

/// <summary>TextRank over the co-occurrence graph of one document's stems.</summary>
internal static class TextRankSample
{
    public static void Run()
    {
        TextRank textRank = new();
        IReadOnlyList<KeywordMatch> ranked = textRank.Extract(KeywordsCorpus.Document);

        Console.WriteLine($"  keywords found = {ranked.Count}");
        Console.WriteLine($"  top phrase     = {ranked[0].Phrase}");
        Console.WriteLine();
    }
}
