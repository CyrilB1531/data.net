using Lodestar.Text.Persistence;
using Lodestar.Text.Vectorization;

namespace Lodestar.Sample;

/// <summary>The bounds a consumer applies to a file they did not produce.</summary>
internal static class ArtifactLoadOptionsSample
{
    public static void Run()
    {
        var bounds = new ArtifactLoadOptions
        {
            MaxTotalBytes = 8L * 1024 * 1024,
            MaxVocabularySize = 100_000,
            MaxTokenLength = 512,
            MaxArrayLength = 100_000,
            MaxJsonDepth = 32,
        };

        var counts = new CountVectorizer(TextCorpus.Counting());
        counts.Fit(TextCorpus.Documents);

        Console.WriteLine($"  reloaded         : count={RoundTrip(counts, bounds).GetFeatureNames().Count} features, "
            + $"bounds {bounds.MaxTotalBytes / (1024 * 1024)} MB / depth {bounds.MaxJsonDepth}");
    }

    private static CountVectorizer RoundTrip(CountVectorizer vectorizer, ArtifactLoadOptions bounds)
    {
        using var buffer = new MemoryStream();
        vectorizer.Save(buffer);
        buffer.Position = 0;
        return CountVectorizer.Load(buffer, bounds);
    }
}
