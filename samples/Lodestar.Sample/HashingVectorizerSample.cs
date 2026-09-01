using Lodestar.Abstractions;
using Lodestar.Text.Persistence;
using Lodestar.Text.Vectorization;

namespace Lodestar.Sample;

/// <summary>No vocabulary, so it never sees the corpus twice.</summary>
internal static class HashingVectorizerSample
{
    public static void Run()
    {
        var hashing = new HashingVectorizer(new HashingVectorizerOptions
        {
            NumFeatures = 1 << 10,
            AlternateSign = true,
            Norm = SparseNorm.L2,
            Count = TextCorpus.Counting(),
        });
        CsrMatrix hashed = hashing.FitTransform(TextCorpus.Documents);

        Console.WriteLine($"  HashingVectorizer: {hashed.RowCount} x {hashing.NumFeatures}, "
            + $"{hashing.Transform(TextCorpus.Documents).NonZeroCount} non-zeros on re-transform");

        // It has no vocabulary to persist, and still round-trips: the bucket count and
        // the sign choice are what a consumer has to reload to get the same columns.
        HashingVectorizer reloaded = RoundTrip(hashing);
        Console.WriteLine($"  reloaded         : {reloaded.NumFeatures} buckets");
    }

    private static HashingVectorizer RoundTrip(HashingVectorizer vectorizer)
    {
        using var buffer = new MemoryStream();
        vectorizer.Save(buffer);
        buffer.Position = 0;
        return HashingVectorizer.Load(buffer, new ArtifactLoadOptions { MaxTotalBytes = 8L * 1024 * 1024 });
    }
}
