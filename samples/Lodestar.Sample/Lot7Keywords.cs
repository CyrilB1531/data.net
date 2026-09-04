using Lodestar.Text.Keywords;
using Lodestar.Text.Vectorization;

namespace Lodestar.Sample;

/// <summary>Lot 7 — what one document is about, three ways.</summary>
internal static class Lot7Keywords
{
    private const string Document =
        "Compatibility of systems of linear constraints over the set of natural numbers. " +
        "Criteria of compatibility of a system of linear Diophantine equations.";

    public static void Run()
    {
        Console.WriteLine("lot 7 — keyword extraction");

        IReadOnlyList<KeywordMatch> rake = new Rake().Extract(Document);
        Console.WriteLine($"  RAKE, deg/freq   : {rake[0].Phrase} ({Inv.F4(rake[0].Score)})");

        // Every RakeOptions switch, on one instance, rather than the defaults the
        // first call above took.
        var byDegree = new Rake(new RakeOptions
        {
            Metric = RakeMetric.WordDegree,
            MinLength = 2,
            MaxLength = 4,
            IncludeRepeatedPhrases = false,
            StopWords = StopWords.English,
            TokenPattern = @"\b\w+\b",
        });
        Console.WriteLine($"  RAKE, degree     : {byDegree.Extract(Document)[0].Phrase}");

        // Every TextRankOptions switch, on one instance -- Words wins over Ratio
        // when both are set, so setting both here is still meaningful.
        var textRank = new TextRank(new TextRankOptions
        {
            Words = 3,
            Ratio = 0.3,
            Damping = 0.85,
            Tolerance = 1e-10,
            MaxIterations = 500,
            Window = 3,
            StopWords = StopWords.English,
            TokenPattern = @"\b\w+\b",
        });
        IReadOnlyList<KeywordMatch> ranked = textRank.Extract(Document);
        Console.WriteLine($"  TextRank         : {ranked[0].Phrase} ({Inv.F4(ranked[0].Score)})");
        Console.WriteLine();
    }
}
