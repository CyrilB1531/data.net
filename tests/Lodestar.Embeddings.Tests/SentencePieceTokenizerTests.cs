using System.Text.Json;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Tokenization;
using Xunit;

namespace Lodestar.Embeddings.Tests;

public sealed class SentencePieceTokenizerTests
{
    // The id-based constructor is obsolete but still shipped, and "still shipped"
    // is only true if it still behaves. These tests exercise it deliberately.
#pragma warning disable CS0618

    private static SentencePieceTokenizer BuildFromOracle(JsonDocument doc)
    {
        JsonElement meta = doc.RootElement.GetProperty("metadata");
        var vocab = new List<SentencePiece>();
        foreach (JsonElement e in meta.GetProperty("vocab").EnumerateArray())
        {
            vocab.Add(new SentencePiece(
                e.GetProperty("piece").GetString()!,
                e.GetProperty("score").GetDouble(),
                e.GetProperty("id").GetInt32()));
        }
        int unkId = meta.GetProperty("unk_id").GetInt32();
        return new SentencePieceTokenizer(vocab, unkId);
    }

    [Fact]
    public void Encode_matches_sentencepiece()
    {
        using JsonDocument doc = OracleLoader.Load("sentencepiece.json");
        SentencePieceTokenizer tokenizer = BuildFromOracle(doc);

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string text = c.GetProperty("text").GetString()!;
            string[] expectedPieces = c.GetProperty("pieces").EnumerateArray().Select(e => e.GetString()!).ToArray();
            int[] expectedIds = c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()).ToArray();

