using Lodestar.Text.Vectorization;

namespace Lodestar.Sample;

/// <summary>The two option objects the one-pass vectorizer composes.</summary>
internal static class TfidfVectorizerOptionsSample
{
    public static void Run()
    {
        var options = new TfidfVectorizerOptions
        {
            Count = TextCorpus.Counting(),
            Tfidf = new TfidfOptions { Norm = SparseNorm.L2 },
        };

        Console.WriteLine($"  TfidfVectorizerOptions: counting analyzer={options.Count.Analyzer}, weighting norm={options.Tfidf.Norm}");
    }
}
