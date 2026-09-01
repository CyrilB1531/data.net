using Lodestar.Abstractions;
using Lodestar.Text.Persistence;
using Lodestar.Text.Vectorization;

namespace Lodestar.Sample;

/// <summary>Counting and weighting in one pass, and the width a later transform keeps.</summary>
internal static class TfidfVectorizerSample
{
    public static void Run()
    {
        var tfidf = new TfidfVectorizer(new TfidfVectorizerOptions
        {
            Count = TextCorpus.Counting(),
            Tfidf = new TfidfOptions { Norm = SparseNorm.L2 },
        });
        CsrMatrix matrix = tfidf.Fit(TextCorpus.Documents).FitTransform(TextCorpus.Documents);
        Console.WriteLine($"  TfidfVectorizer  : {matrix.RowCount} docs x {matrix.ColumnCount} terms, {matrix.Values.Length} non-zeros");

        // A term the fit never saw is dropped rather than counted, so the width is the fit's.
        CsrMatrix unseen = tfidf.Transform(["a document about cats and dogs"]);
        Console.WriteLine($"  Tfidf transform  : {unseen.ColumnCount} terms wide, the fit's width, "
            + $"idf[0]={Inv.F4(tfidf.Idf[0])}");

        // A fitted vocabulary is worth persisting: refitting is what a consumer cannot do
        // on a machine that never saw the training corpus.
        TfidfVectorizer reloaded = RoundTrip(tfidf);
        Console.WriteLine($"  reloaded         : {reloaded.GetFeatureNames().Count} features");
    }

    private static TfidfVectorizer RoundTrip(TfidfVectorizer vectorizer)
    {
        using var buffer = new MemoryStream();
        vectorizer.Save(buffer);
        buffer.Position = 0;
        return TfidfVectorizer.Load(buffer, new ArtifactLoadOptions { MaxTotalBytes = 8L * 1024 * 1024 });
    }
}
