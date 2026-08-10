using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests.Tokenization;

/// <summary>
/// These types are records, so they advertise value equality. Their payload is a
/// dictionary or a list, and the compiler-generated <c>Equals</c> compares those
/// with <see cref="EqualityComparer{T}.Default"/> — which falls back to reference
/// identity for collections. Two vocabularies with identical content would compare
/// unequal, silently, in the one place a caller has every reason to trust the
/// comparison.
/// </summary>
public sealed class ValueEqualityTests
{
    /// <summary>
    /// <see cref="AddedToken.Normalized"/> defaults to <c>!Special</c> rather than to
    /// <see langword="false"/>, so it is computed from a nullable backing field. The
    /// generated record equality would compare that field, making a token that left
    /// it unset unequal to one that set it to the value the default already gives —
    /// while both report the same <c>Normalized</c>. That is not a corner: it is a
    /// vocabulary read from a file compared against one written out by hand, which
    /// both vocabulary types do element-wise.
    /// </summary>
    [Fact]
    public void An_added_token_that_states_its_default_equals_one_that_leaves_it_unset()
    {
        var unset = new AddedToken("<x>", 2);
        var stated = new AddedToken("<x>", 2) { Normalized = true };

        Assert.True(unset.Normalized);
        Assert.Equal(unset, stated);
        Assert.Equal(unset.GetHashCode(), stated.GetHashCode());
    }

    [Fact]
    public void An_added_token_that_states_the_special_default_equals_one_that_leaves_it_unset()
    {
        var unset = new AddedToken("<x>", 2) { Special = true };
        var stated = new AddedToken("<x>", 2) { Special = true, Normalized = false };

        Assert.False(unset.Normalized);
        Assert.Equal(unset, stated);
        Assert.Equal(unset.GetHashCode(), stated.GetHashCode());
    }

    [Fact]
    public void Added_tokens_differing_only_in_the_pass_they_match_in_are_not_equal()
    {
        var raw = new AddedToken("<x>", 2) { Special = true };
        var normalized = new AddedToken("<x>", 2) { Special = true, Normalized = true };

        Assert.NotEqual(raw, normalized);
    }

    /// <summary>
    /// A <c>with</c> must not silently move a token into the other pass. The backing
    /// field carries through, so an explicit <see cref="AddedToken.Normalized"/>
    /// survives a copy that sets <see cref="AddedToken.Special"/> — where recomputing
    /// the default would flip it. <see cref="WordPieceTokenizer"/> copies added
    /// tokens this way to lowercase their content.
    /// </summary>
    [Fact]
    public void A_with_expression_keeps_an_explicitly_set_normalized_flag()
    {
        var special = new AddedToken("<x>", 2) { Normalized = true } with { Special = true };
        var ordinary = new AddedToken("<x>", 2) { Special = true, Normalized = false } with { Special = false };

        Assert.True(special.Normalized);
        Assert.False(ordinary.Normalized);
        // And an unset one still follows whatever Special the copy ends up with.
        Assert.False((new AddedToken("<x>", 2) with { Special = true }).Normalized);
    }