            TokenizationResult actual = tokenizer.Encode(text);
            if (!expectedPieces.SequenceEqual(actual.Tokens) || !expectedIds.SequenceEqual(actual.Ids))
            {
                failures.Add($"\"{text}\"\n  exp pieces: [{string.Join(", ", expectedPieces)}]\n  got pieces: [{string.Join(", ", actual.Tokens)}]\n  exp ids: [{string.Join(", ", expectedIds)}]\n  got ids: [{string.Join(", ", actual.Ids)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// The obsolete constructor guesses which pieces are controls from their ids;
    /// the new one is told. On a model that puts its markers at 0, 1 and 2 — the
    /// case the guess was written for — the two must agree exactly. That is what
    /// makes the deprecation safe rather than a silent behaviour change.
    /// </summary>
    [Fact]
    public void The_obsolete_constructor_agrees_with_the_type_based_one()
    {
        using JsonDocument doc = OracleLoader.Load("sentencepiece.json");
        SentencePieceTokenizer byId = BuildFromOracle(doc);
        var byType = new SentencePieceTokenizer(
            SentencePieceModelLoader.Load(Path.Combine(AppContext.BaseDirectory, "oracles", "tiny_sp.model")));

        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string text = c.GetProperty("text").GetString()!;
            Assert.Equal(byId.Encode(text).Tokens, byType.Encode(text).Tokens);
            Assert.Equal(byId.Encode(text).Ids, byType.Encode(text).Ids);
        }
    }

#pragma warning restore CS0618

    /// <summary>
    /// A control marker sitting outside ids 0-2 -- the failure the id-based guess cannot see -- must
    /// still never be emitted. The input has to contain the marker's own string: encoding text with no
    /// <c>&lt;</c> in it asserts nothing, since the marker could not have been emitted either way and the
    /// test would pass just as happily with the exclusion removed. Feeding it <c>a&lt;s&gt;s</c> is what
    /// makes the assertion able to fail. The score is deliberately the best in the vocabulary, so a
    /// tokenizer that failed to exclude the marker would not merely be able to emit it -- Viterbi would prefer it.
    /// </summary>
    [Fact]
    public void Controls_outside_the_first_three_ids_are_still_excluded()
    {
        SentencePiece[] pieces =
        [
            new("a", -1.0, 0),
            new("▁", -2.0, 1),
            new("s", -5.0, 2),
            new("<s>", -0.1, 3),
            new("<unk>", 0.0, 4),
        ];
        SentencePieceType[] types =
        [
            SentencePieceType.Normal,
            SentencePieceType.Normal,
            SentencePieceType.Normal,
            SentencePieceType.Control,
            SentencePieceType.Unknown,
        ];
        var tokenizer = new SentencePieceTokenizer(new SentencePieceVocabulary(pieces, types, UnkId: 4, BosId: 3, EosId: -1, PadId: -1));

        TokenizationResult result = tokenizer.Encode("a<s>s");

        Assert.DoesNotContain("<s>", result.Tokens);
        Assert.DoesNotContain(3, result.Ids);
    }

    /// <summary>
    /// The same guarantee for the unknown piece: <c>&lt;unk&gt;</c> is what the tokenizer emits for
    /// uncovered text, so matching it as text would let a document name its own unknown token. The
    /// vocabulary covers every character of the input on purpose: left uncovered, the run
    /// <c>&lt;unk&gt;</c> comes back as a single unknown token whose surface is the same string, the same
    /// tokens and ids a wrongly-matched piece would produce, so the assertion could not tell the two
    /// apart. Covered, the two answers differ: five cheap pieces, or one matched marker scoring better.
    /// </summary>
    [Fact]
    public void The_unknown_piece_is_never_matched_as_text()
    {
        SentencePiece[] pieces =
        [
            new("a", -1.0, 0),
            new("▁", -2.0, 1),
            new("<unk>", -0.1, 2),
            .. "<unk>".Select((c, i) => new SentencePiece(c.ToString(), -4.0, 3 + i)),
        ];
        SentencePieceType[] types =
        [
            SentencePieceType.Normal,
            SentencePieceType.Normal,
            SentencePieceType.Unknown,
            .. Enumerable.Repeat(SentencePieceType.Normal, 5),
        ];
        var tokenizer = new SentencePieceTokenizer(new SentencePieceVocabulary(pieces, types, UnkId: 2, BosId: -1, EosId: -1, PadId: -1));

        TokenizationResult result = tokenizer.Encode("a<unk>a");

        Assert.DoesNotContain("<unk>", result.Tokens);
        Assert.DoesNotContain(2, result.Ids);
    }

    /// <summary>
    /// The fairseq layout, in one vocabulary: <c>&lt;s&gt;</c>=0, <c>&lt;pad&gt;</c>=1, <c>&lt;/s&gt;</c>=2,
    /// <c>&lt;unk&gt;</c>=3 and <c>&lt;mask&gt;</c> last -- HuggingFace's numbering for XLM-R, which the
    /// id-based guess reads as "only <c>&lt;s&gt;</c>, <c>&lt;pad&gt;</c> and <c>&lt;/s&gt;</c> are
    /// controls". Every marker is scored 0, the best in the vocabulary, and every character of the input
    /// is covered by a normal piece, so nothing here can come out as the unknown piece for want of an
    /// alternative: an id from this set in the output means the marker was matched as text. This is the
    /// fast, fixture-free mirror of <see cref="XlmRobertaFairseqTests"/>, which asserts the same property
    /// over XLM-R's real 250 002-piece vocabulary, <c>&lt;mask&gt;</c> at 250001 included.
    /// </summary>
    [Fact]
    public void A_fairseq_layout_matches_none_of_its_five_markers()
    {
        SentencePiece[] pieces =
        [
            new("<s>", 0.0, 0),
            new("<pad>", 0.0, 1),
            new("</s>", 0.0, 2),
            new("<unk>", 0.0, 3),
            new("▁", -3.0, 4),
            new("a", -2.0, 5),
            new("b", -2.0, 6),
            .. "<>/spdunkm".Select((c, i) => new SentencePiece(c.ToString(), -4.0, 7 + i)),
            new("<mask>", 0.0, 17),
        ];
        SentencePieceType[] types =
        [
            SentencePieceType.Control,
            SentencePieceType.Control,
            SentencePieceType.Control,
            SentencePieceType.Unknown,
            .. Enumerable.Repeat(SentencePieceType.Normal, 13),
            SentencePieceType.Control,
        ];
        var tokenizer = new SentencePieceTokenizer(
            new SentencePieceVocabulary(pieces, types, UnkId: 3, BosId: 0, EosId: 2, PadId: 1));

        TokenizationResult result = tokenizer.Encode("a<s>b<pad>a</s>b<unk>a<mask>b");

        foreach (int id in (int[])[0, 1, 2, 3, 17])
        {
            Assert.DoesNotContain(pieces[id].Piece, result.Tokens);
            Assert.DoesNotContain(id, result.Ids);
        }
    }

    [Fact]
    public void A_vocabulary_whose_pieces_and_types_disagree_is_rejected()
    {
        var vocabulary = new SentencePieceVocabulary(
            [new SentencePiece("a", -1.0, 0)],
            [SentencePieceType.Normal, SentencePieceType.Normal],
            UnkId: 0,
            BosId: -1,
            EosId: -1,
            PadId: -1);

        ArgumentException error = Assert.Throws<ArgumentException>(() => new SentencePieceTokenizer(vocabulary));

        Assert.Contains("1 pieces but 2 types", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_vocabulary_whose_unknown_id_is_out_of_range_is_rejected()
    {
        var vocabulary = new SentencePieceVocabulary(
            [new SentencePiece("a", -1.0, 0)],
            [SentencePieceType.Normal],
            UnkId: 7,
            BosId: -1,
            EosId: -1,
            PadId: -1);

        ArgumentException error = Assert.Throws<ArgumentException>(() => new SentencePieceTokenizer(vocabulary));

        Assert.Contains("outside the vocabulary range", error.Message, StringComparison.Ordinal);
    }
}
