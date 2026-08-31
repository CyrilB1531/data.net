using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests.Tokenization;

/// <summary>
/// The resolution path, on a directly built vocabulary: each rule the oracle corpus
/// replays, stated once where a failure names the rule rather than a stream.
/// </summary>
public sealed class BpeByteFallbackTests
{
    [Fact]
    public void An_uncovered_symbol_becomes_one_piece_per_utf8_byte()
    {
        BpeTokenizer tokenizer = Build();

        Assert.Equal(["a", "<0xC3>", "<0xA9>", "b"], tokenizer.Encode("aéb").Tokens);
        Assert.Equal(["<0xE6>", "<0x97>", "<0xA5>"], tokenizer.Encode("日").Tokens);
        Assert.Equal(["<0xF0>", "<0x9F>", "<0x99>", "<0x82>"], tokenizer.Encode("🙂").Tokens);
    }

    [Fact]
    public void A_covered_symbol_is_never_expanded()
    {
        BpeTokenizer tokenizer = Build(covering: "é");

        Assert.Equal(["é"], tokenizer.Encode("é").Tokens);
    }

    [Fact]
    public void Byte_pieces_take_part_in_merges()
    {
        // Measured: declaring the merge <0xC3> <0xA9> gives one token, which a post-pass
        // over unmergeable symbols could not produce.
        BpeTokenizer tokenizer = Build(covering: "<0xC3><0xA9>", merge: ("<0xC3>", "<0xA9>"));

        Assert.Equal(["<0xC3><0xA9>"], tokenizer.Encode("é").Tokens);
    }

    [Fact]
    public void A_byte_resolved_symbol_is_never_fused()
    {
        // fuse_unk fuses the unknown token; there is no unknown token left to fuse here.
        BpeTokenizer tokenizer = Build(fuseUnk: true);

        Assert.Equal(["a", "<0xC3>", "<0xA9>", "<0xC3>", "<0xA9>", "b"], tokenizer.Encode("aééb").Tokens);
    }

    [Fact]
    public void The_literal_text_of_a_piece_is_not_the_piece()
    {
        // "<0xC3>" the text is six characters, each resolving on its own.
        BpeTokenizer tokenizer = Build();

        Assert.Equal(
            ["<0x3C>", "<0x30>", "<0x78>", "<0x43>", "<0x33>", "<0x3E>"],
            tokenizer.Encode("<0xC3>").Tokens);
    }

    [Fact]
    public void A_hand_built_vocabulary_missing_a_piece_is_refused()
    {
        // BpeVocabulary is public and constructible without a loader, so LoadBpe's refusal
        // cannot reach here; without this one Encode fails on a bare dictionary indexer.
        var vocab = new Dictionary<string, int>(StringComparer.Ordinal) { ["<unk>"] = 0, ["a"] = 1, ["b"] = 2 };
        for (int i = 0; i < BytePieces.Count; i++)
        {
            if (i is not (0x28 or 0xA9))
            {
                vocab[BytePieces.Name(i)] = vocab.Count;
            }
        }

        ArgumentException thrown = Assert.Throws<ArgumentException>(() => new BpeTokenizer(
            new BpeVocabulary(vocab, []) { ByteFallback = true, UnkToken = "<unk>", NoPreTokenizer = true }));

        Assert.Contains("<0x28>", thrown.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("<0xA9>", thrown.Message, StringComparison.Ordinal);
    }

    /// <summary>A vocabulary of <c>a</c>, <c>b</c>, the unknown token and all 256 byte pieces.</summary>
    private static BpeTokenizer Build(string? covering = null, (string Left, string Right)? merge = null, bool fuseUnk = false)
    {
        var vocab = new Dictionary<string, int>(StringComparer.Ordinal) { ["<unk>"] = 0, ["a"] = 1, ["b"] = 2 };
        for (int i = 0; i < BytePieces.Count; i++)
        {
            vocab[BytePieces.Name(i)] = vocab.Count;
        }
        if (covering is not null)
        {
            vocab[covering] = vocab.Count;
        }
        var merges = new List<MergePair>();
        if (merge is { } pair)
        {
            merges.Add(new MergePair(pair.Left, pair.Right));
        }
        return new BpeTokenizer(new BpeVocabulary(vocab, merges)
        {
            ByteFallback = true,
            UnkToken = "<unk>",
            FuseUnk = fuseUnk,
            NoPreTokenizer = true,
        });
    }
}
