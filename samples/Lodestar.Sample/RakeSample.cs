using Lodestar.Text.Keywords;

namespace Lodestar.Sample;

/// <summary>Candidates between stop words, scored by summing a per-word score over the run.</summary>
internal static class RakeSample
{
    public static void Run()
    {
        Rake rake = new();
        IReadOnlyList<KeywordMatch> matches = rake.Extract(KeywordsCorpus.Document);

        Console.WriteLine($"  candidates found = {matches.Count}");
        Console.WriteLine($"  top phrase       = {matches[0].Phrase}");
        Console.WriteLine($"  runner-up        = {matches[1].Phrase}");
        Console.WriteLine();
    }
}
