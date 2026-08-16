using Lodestar.Text.Vectorization;
using Xunit;

namespace Lodestar.Text.Tests.Persistence;

/// <summary>
/// Every vectorizer ships four ways to persist — stream, file, and an async
/// counterpart of each — and the round-trip suite exercised only the stream ones.
/// A file overload that never opened a file, or an async one that wrote a
/// different document, would have shipped unnoticed.
/// </summary>
public sealed class PersistenceOverloadTests : IDisposable
{
    private static readonly string[] Corpus =
    [
        "the quick brown fox jumps over the lazy dog",
        "the lazy dog sleeps all day long",
        "quick thinking beats slow planning every time",
    ];

    private static readonly string[] Holdout = ["the quick fox and the lazy dog", "slow planning wins"];

    private readonly string _path = Path.Combine(
        Path.GetTempPath(), $"datanet-overload-{Guid.NewGuid():N}.json");

    public void Dispose() => File.Delete(_path);

    [Fact]
    public void Count_round_trips_through_a_file()
    {
        var original = new CountVectorizer(Options).Fit(Corpus);

        original.Save(_path);
        CountVectorizer reloaded = CountVectorizer.Load(_path);

        Assert.Equal(original.GetFeatureNames(), reloaded.GetFeatureNames());
        AssertIdentical(original.Transform(Holdout), reloaded.Transform(Holdout));
    }

    [Fact]
    public async Task Count_round_trips_asynchronously()
    {
        var original = new CountVectorizer(Options).Fit(Corpus);

        using var stream = new MemoryStream();
        await original.SaveAsync(stream);
        stream.Position = 0;
        CountVectorizer reloaded = await CountVectorizer.LoadAsync(stream);

        Assert.Equal(original.GetFeatureNames(), reloaded.GetFeatureNames());
        AssertIdentical(original.Transform(Holdout), reloaded.Transform(Holdout));
    }

    [Fact]
    public async Task Count_writes_the_same_document_synchronously_and_asynchronously()
    {
        var original = new CountVectorizer(Options).Fit(Corpus);

        using var written = new MemoryStream();
        // SonarLint S6966: calling the synchronous writer here is the point — the test
        // exists to prove the two paths emit the same document.
#pragma warning disable S6966
        original.Save(written);
#pragma warning restore S6966
        using var writtenAsync = new MemoryStream();
        await original.SaveAsync(writtenAsync);

        Assert.Equal(written.ToArray(), writtenAsync.ToArray());
    }

    [Fact]
    public void Hashing_round_trips_through_a_file()
    {
        var original = new HashingVectorizer(new HashingVectorizerOptions { NumFeatures = 64, AlternateSign = false });

        original.Save(_path);
        HashingVectorizer reloaded = HashingVectorizer.Load(_path);

        // Hashing learns nothing, so its options are the whole artifact: a different
        // NumFeatures produces different columns and nothing downstream notices.
        AssertIdentical(original.Transform(Holdout), reloaded.Transform(Holdout));
    }

    [Fact]
    public async Task Hashing_round_trips_asynchronously()
    {
        var original = new HashingVectorizer(new HashingVectorizerOptions { NumFeatures = 64, AlternateSign = false });

        using var stream = new MemoryStream();
        await original.SaveAsync(stream);
        stream.Position = 0;
        HashingVectorizer reloaded = await HashingVectorizer.LoadAsync(stream);

        AssertIdentical(original.Transform(Holdout), reloaded.Transform(Holdout));
    }

    [Fact]
    public async Task Hashing_writes_the_same_document_synchronously_and_asynchronously()
    {
        var original = new HashingVectorizer(new HashingVectorizerOptions { NumFeatures = 64 });

        using var written = new MemoryStream();
        // SonarLint S6966: calling the synchronous writer here is the point — the test
        // exists to prove the two paths emit the same document.
#pragma warning disable S6966
        original.Save(written);
#pragma warning restore S6966
        using var writtenAsync = new MemoryStream();
        await original.SaveAsync(writtenAsync);

        Assert.Equal(written.ToArray(), writtenAsync.ToArray());
    }

    [Fact]
    public async Task Tfidf_round_trips_through_a_file_and_asynchronously()
    {
        var original = new TfidfVectorizer(new TfidfVectorizerOptions { Count = Options }).Fit(Corpus);

        original.Save(_path);
        TfidfVectorizer fromFile = TfidfVectorizer.Load(_path);

        using var stream = new MemoryStream();
        await original.SaveAsync(stream);
        stream.Position = 0;
        TfidfVectorizer fromStream = await TfidfVectorizer.LoadAsync(stream);

        AssertIdentical(original.Transform(Holdout), fromFile.Transform(Holdout));
        AssertIdentical(original.Transform(Holdout), fromStream.Transform(Holdout));
    }

    [Fact]
    public void Loading_a_file_that_does_not_exist_reports_the_path()
    {
        string missing = Path.Combine(Path.GetTempPath(), $"datanet-absent-{Guid.NewGuid():N}.json");

        Assert.Throws<FileNotFoundException>(() => CountVectorizer.Load(missing));
    }

    private static CountVectorizerOptions Options => new()
    {
        NgramRange = (1, 2),
        StopWords = ["the"],
        Binary = true,
    };

    private static void AssertIdentical(CsrMatrix expected, CsrMatrix actual)
    {
        Assert.Equal(expected.RowCount, actual.RowCount);
        Assert.Equal(expected.ColumnCount, actual.ColumnCount);
        Assert.Equal(expected.RowPointers, actual.RowPointers);
        Assert.Equal(expected.ColumnIndices, actual.ColumnIndices);
        for (int i = 0; i < expected.Values.Length; i++)
        {
            Assert.Equal(
                BitConverter.DoubleToInt64Bits(expected.Values[i]),
                BitConverter.DoubleToInt64Bits(actual.Values[i]));
        }
    }
}