    [Fact]
    public void Two_word_piece_vocabularies_with_the_same_content_are_equal()
    {
        var a = new WordPieceVocabulary(
            new Dictionary<string, int>(StringComparer.Ordinal) { ["[UNK]"] = 0, ["alpha"] = 1 },
            "[UNK]",
            "##",
            Lowercase: true);
        var b = new WordPieceVocabulary(
            new Dictionary<string, int>(StringComparer.Ordinal) { ["[UNK]"] = 0, ["alpha"] = 1 },
            "[UNK]",
            "##",
            Lowercase: true);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Word_piece_vocabularies_differing_in_one_id_are_not_equal()
    {
        var a = new WordPieceVocabulary(
            new Dictionary<string, int>(StringComparer.Ordinal) { ["[UNK]"] = 0, ["alpha"] = 1 },
            "[UNK]", "##", Lowercase: false);
        var b = new WordPieceVocabulary(
            new Dictionary<string, int>(StringComparer.Ordinal) { ["[UNK]"] = 0, ["alpha"] = 2 },
            "[UNK]", "##", Lowercase: false);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Word_piece_vocabularies_differing_only_in_a_scalar_are_not_equal()
    {
        var vocab = new Dictionary<string, int>(StringComparer.Ordinal) { ["[UNK]"] = 0 };
        var a = new WordPieceVocabulary(vocab, "[UNK]", "##", Lowercase: true);
        var b = new WordPieceVocabulary(vocab, "[UNK]", "##", Lowercase: false);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Two_sentence_piece_vocabularies_with_the_same_content_are_equal()
    {
        SentencePieceVocabulary Build() => new(
            [new SentencePiece("<unk>", 0.0, 0), new SentencePiece("▁a", -1.5, 1)],
            [SentencePieceType.Unknown, SentencePieceType.Normal],
            UnkId: 0, BosId: -1, EosId: -1, PadId: -1);

        Assert.Equal(Build(), Build());
        Assert.Equal(Build().GetHashCode(), Build().GetHashCode());
    }

    [Fact]
    public void Sentence_piece_vocabularies_differing_in_one_score_are_not_equal()
    {
        var a = new SentencePieceVocabulary(
            [new SentencePiece("<unk>", 0.0, 0)], [SentencePieceType.Unknown],
            UnkId: 0, BosId: -1, EosId: -1, PadId: -1);
        var b = new SentencePieceVocabulary(
            [new SentencePiece("<unk>", -0.5, 0)], [SentencePieceType.Unknown],
            UnkId: 0, BosId: -1, EosId: -1, PadId: -1);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Sentence_piece_vocabularies_differing_in_one_type_are_not_equal()
    {
        SentencePiece[] pieces = [new SentencePiece("<unk>", 0.0, 0)];
        var a = new SentencePieceVocabulary(pieces, [SentencePieceType.Unknown], 0, -1, -1, -1);
        var b = new SentencePieceVocabulary(pieces, [SentencePieceType.Control], 0, -1, -1, -1);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void Two_tokenization_results_with_the_same_content_are_equal()
    {
        var a = new TokenizationResult(["play", "##ing"], [1, 2]);
        var b = new TokenizationResult(["play", "##ing"], [1, 2]);

        Assert.Equal(a, b);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void Tokenization_results_differing_in_one_id_are_not_equal()
    {
        var a = new TokenizationResult(["play"], [1]);
        var b = new TokenizationResult(["play"], [2]);

        Assert.NotEqual(a, b);
    }

    [Fact]
    public void A_tokenizer_result_can_be_compared_against_an_expected_literal()
    {
        // The reason this matters: writing the expected result out by hand is the
        // natural way to assert an encoding, and it silently failed before.
        var tokenizer = new WordPieceTokenizer(
            new Dictionary<string, int>(StringComparer.Ordinal) { ["[UNK]"] = 0, ["play"] = 1, ["##ing"] = 2 });

        TokenizationResult actual = tokenizer.Encode("playing");

        Assert.Equal(new TokenizationResult(["play", "##ing"], [1, 2]), actual);
    }

    private static BpeVocabulary SampleBpe() => new(
        new Dictionary<string, int>(StringComparer.Ordinal) { ["a"] = 0, ["b"] = 1, ["ab"] = 2 },
        [new MergePair("a", "b")])
    {
        ByteLevel = true,
        PreTokenizerPattern = BpePatterns.Gpt2,
    };

    [Fact]
    public void Two_BpeVocabularies_with_the_same_content_are_equal()
    {
        Assert.Equal(SampleBpe(), SampleBpe());
        Assert.Equal(SampleBpe().GetHashCode(), SampleBpe().GetHashCode());
    }

    [Fact]
    public void A_BpeVocabulary_differing_in_one_merge_is_not_equal()
    {
        BpeVocabulary other = SampleBpe() with { Merges = [new MergePair("b", "a")] };
        Assert.NotEqual(SampleBpe(), other);
    }

    [Fact]
    public void A_BpeVocabulary_differing_in_a_flag_is_not_equal()
    {
        Assert.NotEqual(SampleBpe(), SampleBpe() with { ByteLevel = false });
    }

    [Fact]
    public void Merge_order_is_rank_order()
    {
        BpeVocabulary vocab = SampleBpe();
        Assert.Equal(new MergePair("a", "b"), vocab.Merges[0]);
        Assert.Equal(3, vocab.Count);
    }

    [Fact]
    public void BpeVocabularies_differing_only_in_pre_tokenizer_pattern_are_not_equal_and_do_not_share_a_hash()
    {
        BpeVocabulary other = SampleBpe() with { PreTokenizerPattern = BpePatterns.Llama3 };

        Assert.NotEqual(SampleBpe(), other);
        Assert.NotEqual(SampleBpe().GetHashCode(), other.GetHashCode());
    }
}
