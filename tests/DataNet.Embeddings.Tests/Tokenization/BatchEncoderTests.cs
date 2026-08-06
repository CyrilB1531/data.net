using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

/// <summary>
/// What <see cref="BatchEncoder"/> does at the edges the oracle cannot express:
/// the inputs it refuses, the templates other model families use, and the shape
/// of the batch it hands to the runtime.
/// </summary>
public sealed class BatchEncoderTests
{
    // Ids chosen to collide with nothing conventional, for the same reason the
    // corpus vocabulary puts [CLS] at 45: a template is a name, not a number.
    private static readonly Dictionary<string, int> Vocabulary = new(StringComparer.Ordinal)
    {
        ["[UNK]"] = 0,
        ["the"] = 1,
        ["cat"] = 2,
        ["sat"] = 3,
        ["[CLS]"] = 7,
        ["[SEP]"] = 8,
        ["[PAD]"] = 9,
        ["<s>"] = 10,
        ["</s>"] = 11,
        ["<pad>"] = 12,
    };

    private static WordPieceTokenizer Tokenizer() => new(Vocabulary, "[UNK]");

    private static BatchEncoder Encoder(EncodingOptions? options = null) => new(Tokenizer(), options);

    [Fact]
    public void Empty_text_encodes_to_the_template_alone()
    {
        Assert.Equal([7, 8], Encoder().Encode(""));
    }

    [Fact]
    public void Roberta_and_T5_wrap_their_own_way()
    {
        long[] roberta = Encoder(new EncodingOptions { Template = SpecialTokenTemplate.Roberta }).Encode("cat");
        long[] t5 = Encoder(new EncodingOptions { Template = SpecialTokenTemplate.T5 }).Encode("cat");
        long[] none = Encoder(new EncodingOptions { Template = SpecialTokenTemplate.None }).Encode("cat");

        Assert.Equal([10, 2, 11], roberta);
        Assert.Equal([2, 11], t5);          // T5 appends only the terminator
        Assert.Equal([2], none);
    }

