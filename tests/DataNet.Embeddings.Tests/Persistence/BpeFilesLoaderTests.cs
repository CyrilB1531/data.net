using System.Text;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests.Persistence;

public sealed class BpeFilesLoaderTests
{
    private const string Vocab = """{"a":0,"b":1,"ab":2,"[UNK]":3}""";
    private const string Merges = "#version: 0.2\na b\n";

    private static Stream Utf8(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));

    [Fact]
    public void Load_reads_the_vocabulary_and_the_ranked_merges()
    {
        BpeVocabulary vocab = BpeFilesLoader.Load(Utf8(Vocab), Utf8(Merges));

        Assert.Equal(4, vocab.Count);
        Assert.Equal(2, vocab.Vocab["ab"]);
        Assert.Single(vocab.Merges);
        Assert.Equal(new MergePair("a", "b"), vocab.Merges[0]);
        Assert.True(vocab.ByteLevel);
        Assert.Equal(BpePatterns.Gpt2, vocab.PreTokenizerPattern);
    }

    [Fact]
    public void The_version_comment_is_not_a_merge()
    {
        BpeVocabulary vocab = BpeFilesLoader.Load(Utf8(Vocab), Utf8(Merges));
        Assert.Single(vocab.Merges);
        Assert.Equal(new MergePair("a", "b"), vocab.Merges[0]);
    }

    [Fact]
    public void A_merge_whose_left_symbol_starts_with_a_hash_is_kept()
    {
        // GPT-2's byte-level alphabet leaves '#' as itself, so eight of its
        // 50 000 merge lines start with one. '#' is a header marker on the
        // first line only, never a comment marker.
        BpeVocabulary vocab = BpeFilesLoader.Load(
            Utf8("{\"#\":0,\"##\":1,\"####\":2}"),
            Utf8("#version: 0.2\n# #\n## ##\n"));

        Assert.Equal(2, vocab.Merges.Count);
        Assert.Equal(new MergePair("#", "#"), vocab.Merges[0]);
        Assert.Equal(new MergePair("##", "##"), vocab.Merges[1]);
    }

    [Fact]
    public void A_merge_line_without_a_separator_is_refused()
    {
        Assert.Throws<InvalidDataException>(
            () => BpeFilesLoader.Load(Utf8(Vocab), Utf8("#version: 0.2\nab\n")));
    }

    [Fact]
    public void An_empty_vocabulary_is_refused()
    {
        Assert.Throws<InvalidDataException>(() => BpeFilesLoader.Load(Utf8("{}"), Utf8(Merges)));
    }

    [Fact]
    public void A_vocabulary_over_the_limit_is_refused()
    {
        var bounds = new ArtifactLoadOptions { MaxVocabularySize = 2 };
        Assert.Throws<InvalidDataException>(() => BpeFilesLoader.Load(Utf8(Vocab), Utf8(Merges), bounds));
    }

    [Fact]
    public void The_classic_layout_is_not_byte_level()
    {
        BpeVocabulary vocab = BpeFilesLoader.Load(Utf8(Vocab), Utf8(Merges), byteLevel: false);
        Assert.False(vocab.ByteLevel);
        Assert.Null(vocab.PreTokenizerPattern);
    }

    /// <summary>The real files, which is the layout the loader exists for.</summary>
    [Fact]
    public void Load_reads_the_vendored_gpt2_files()
    {
        string dir = Path.Combine(AppContext.BaseDirectory, "oracles");
        BpeVocabulary vocab = BpeFilesLoader.Load(
            Path.Combine(dir, "gpt2_vocab.json"),
            Path.Combine(dir, "gpt2_merges.txt"),
            new ArtifactLoadOptions { MaxTotalBytes = 8L * 1024 * 1024, MaxVocabularySize = 100_000, MaxArrayLength = 100_000 });

        Assert.Equal(50257, vocab.Count);
        Assert.Equal(0, vocab.SkippedMerges);
        Assert.Equal(new BpeTokenizer(ByteLevelBpeTests.Gpt2Vocabulary()).Encode("Hello, world!").Ids,
                     new BpeTokenizer(vocab).Encode("Hello, world!").Ids);
    }

    [Fact]
    public async Task LoadAsync_agrees_with_the_synchronous_overload()
    {
        // SonarLint S6966: calling the synchronous overload here is the point of
        // the test, not an oversight — it exists to compare its result against
        // LoadAsync's.
#pragma warning disable S6966
        BpeVocabulary sync = BpeFilesLoader.Load(Utf8(Vocab), Utf8(Merges));
#pragma warning restore S6966
        BpeVocabulary viaAsync = await BpeFilesLoader.LoadAsync(Utf8(Vocab), Utf8(Merges));
        Assert.Equal(sync, viaAsync);
    }
}
