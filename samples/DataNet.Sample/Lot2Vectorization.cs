using Lodestar.Text.Persistence;
using Lodestar.Text.Vectorization;

namespace DataNet.Sample;

/// <summary>
/// Lot 2 — turning documents into sparse vectors, and persisting the fitted
/// vocabulary so a consumer can reload it without refitting.
/// </summary>
internal static class Lot2Vectorization
{
    private static readonly string[] Documents =
    [
        "the quick brown fox jumps over the lazy dog",
        "a quick brown dog outpaces a lazy fox",
        "the lazy dog sleeps",
    ];

    public static void Run()
    {
        Console.WriteLine("lot 2 — vectorization");

        // Counts first: CountVectorizer owns the tokenization and the vocabulary.
        var countOptions = new CountVectorizerOptions
        {
            Analyzer = AnalyzerKind.Word,
            Lowercase = true,
            StripAccents = true,
            Binary = false,
            NgramRange = (1, 2),
            MinDf = 0,
            MaxDf = 1.0,
            StopWords = StopWords.English,
            TokenPattern = @"\b\w\w+\b",
        };
        var counts = new CountVectorizer(countOptions);
        CsrMatrix countMatrix = counts.FitTransform(Documents);
        Console.WriteLine($"  CountVectorizer  : {countMatrix.RowCount} x {countMatrix.ColumnCount}, {countMatrix.NonZeroCount} non-zeros");
        Console.WriteLine($"  first features   : {string.Join(", ", counts.GetFeatureNames().Take(4))}");
        Console.WriteLine($"  stop-word lists  : en={StopWords.English.Count} fr={StopWords.French.Count} de={StopWords.German.Count} "
            + $"es={StopWords.Spanish.Count} it={StopWords.Italian.Count} pt={StopWords.Portuguese.Count}");

        // The CSR matrix is the exchange format between every stage.
        countMatrix.NormalizeRows(SparseNorm.L2);
        Console.WriteLine($"  row 0 L2 / L1    = {countMatrix.RowL2Norm(0):F4} / {countMatrix.RowL1Norm(0):F4}");
        Console.WriteLine($"  CSR arrays       : values={countMatrix.Values.Length} cols={countMatrix.ColumnIndices.Length} ptrs={countMatrix.RowPointers.Length}");
        double[] product = countMatrix.Multiply(new double[countMatrix.ColumnCount]);
        double[,] dense = countMatrix.ToDense();
        Console.WriteLine($"  Multiply / ToDense: {product.Length} rows, dense {dense.GetLength(0)}x{dense.GetLength(1)}");

        // The transformer applies the IDF weighting to counts someone else produced.
        var transformer = new TfidfTransformer(new TfidfOptions
        {
            Norm = SparseNorm.L2,
            SmoothIdf = true,
            SublinearTf = false,
            UseIdf = true,
        });
        CsrMatrix weighted = transformer.Fit(counts.Transform(Documents)).Transform(counts.Transform(Documents));
        Console.WriteLine($"  TfidfTransformer : {weighted.NonZeroCount} non-zeros, idf[0]={transformer.Idf[0]:F4}");

        // TfidfVectorizer is the two of them in one pass.
        var tfidf = new TfidfVectorizer(new TfidfVectorizerOptions
        {
            Count = countOptions,
            Tfidf = new TfidfOptions { Norm = SparseNorm.L2 },
        });
        CsrMatrix matrix = tfidf.Fit(Documents).FitTransform(Documents);
        Console.WriteLine($"  TfidfVectorizer  : {matrix.RowCount} docs x {matrix.ColumnCount} terms, {matrix.Values.Length} non-zeros");

        // Hashing needs no vocabulary, so it never sees the corpus twice.
        var hashing = new HashingVectorizer(new HashingVectorizerOptions
        {
            NumFeatures = 1 << 10,
            AlternateSign = true,
            Norm = SparseNorm.L2,
            Count = countOptions,
        });
        CsrMatrix hashed = hashing.FitTransform(Documents);
        Console.WriteLine($"  HashingVectorizer: {hashed.RowCount} x {hashing.NumFeatures}, {hashing.Transform(Documents).NonZeroCount} non-zeros on re-transform");

        // Round-trip each fitted artifact through a stream, with the load bounds
        // a consumer would apply to a file they did not produce.
        var bounds = new ArtifactLoadOptions
        {
            MaxTotalBytes = 8L * 1024 * 1024,
            MaxVocabularySize = 100_000,
            MaxTokenLength = 512,
            MaxArrayLength = 100_000,
            MaxJsonDepth = 32,
        };
        Console.WriteLine($"  reloaded         : tfidf={RoundTrip(tfidf, bounds).GetFeatureNames().Count} features, "
            + $"count={RoundTripCount(counts, bounds).GetFeatureNames().Count} features, "
            + $"hashing={RoundTripHashing(hashing, bounds).NumFeatures} buckets");
        Console.WriteLine();
    }

    private static TfidfVectorizer RoundTrip(TfidfVectorizer vectorizer, ArtifactLoadOptions bounds)
    {
        using var buffer = new MemoryStream();
        vectorizer.Save(buffer);
        buffer.Position = 0;
        return TfidfVectorizer.Load(buffer, bounds);
    }

    private static CountVectorizer RoundTripCount(CountVectorizer vectorizer, ArtifactLoadOptions bounds)
    {
        using var buffer = new MemoryStream();
        vectorizer.Save(buffer);
        buffer.Position = 0;
        return CountVectorizer.Load(buffer, bounds);
    }

    private static HashingVectorizer RoundTripHashing(HashingVectorizer vectorizer, ArtifactLoadOptions bounds)
    {
        using var buffer = new MemoryStream();
        vectorizer.Save(buffer);
        buffer.Position = 0;
        return HashingVectorizer.Load(buffer, bounds);
    }
}
