using System.Text;
using System.Text.Json;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests.Persistence;

/// <summary>
/// Replays <c>spiece_model.json</c> against the real <c>tiny_sp.model</c>: the
/// hand-written protobuf reader must recover exactly what
/// <c>sentencepiece_model_pb2</c> recovers, down to the piece types the id-based
/// guess used to invent.
/// </summary>
public sealed class SentencePieceModelLoaderTests
{
    private const double ScoreTolerance = 1e-9;

    [Fact]
    public void Load_recovers_every_piece_score_and_type()
    {
        using JsonDocument doc = OracleLoader.Load("spiece_model.json");
        JsonElement meta = doc.RootElement.GetProperty("metadata");
        SentencePieceVocabulary vocabulary = LoadModel();

        JsonElement expectedPieces = meta.GetProperty("pieces");
        Assert.Equal(expectedPieces.GetArrayLength(), vocabulary.Count);

        var failures = new List<string>();
        int index = 0;
        foreach (JsonElement expected in expectedPieces.EnumerateArray())
        {
            SentencePiece actual = vocabulary.Pieces[index];
            string expectedPiece = expected.GetProperty("piece").GetString()!;
            double expectedScore = expected.GetProperty("score").GetDouble();
            int expectedType = expected.GetProperty("type").GetInt32();

            if (!string.Equals(expectedPiece, actual.Piece, StringComparison.Ordinal))
            {
                failures.Add($"id {index}: piece '{expectedPiece}' != '{actual.Piece}'");
            }
            if (Math.Abs(expectedScore - actual.Score) > ScoreTolerance)
            {
                failures.Add($"id {index}: score {expectedScore} != {actual.Score}");
            }
            if (expectedType != (int)vocabulary.Types[index])
            {
                failures.Add($"id {index}: type {expectedType} != {(int)vocabulary.Types[index]}");
            }
            if (actual.Id != index)
            {
                failures.Add($"id {index}: id field is {actual.Id}");
            }
            index++;
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    [Fact]
    public void Load_recovers_the_special_token_ids_from_the_trainer_spec()
    {
        using JsonDocument doc = OracleLoader.Load("spiece_model.json");
        JsonElement meta = doc.RootElement.GetProperty("metadata");
        SentencePieceVocabulary vocabulary = LoadModel();

        Assert.Equal(meta.GetProperty("unk_id").GetInt32(), vocabulary.UnkId);
        Assert.Equal(meta.GetProperty("bos_id").GetInt32(), vocabulary.BosId);
        Assert.Equal(meta.GetProperty("eos_id").GetInt32(), vocabulary.EosId);
        Assert.Equal(meta.GetProperty("pad_id").GetInt32(), vocabulary.PadId);
    }

    [Fact]
    public void The_loaded_vocabulary_drives_the_tokenizer_to_the_reference_encoding()
    {
        using JsonDocument doc = OracleLoader.Load("spiece_model.json");
        var tokenizer = new SentencePieceTokenizer(LoadModel());

        OracleReplay.AssertEncodings(doc, tokenizer.Encode, "pieces");
    }

    [Fact]
    public void Control_and_unknown_pieces_are_excluded_from_matching()
    {
        SentencePieceVocabulary vocabulary = LoadModel();

        for (int id = 0; id < vocabulary.Count; id++)
        {
            bool isMarker = vocabulary.Types[id] is SentencePieceType.Control or SentencePieceType.Unknown;
            Assert.Equal(!isMarker, vocabulary.IsMatchable(id));
        }
    }

    [Fact]
    public void Load_leaves_the_caller_s_stream_open()
    {
        using FileStream file = File.OpenRead(ModelPath);

        _ = SentencePieceModelLoader.Load(file);

        Assert.True(file.CanRead);
    }

    [Fact]
    public async Task LoadAsync_reads_the_same_vocabulary()
    {
        using FileStream file = File.OpenRead(ModelPath);

        SentencePieceVocabulary vocabulary = await SentencePieceModelLoader.LoadAsync(file);

        Assert.Equal(LoadModel().Count, vocabulary.Count);
    }

    [Fact]
    public void A_truncated_model_is_rejected()
    {
        byte[] bytes = File.ReadAllBytes(ModelPath);
        using var stream = new MemoryStream(bytes, 0, bytes.Length / 3);

        InvalidDataException error = Assert.Throws<InvalidDataException>(() => SentencePieceModelLoader.Load(stream));

        Assert.Contains("not a well-formed protocol-buffers message", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void Input_that_is_not_a_protobuf_message_is_rejected()
    {
        using var stream = new MemoryStream([0xFF, 0xFF, 0xFF, 0xFF, 0xFF]);

        Assert.Throws<InvalidDataException>(() => SentencePieceModelLoader.Load(stream));
    }

    [Fact]
    public void An_empty_model_is_rejected()
    {
        using var stream = new MemoryStream([]);

        InvalidDataException error = Assert.Throws<InvalidDataException>(() => SentencePieceModelLoader.Load(stream));

        Assert.Contains("declares no pieces", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_model_over_the_byte_limit_is_rejected()
    {
        using FileStream file = File.OpenRead(ModelPath);

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => SentencePieceModelLoader.Load(file, new ArtifactLoadOptions { MaxTotalBytes = 64 }));

        Assert.Contains("MaxTotalBytes", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_model_over_the_vocabulary_limit_is_rejected()
    {
        using FileStream file = File.OpenRead(ModelPath);

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => SentencePieceModelLoader.Load(file, new ArtifactLoadOptions { MaxVocabularySize = 10 }));

        Assert.Contains("MaxVocabularySize", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void The_oracle_confirms_the_model_uses_the_identity_normalizer()
    {
        using JsonDocument doc = OracleLoader.Load("spiece_model.json");
        JsonElement meta = doc.RootElement.GetProperty("metadata");

        // The loader refuses anything else, so the fixture must be the case it accepts.
        Assert.Equal("identity", meta.GetProperty("normalizer_name").GetString());
        Assert.True(meta.GetProperty("add_dummy_prefix").GetBoolean());
        Assert.True(meta.GetProperty("remove_extra_whitespaces").GetBoolean());
        Assert.True(meta.GetProperty("escape_whitespaces").GetBoolean());
    }

    private static string ModelPath => Path.Combine(AppContext.BaseDirectory, "oracles", "tiny_sp.model");

    private static SentencePieceVocabulary LoadModel() => SentencePieceModelLoader.Load(ModelPath);

    // ---- The trainer_spec settings that decide whether the file is reproducible ----
    //
    // tiny_sp.model is a plain unigram model without byte_fallback, so it sits on the
    // accepting side of both checks and cannot catch either. These synthesize the
    // rejecting side: a piece table alone looks perfectly valid, which is exactly what
    // makes an unread model_type dangerous — the vocabulary loads and tokenizes wrong.

    [Theory]
    [InlineData(2, "BPE")]
    [InlineData(3, "WORD")]
    [InlineData(4, "CHAR")]
    public void A_model_trained_with_another_algorithm_is_rejected(int modelType, string expectedName)
    {
        using var stream = new MemoryStream(SyntheticModel(modelType: modelType));

        InvalidDataException error = Assert.Throws<InvalidDataException>(() => SentencePieceModelLoader.Load(stream));

        Assert.Contains(expectedName, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_model_that_declares_the_unigram_algorithm_still_loads()
    {
        using var stream = new MemoryStream(SyntheticModel(modelType: 1));

        SentencePieceVocabulary vocabulary = SentencePieceModelLoader.Load(stream);

        Assert.Equal(3, vocabulary.Count);
    }

    [Fact]
    public void A_model_trained_with_byte_fallback_is_rejected()
    {
        using var stream = new MemoryStream(SyntheticModel(modelType: 1, byteFallback: true));

        InvalidDataException error = Assert.Throws<InvalidDataException>(() => SentencePieceModelLoader.Load(stream));

        Assert.Contains("byte_fallback", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_piece_longer_than_the_token_limit_is_rejected()
    {
        using var stream = new MemoryStream(SyntheticModel(modelType: 1, extraPiece: new string('a', 40)));

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => SentencePieceModelLoader.Load(stream, new ArtifactLoadOptions { MaxTokenLength = 8 }));

        Assert.Contains("MaxTokenLength", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_model_with_no_normalizer_spec_is_rejected()
    {
        // Otherwise the normalizer check is skippable by deleting a field.
        using var stream = new MemoryStream(SyntheticModel(modelType: 1, normalizerSpec: false));

        InvalidDataException error = Assert.Throws<InvalidDataException>(() => SentencePieceModelLoader.Load(stream));

        Assert.Contains("no normalizer_spec", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_special_token_id_outside_the_vocabulary_is_rejected()
    {
        // The tokenizer never indexes by bos_id, but a caller naming the sentence
        // markers does, and would meet the bad id far from the file that carried it.
        using var stream = new MemoryStream(SyntheticModel(modelType: 1, bosId: 5000));

        InvalidDataException error = Assert.Throws<InvalidDataException>(() => SentencePieceModelLoader.Load(stream));

        Assert.Contains("bos_id 5000", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_special_token_id_of_minus_one_means_the_model_has_none()
    {
        using var stream = new MemoryStream(SyntheticModel(modelType: 1, bosId: -1));

        SentencePieceVocabulary vocabulary = SentencePieceModelLoader.Load(stream);

        Assert.Equal(-1, vocabulary.BosId);
    }

    /// <summary>
    /// Builds a minimal <c>ModelProto</c>: three pieces, an identity
    /// <c>normalizer_spec</c>, plus whichever <c>trainer_spec</c> fields the test is
    /// about. Hand-encoded because the point is to exercise the hand-written reader
    /// against bytes it has never seen.
    /// </summary>
    private static byte[] SyntheticModel(
        int? modelType = null,
        bool? byteFallback = null,
        string? extraPiece = null,
        int? bosId = null,
        bool normalizerSpec = true)
    {
        var body = new List<byte>();
        foreach (string piece in new[] { "<unk>", "▁a", extraPiece ?? "▁b" })
        {
            AppendLengthDelimited(body, fieldNumber: 1, PieceMessage(piece));
        }

        var trainer = new List<byte>();
        if (modelType is int type)
        {
            AppendVarintField(trainer, fieldNumber: 3, (ulong)type);
        }
        if (byteFallback is bool fallback)
        {
            AppendVarintField(trainer, fieldNumber: 35, fallback ? 1UL : 0UL);
        }
        if (bosId is int bos)
        {
            AppendVarintField(trainer, fieldNumber: 41, (ulong)bos);
        }
        if (trainer.Count > 0)
        {
            AppendLengthDelimited(body, fieldNumber: 2, trainer.ToArray());
        }

        if (normalizerSpec)
        {
            var normalizer = new List<byte>();
            AppendLengthDelimited(normalizer, fieldNumber: 1, Encoding.UTF8.GetBytes("identity"));
            AppendLengthDelimited(body, fieldNumber: 3, normalizer.ToArray());
        }
        return body.ToArray();
    }

    private static byte[] PieceMessage(string text)
    {
        var message = new List<byte>();
        AppendLengthDelimited(message, fieldNumber: 1, Encoding.UTF8.GetBytes(text));

        byte[] score = BitConverter.GetBytes(-1.5f);
        if (!BitConverter.IsLittleEndian)
        {
            Array.Reverse(score);
        }
        AppendTag(message, fieldNumber: 2, wireType: 5);
        message.AddRange(score);
        return message.ToArray();
    }

    private static void AppendLengthDelimited(List<byte> target, int fieldNumber, byte[] payload)
    {
        AppendTag(target, fieldNumber, wireType: 2);
        AppendVarint(target, (ulong)payload.Length);
        target.AddRange(payload);
    }

    private static void AppendVarintField(List<byte> target, int fieldNumber, ulong value)
    {
        AppendTag(target, fieldNumber, wireType: 0);
        AppendVarint(target, value);
    }

    private static void AppendTag(List<byte> target, int fieldNumber, int wireType) =>
        AppendVarint(target, ((ulong)fieldNumber << 3) | (uint)wireType);

    private static void AppendVarint(List<byte> target, ulong value)
    {
        while (value >= 0x80)
        {
            target.Add((byte)(value | 0x80));
            value >>= 7;
        }
        target.Add((byte)value);
    }
}
