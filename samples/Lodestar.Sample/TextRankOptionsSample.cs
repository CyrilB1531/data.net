using Lodestar.Text.Keywords;

namespace Lodestar.Sample;

/// <summary>
/// What each switch buys: Words overrides Ratio when both are set, the window decides
/// how far a co-occurrence edge reaches, damping and tolerance shape how the power
/// iteration converges, and the stop words and token pattern decide what is ranked at all.
/// </summary>
internal static class TextRankOptionsSample
{
    public static void Run()
    {
        // Words wins over Ratio whenever both are set -- a ratio of 0.9 would keep far
        // more than three of this graph's nodes, and it does not.
        var byWords = new TextRank(new TextRankOptions { Words = 3, Ratio = 0.9 });
        Console.WriteLine($"  Words=3, Ratio=0.9      = {byWords.Extract(KeywordsCorpus.Document).Count} keywords");

        // Left to Ratio alone, this document's graph is too small for 10% of its nodes
        // to round up to one: the extractor returns none rather than round up for it.
        var byRatio = new TextRank(new TextRankOptions { Ratio = 0.1 });
        Console.WriteLine($"  Ratio=0.1 alone         = {byRatio.Extract(KeywordsCorpus.Document).Count} keywords");

        // A wider window lets two words rank together from further apart in the raw
        // stream, which is enough to change which stem the graph ranks first here.
        var narrowWindow = new TextRank(new TextRankOptions { Window = 2, Words = 3 });
        var wideWindow = new TextRank(new TextRankOptions { Window = 5, Words = 3 });
        Console.WriteLine($"  Window=2 top            = {narrowWindow.Extract(KeywordsCorpus.Document)[0].Phrase}");
        Console.WriteLine($"  Window=5 top            = {wideWindow.Extract(KeywordsCorpus.Document)[0].Phrase}");

        // MaxIterations is a cap the default Tolerance cannot meet in one step; loosening
        // Tolerance lets that same cap converge instead of throwing.
        try
        {
            _ = new TextRank(new TextRankOptions { MaxIterations = 1 }).Extract(KeywordsCorpus.Document);
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"  MaxIterations=1         = {ex.Message}");
        }
        var loose = new TextRank(new TextRankOptions { MaxIterations = 1, Tolerance = 0.5 });
        Console.WriteLine($"  ... Tolerance=0.5       = {loose.Extract(KeywordsCorpus.Document)[0].Phrase}, no throw");

        // A damping closer to 1 weighs the graph's own structure more against the random
        // jump, which moves the winning stem's score without changing which stem wins.
        var lowDamping = new TextRank(new TextRankOptions { Damping = 0.15, Words = 3 });
        var highDamping = new TextRank(new TextRankOptions { Damping = 0.85, Words = 3 });
        Console.WriteLine($"  Damping=0.15 top score  = {Inv.F4(lowDamping.Extract(KeywordsCorpus.Document)[0].Score)}");
        Console.WriteLine($"  Damping=0.85 top score  = {Inv.F4(highDamping.Extract(KeywordsCorpus.Document)[0].Score)}");

        // Stop words delimit the graph's nodes, not just filter them: with only "of" held
        // back, "the" survives long enough to rank alongside the document's real content.
        var narrowStop = new TextRank(new TextRankOptions { StopWords = ["of"], Words = 3 });
        Console.WriteLine($"  StopWords={{\"of\"}}        = "
            + $"{string.Join(", ", narrowStop.Extract(KeywordsCorpus.Document).Select(m => m.Phrase))}");

        // TokenPattern decides what counts as a word at all: requiring four characters
        // changes which stems ever reach the graph, and so which one ranks first.
        var longWords = new TextRank(new TextRankOptions { TokenPattern = @"\b\w{4,}\b", Words = 3 });
        Console.WriteLine($"  TokenPattern >= 4 chars = {longWords.Extract(KeywordsCorpus.Document)[0].Phrase}");
        Console.WriteLine();
    }
}