    [Fact]
    public void A_vocabulary_missing_a_template_token_fails_loudly()
    {
        var withoutRoberta = new WordPieceTokenizer(
            new Dictionary<string, int>(StringComparer.Ordinal) { ["[UNK]"] = 0, ["cat"] = 1 }, "[UNK]");

        ArgumentException error = Assert.Throws<ArgumentException>(
            () => new BatchEncoder(withoutRoberta, new EncodingOptions { Template = SpecialTokenTemplate.Roberta }));

        // Naming the missing token is the whole point: the alternative is a model
        // fed a plausible id and an embedding that is wrong without being invalid.
        Assert.Contains("<s>", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void MaxLength_below_one_is_refused(int maxLength) =>
        Assert.Throws<ArgumentException>(() => Encoder(new EncodingOptions { MaxLength = maxLength }));

    [Fact]
    public void MaxLength_with_no_room_for_the_template_is_refused()
    {
        // BERT inserts two tokens, so a budget of one can hold no text at all.
        ArgumentException error = Assert.Throws<ArgumentException>(
            () => Encoder(new EncodingOptions { MaxLength = 1 }));
        Assert.Contains("special tokens", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void BatchSize_below_one_is_refused() =>
        Assert.Throws<ArgumentException>(() => Encoder(new EncodingOptions { BatchSize = 0 }));

    [Fact]
    public void Truncation_keeps_the_budget_and_the_template()
    {
        // MaxLength 4, two of which the template takes: two text tokens survive.
        long[] encoded = Encoder(new EncodingOptions { MaxLength = 4 }).Encode("the cat sat");
        Assert.Equal([7, 1, 2, 8], encoded);
    }

    [Fact]
    public void A_sequence_exactly_at_the_limit_is_left_alone()
    {
        long[] encoded = Encoder(new EncodingOptions { MaxLength = 5 }).Encode("the cat sat");
        Assert.Equal([7, 1, 2, 3, 8], encoded);
    }

    [Fact]
    public void Truncation_none_refuses_rather_than_dropping_the_tail()
    {
        var options = new EncodingOptions { MaxLength = 4, Truncation = TruncationStrategy.None };

        ArgumentException error = Assert.Throws<ArgumentException>(() => Encoder(options).Encode("the cat sat"));

        // The message has to carry the numbers, or the caller cannot tell whether
        // to raise the limit or shorten the document.
        Assert.Contains("5 tokens", error.Message, StringComparison.Ordinal);
        Assert.Contains("MaxLength of 4", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_batch_is_padded_to_its_own_longest_row()
    {
        EncodedBatch batch = Encoder().EncodeBatch(["cat", "the cat sat"]);

        Assert.Equal(2, batch.Count);
        Assert.Equal(5, batch.SequenceLength);
        Assert.Equal([7, 2, 8, 9, 9], batch.InputIds[..5].ToArray());
        Assert.Equal([1, 1, 1, 0, 0], batch.AttentionMask[..5].ToArray());
        Assert.Equal([7, 1, 2, 3, 8], batch.InputIds[5..].ToArray());
        Assert.Equal([1, 1, 1, 1, 1], batch.AttentionMask[5..].ToArray());
        Assert.Equal([3, 5], batch.Lengths);
    }

    [Fact]
    public void Sequence_returns_a_row_without_its_padding()
    {
        EncodedBatch batch = Encoder().EncodeBatch(["cat", "the cat sat"]);

        Assert.Equal([7, 2, 8], batch.Sequence(0).ToArray());
        Assert.Equal([7, 1, 2, 3, 8], batch.Sequence(1).ToArray());
        Assert.Throws<ArgumentOutOfRangeException>(() => batch.Sequence(2));
        Assert.Throws<ArgumentOutOfRangeException>(() => batch.Sequence(-1));
    }

    [Fact]
    public void An_empty_batch_is_empty_rather_than_a_zero_width_tensor()
    {
        EncodedBatch batch = Encoder().EncodeBatch([]);

        Assert.Equal(0, batch.Count);
        Assert.Equal(0, batch.SequenceLength);
        Assert.Empty(batch.InputIds.ToArray());
    }

    [Fact]
    public void Cancellation_is_observed_between_texts()
    {
        using var source = new CancellationTokenSource();
        source.Cancel();

        Assert.Throws<OperationCanceledException>(
            () => Encoder().EncodeBatch(["the", "cat"], source.Token));
    }

    [Fact]
    public void Null_arguments_are_refused()
    {
        Assert.Throws<ArgumentNullException>(() => new BatchEncoder(null!));
        Assert.Throws<ArgumentNullException>(() => Encoder().Encode(null!));
        Assert.Throws<ArgumentNullException>(() => Encoder().EncodeBatch(null!));
    }

    /// <summary>
    /// Two templates spelling the same convention are equal, and one spelling a
    /// different one is not.
    /// </summary>
    /// <remarks>
    /// A record advertises value equality, and the generated version would compare
    /// the two token lists by reference — so <c>SpecialTokenTemplate.Bert</c> would
    /// be unequal to a template a caller wrote out by hand.
    /// </remarks>
    [Fact]
    public void Templates_compare_by_value()
    {
        var handWritten = new SpecialTokenTemplate(["[CLS]"], ["[SEP]"], "[PAD]");

        Assert.Equal(SpecialTokenTemplate.Bert, handWritten);
        Assert.Equal(SpecialTokenTemplate.Bert.GetHashCode(), handWritten.GetHashCode());
        Assert.NotEqual(SpecialTokenTemplate.Bert, SpecialTokenTemplate.Roberta);
        Assert.NotEqual(SpecialTokenTemplate.Bert, new SpecialTokenTemplate(["[SEP]"], ["[CLS]"], "[PAD]"));
        Assert.Equal(2, SpecialTokenTemplate.Bert.SpecialTokenCount);
        Assert.Equal(0, SpecialTokenTemplate.None.SpecialTokenCount);
    }

    /// <summary>
    /// <c>SentencePieceTokenizer</c> keeps its control pieces out of the matching
    /// table so they can never match text; a template still has to be able to name
    /// them.
    /// </summary>
    [Fact]
    public void SentencePiece_resolves_the_control_pieces_it_refuses_to_match()
    {
        var vocabulary = new SentencePieceVocabulary(
            [
                new SentencePiece("<unk>", 0, 0),
                new SentencePiece("<s>", 0, 1),
                new SentencePiece("</s>", 0, 2),
                new SentencePiece("<pad>", 0, 3),
                new SentencePiece("▁cat", -1, 4),
            ],
            [
                SentencePieceType.Unknown,
                SentencePieceType.Control,
                SentencePieceType.Control,
                SentencePieceType.Control,
                SentencePieceType.Normal,
            ],
            UnkId: 0, BosId: 1, EosId: 2, PadId: 3);
        var tokenizer = new SentencePieceTokenizer(vocabulary);

        Assert.True(tokenizer.TryGetId("<s>", out int bos));
        Assert.True(tokenizer.TryGetId("</s>", out int eos));
        Assert.Equal(1, bos);
        Assert.Equal(2, eos);
        Assert.False(tokenizer.TryGetId("<mask>", out _));

        // And they are still unreachable from text, which is why they were excluded.
        Assert.DoesNotContain("<s>", tokenizer.Encode("<s> cat").Tokens);
    }
}
