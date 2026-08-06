using System.Text.Json;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests;

/// <summary>
/// Replays <c>xlmr_fairseq.json</c> against the XLM-R vocabulary itself — the
/// corpus issue #63 asked for, and the one the id-based control filter could not
/// have passed.
/// </summary>
/// <remarks>
/// <para>
/// <c>xlmr_fairseq.model</c> carries xlm-roberta-base's own 250 000 pieces and
/// scores, at the ids HuggingFace gives them, laid out the way
/// <c>XLMRobertaTokenizer</c> lays them out: <c>&lt;s&gt;</c>=0,
/// <c>&lt;pad&gt;</c>=1, <c>&lt;/s&gt;</c>=2, <c>&lt;unk&gt;</c>=3 and
/// <c>&lt;mask&gt;</c>=250001. Every marker but one sits outside the 0-2 window
/// the old constructor tested, and <c>&lt;mask&gt;</c> sits a quarter of a
/// million ids away from it.
/// </para>
/// <para>
/// Since #75 the fixture also carries XLM-R's own <c>nmt_nfkc</c> character map,
/// which <c>tools/fetch_xlmr_vocab.py</c> used to overwrite with <c>identity</c>
/// because the loader refused anything else. It is therefore the stock XLM-R
/// pipeline with the vocabulary relabelled, and the last six oracle inputs — full
/// width forms, ligatures, decomposed accents, exotic spaces, control characters
/// — are segmented through a normalization pass rather than past an inert one.
/// See <c>docs/decisions/0014-precompiled-normalizer.md</c>.
/// </para>
/// </remarks>
public sealed class XlmRobertaFairseqTests
{
    private const double ScoreTolerance = 1e-9;

    // 5 MB of protobuf, parsed once for the whole class rather than per test.
    private static readonly Lazy<SentencePieceVocabulary> Vocabulary = new(() =>
        SentencePieceModelLoader.Load(Path.Combine(AppContext.BaseDirectory, "oracles", "xlmr_fairseq.model")));

    [Fact]
    public void The_vocabulary_is_in_fairseq_layout()
    {
        using JsonDocument doc = OracleLoader.Load("xlmr_fairseq.json");
        JsonElement meta = doc.RootElement.GetProperty("metadata");
        SentencePieceVocabulary vocabulary = Vocabulary.Value;

        Assert.Equal(meta.GetProperty("vocab_size").GetInt32(), vocabulary.Count);
        Assert.Equal(meta.GetProperty("unk_id").GetInt32(), vocabulary.UnkId);
        Assert.Equal(meta.GetProperty("bos_id").GetInt32(), vocabulary.BosId);
        Assert.Equal(meta.GetProperty("eos_id").GetInt32(), vocabulary.EosId);
        Assert.Equal(meta.GetProperty("pad_id").GetInt32(), vocabulary.PadId);
    }

    /// <summary>
    /// The property the whole issue is about, stated over the real vocabulary:
    /// each of the five markers is where sentencepiece says it is, carries the
    /// type sentencepiece gives it, and is excluded from the matching table.
    /// </summary>
    [Fact]
    public void None_of_the_five_markers_is_matchable()
    {
        using JsonDocument doc = OracleLoader.Load("xlmr_fairseq.json");
        SentencePieceVocabulary vocabulary = Vocabulary.Value;

        var failures = new List<string>();
        foreach (JsonElement marker in doc.RootElement.GetProperty("metadata").GetProperty("markers").EnumerateArray())
        {
            string piece = marker.GetProperty("piece").GetString()!;
            int id = marker.GetProperty("id").GetInt32();
            int type = marker.GetProperty("type").GetInt32();

            if (!string.Equals(piece, vocabulary.Pieces[id].Piece, StringComparison.Ordinal))
            {
                failures.Add($"{piece} is at id {id} in the oracle but '{vocabulary.Pieces[id].Piece}' is there");
            }
            if (type != (int)vocabulary.Types[id])
            {
                failures.Add($"{piece}: type {type} != {(int)vocabulary.Types[id]}");
            }
            if (vocabulary.IsMatchable(id))
            {
                failures.Add($"{piece} (id {id}) is matchable against text");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public void Sampled_pieces_carry_their_score_and_type()
    {
        using JsonDocument doc = OracleLoader.Load("xlmr_fairseq.json");
        SentencePieceVocabulary vocabulary = Vocabulary.Value;

        var failures = new List<string>();
        foreach (JsonElement sample in doc.RootElement.GetProperty("metadata").GetProperty("sampled_pieces").EnumerateArray())
        {
            int id = sample.GetProperty("id").GetInt32();
            SentencePiece actual = vocabulary.Pieces[id];
            string expectedPiece = sample.GetProperty("piece").GetString()!;
            double expectedScore = sample.GetProperty("score").GetDouble();
            int expectedType = sample.GetProperty("type").GetInt32();

            if (!string.Equals(expectedPiece, actual.Piece, StringComparison.Ordinal))
            {
                failures.Add($"id {id}: piece '{expectedPiece}' != '{actual.Piece}'");
            }
            if (Math.Abs(expectedScore - actual.Score) > ScoreTolerance)
            {
                failures.Add($"id {id}: score {expectedScore} != {actual.Score}");
            }
            if (expectedType != (int)vocabulary.Types[id])
            {
                failures.Add($"id {id}: type {expectedType} != {(int)vocabulary.Types[id]}");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public void Encode_matches_sentencepiece_over_the_xlmr_vocabulary()
    {
        using JsonDocument doc = OracleLoader.Load("xlmr_fairseq.json");
        var tokenizer = new SentencePieceTokenizer(Vocabulary.Value);

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
    /// The end-to-end guarantee, asserted directly rather than left implied by
    /// the parity replay: whatever a document says, encoding it never lands on a
    /// marker. Several oracle inputs name all five markers literally, which is
    /// the only way this assertion can fail.
    /// </summary>
    [Fact]
    public void Encode_never_emits_a_marker_however_the_text_spells_it()
    {
        using JsonDocument doc = OracleLoader.Load("xlmr_fairseq.json");
        JsonElement markers = doc.RootElement.GetProperty("metadata").GetProperty("markers");
        var tokenizer = new SentencePieceTokenizer(Vocabulary.Value);

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string text = c.GetProperty("text").GetString()!;
            TokenizationResult actual = tokenizer.Encode(text);
            foreach (JsonElement marker in markers.EnumerateArray())
            {
                string piece = marker.GetProperty("piece").GetString()!;
                int id = marker.GetProperty("id").GetInt32();
                if (actual.Tokens.Contains(piece, StringComparer.Ordinal))
                {
                    failures.Add($"\"{text}\" emitted the piece {piece}");
                }
                if (actual.Ids.Contains(id))
                {
                    failures.Add($"\"{text}\" emitted the id {id} ({piece})");
                }
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }
}
