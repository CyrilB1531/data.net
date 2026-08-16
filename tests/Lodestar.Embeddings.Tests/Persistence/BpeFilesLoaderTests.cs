using System.Text;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests.Persistence;

public sealed class BpeFilesLoaderTests
{
    private const string Vocab = """{"a":0,"b":1,"ab":2,"[UNK]":3}""";
    private const string Merges = "#version: 0.2\na b\n";

    private static MemoryStream Utf8(string text) => new MemoryStream(Encoding.UTF8.GetBytes(text));

    [Fact]
    public void Load_reads_the_vocabulary_and_the_ranked_merges()
    {
        BpeVocabulary vocab = BpeFilesLoader.Load(Utf8(Vocab), Utf8(Merges));

        Assert.Equal(4, vocab.Count);
        Assert.Equal(2, vocab.Vocab["ab"]);
        Assert.Single(vocab.Merges);
        Assert.Equal(new MergePair("a", "b"), vocab.Merges[0]);
        Assert.True(vocab.ByteLevel);
        Assert.Null(vocab.PreSplit);
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
        // GPT-2's byte-level alphabet leaves '#' as itself, so eight of its 50 000 merge lines start
        // with one. '#' is a header marker only on the first line, never a general comment marker.
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

    /// <summary>
    /// A line with more than one space is refused, not split on the first one. Python splits the whole
    /// line and requires exactly two fields, so <c>"a b c"</c> and <c>" a b"</c> are both errors there --
    /// checked against <c>tokenizers</c> 0.23.1, which reports "Merges text file invalid at line 1" for
    /// each. Splitting on the first space instead would load them as <c>("a", "b c")</c> and
    /// <c>("", "a b")</c>, a rank the model never had. Unreachable for a byte-level model, whose
    /// alphabet has no symbol containing a literal space, and reachable for the classic lineage.
    /// </summary>
    [Theory]
    [InlineData("a b c\n")]
    [InlineData("a  b\n")]
    [InlineData(" a b\n")]
    public void A_merge_line_that_is_not_two_symbols_is_refused(string line)
    {
        Assert.Throws<InvalidDataException>(
            () => BpeFilesLoader.Load(Utf8(Vocab), Utf8("#version: 0.2\n" + line)));
    }

    /// <summary>
    /// A trailing space is the one case that stays accepted: <c>"a "</c> splits into
    /// two fields, the second empty, and Python takes it.
    /// </summary>
    [Fact]
    public void A_merge_line_ending_in_a_space_is_a_pair_with_an_empty_right_symbol()
    {
        BpeVocabulary vocab = BpeFilesLoader.Load(Utf8(Vocab), Utf8("#version: 0.2\na \n"));

        Assert.Single(vocab.Merges);
        Assert.Equal(new MergePair("a", string.Empty), vocab.Merges[0]);
    }

    [Fact]
    public void An_empty_vocabulary_is_refused()
    {
        Assert.Throws<InvalidDataException>(() => BpeFilesLoader.Load(Utf8("{}"), Utf8(Merges)));
    }

    [Theory]
    [InlineData("""{"a":"not a number"}""")]
    [InlineData("""{"a":1.5}""")]
    [InlineData("""{"a":99999999999}""")]
    public void A_vocabulary_value_that_is_not_a_32_bit_integer_is_refused(string vocabJson)
    {
        // GetInt32 would leak InvalidOperationException (string value) or FormatException
        // (non-integer/out-of-range) -- neither matches the InvalidDataException this loader documents.
        Assert.Throws<InvalidDataException>(() => BpeFilesLoader.Load(Utf8(vocabJson), Utf8(Merges)));
    }

    [Fact]
    public void A_byte_order_mark_on_merges_txt_does_not_shift_ranks()
    {
        // Left undecoded, EF BB BF becomes a leading U+FEFF, so "#version" no longer matches at
        // offset 0 and the header line is misread as a spurious rank-0 merge, shifting every rank.
        byte[] bom = [0xEF, 0xBB, 0xBF];
        Stream withBom = new MemoryStream([.. bom, .. Encoding.UTF8.GetBytes(Merges)]);

        BpeVocabulary vocab = BpeFilesLoader.Load(Utf8(Vocab), withBom);
        BpeVocabulary withoutBom = BpeFilesLoader.Load(Utf8(Vocab), Utf8(Merges));

        Assert.Equal(withoutBom.Merges, vocab.Merges);
        Assert.Single(vocab.Merges);
        Assert.Equal(new MergePair("a", "b"), vocab.Merges[0]);
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
        // The classic lineage's own pattern, named rather than left null: null no
        // longer stands for it, and BpeTokenizer refuses a vocabulary declaring neither.
        Assert.Equal(BpePatterns.Whitespace, vocab.PreTokenizerPattern);
        Assert.False(vocab.NoPreTokenizer);
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
