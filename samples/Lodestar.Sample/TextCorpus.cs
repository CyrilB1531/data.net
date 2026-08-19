using Lodestar.Text.Vectorization;

namespace Lodestar.Sample;

/// <summary>The three documents and the options every vectorization sample shares.</summary>
/// <remarks>
/// A helper, not a sample: it names no public type of its own. It exists so eleven
/// files do not each carry a copy of the same corpus, which SonarCloud reads as
/// duplication and a reader reads as eleven different corpora.
/// </remarks>
internal static class TextCorpus
{
    public static readonly string[] Documents =
    [
        "the quick brown fox jumps over the lazy dog",
        "a quick brown dog outpaces a lazy fox",
        "the lazy dog sleeps",
    ];

    public static CountVectorizerOptions Counting() => new()
    {
        Analyzer = AnalyzerKind.Word,
        Lowercase = true,
        StripAccents = true,
        Binary = false,
        NgramRange = (1, 2),
        MinDf = 0,
        MaxDf = 1.0,
        StopWords = Lodestar.Text.Vectorization.StopWords.English,
        TokenPattern = @"\b\w\w+\b",
    };
}
