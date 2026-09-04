using Lodestar.Text.Keywords;

namespace Lodestar.Sample;

/// <summary>
/// What each switch buys: the metric rescales the same candidates, the length bounds
/// decide which runs are candidates at all, repetition decides whether a repeat counts
/// once, and the stop words and the token pattern both decide where a run breaks.
/// </summary>
internal static class RakeOptionsSample
{
    public static void Run()
    {
        // The three metrics agree on this document's top candidate and disagree on the
        // score: WordDegree counts co-occurrence alone, WordFrequency counts occurrences.
        foreach (RakeMetric metric in new[] { RakeMetric.DegreeToFrequencyRatio, RakeMetric.WordDegree, RakeMetric.WordFrequency })
        {
            var rake = new Rake(new RakeOptions { Metric = metric });
            KeywordMatch top = rake.Extract(KeywordsCorpus.Document)[0];
            Console.WriteLine($"  {metric,-22}= {top.Phrase} ({Inv.F4(top.Score)})");
        }

        // A bound of 1 on both ends admits only single words, where the unbounded
        // default folded three of them into longer runs instead.
        var singleWords = new Rake(new RakeOptions { MinLength = 1, MaxLength = 1 });
        Console.WriteLine($"  MinLength=MaxLength=1 = {singleWords.Extract(KeywordsCorpus.Document).Count} candidates");

        // IncludeRepeatedPhrases reports a repeat as its own candidate by default; false
        // folds every later occurrence back into the first.
        const string Repeated = "linear constraints. linear constraints again.";
        var deduped = new Rake(new RakeOptions { IncludeRepeatedPhrases = false });
        var withRepeats = new Rake(new RakeOptions { IncludeRepeatedPhrases = true });
        Console.WriteLine($"  IncludeRepeatedPhrases: false={deduped.Extract(Repeated).Count}, true={withRepeats.Extract(Repeated).Count}");

        // Stop words delimit runs as well as drop them: with only "of" held back, "over"
        // and "the" no longer split the run, so it glues into one longer candidate.
        var narrowStop = new Rake(new RakeOptions { StopWords = ["of"] });
        Console.WriteLine($"  StopWords={{\"of\"}}       = {narrowStop.Extract(KeywordsCorpus.Document)[0].Phrase}");

        // TokenPattern decides what counts as a word at all: requiring four characters
        // drops "set" from the candidates outright rather than merging it elsewhere.
        var longWords = new Rake(new RakeOptions { TokenPattern = @"\b\w{4,}\b" });
        Console.WriteLine($"  TokenPattern >= 4 chars = {longWords.Extract(KeywordsCorpus.Document).Count} candidates");
        Console.WriteLine();
    }
}
