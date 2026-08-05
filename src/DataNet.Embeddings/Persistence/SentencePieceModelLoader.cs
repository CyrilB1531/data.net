using System.Text;
using DataNet.Embeddings.Tokenization;
using DataNet.Internal.Persistence;

namespace DataNet.Embeddings.Persistence;

/// <summary>
/// Reads a SentencePiece <c>spiece.model</c> — the trained unigram vocabulary,
/// its scores, its piece types and the model's special-token ids.
/// </summary>
/// <remarks>
/// <para>
/// Matches <c>sentencepiece.SentencePieceProcessor(model_file=…)</c>: the pieces
/// and scores it exposes through <c>id_to_piece</c> / <c>get_score</c>, and the
/// ids it exposes through <c>unk_id()</c>, <c>bos_id()</c>, <c>eos_id()</c> and
/// <c>pad_id()</c>. Scores are stored as 32-bit floats in the file and widened,
/// exactly as the Python binding does, so the values compare equal.
/// </para>
/// <para>
/// <strong>Only identity-normalized models load.</strong>
/// <see cref="SentencePieceTokenizer"/> reproduces the <c>identity</c> normalizer
/// with <c>add_dummy_prefix</c>; a model trained with <c>nmt_nfkc</c> or any
/// precompiled character map would tokenize differently here than it does in
/// Python. Rather than return a vocabulary that silently produces the wrong
/// embeddings, this loader refuses the file and names the normalizer it found.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// SentencePieceVocabulary vocab = SentencePieceModelLoader.Load("spiece.model");
/// var tokenizer = new SentencePieceTokenizer(vocab);
/// </code>
/// </example>
public static class SentencePieceModelLoader
{
    // ModelProto field numbers, from sentencepiece_model.proto.
    private const int FieldPieces = 1;
    private const int FieldTrainerSpec = 2;
    private const int FieldNormalizerSpec = 3;

    // ModelProto.SentencePiece field numbers.
    private const int PieceFieldPiece = 1;
    private const int PieceFieldScore = 2;
    private const int PieceFieldType = 3;

    // TrainerSpec field numbers: the algorithm, then the special-token ids.
    private const int TrainerFieldModelType = 3;
    private const int TrainerFieldByteFallback = 35;
    private const int TrainerFieldUnkId = 40;
    private const int TrainerFieldBosId = 41;
    private const int TrainerFieldEosId = 42;
    private const int TrainerFieldPadId = 43;

    // TrainerSpec.ModelType values, from sentencepiece_model.proto.
    private const int ModelTypeUnigram = 1;

    // NormalizerSpec field numbers.
    private const int NormalizerFieldName = 1;
    private const int NormalizerFieldCharsMap = 2;
    private const int NormalizerFieldAddDummyPrefix = 3;
    private const int NormalizerFieldRemoveExtraWhitespaces = 4;
    private const int NormalizerFieldEscapeWhitespaces = 5;

    /// <summary>Reads a vocabulary from <paramref name="source"/>.</summary>
    /// <param name="source">The <c>spiece.model</c> bytes; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="InvalidDataException">The model is malformed, exceeds a limit, or uses a normalizer this library does not reproduce.</exception>
    public static SentencePieceVocabulary Load(Stream source, ArtifactLoadOptions? options = null)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        return Parse(JsonArtifact.ReadAllBytes(source, limits), limits);
    }

    /// <summary>Reads a vocabulary from the file at <paramref name="path"/>.</summary>
    /// <param name="path">Path to a <c>spiece.model</c>.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="InvalidDataException">The model is malformed, exceeds a limit, or uses a normalizer this library does not reproduce.</exception>
    public static SentencePieceVocabulary Load(string path, ArtifactLoadOptions? options = null)
    {
        using FileStream file = JsonArtifact.OpenRead(path);
        return Load(file, options);
    }

    /// <summary>Asynchronous counterpart of <see cref="Load(Stream, ArtifactLoadOptions?)"/>.</summary>
    /// <param name="source">The <c>spiece.model</c> bytes; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    public static async Task<SentencePieceVocabulary> LoadAsync(
        Stream source,
        ArtifactLoadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        byte[] payload = await JsonArtifact.ReadAllBytesAsync(source, limits, cancellationToken).ConfigureAwait(false);
        return Parse(payload, limits);
    }

    private static SentencePieceVocabulary Parse(byte[] payload, in ArtifactLimits limits)
    {
        var pieces = new List<SentencePiece>();
        var types = new List<SentencePieceType>();
        int unkId = 0;
        int bosId = 1;
        int eosId = 2;
        int padId = -1;

        bool sawNormalizerSpec = false;

        var reader = new ProtobufReader(payload);
        while (reader.TryReadTag(out int field, out int wireType))
        {
            // Every section of a ModelProto is length-delimited, so anything else is
            // a field this reader has no interest in.
            if (wireType != ProtobufReader.WireLengthDelimited)
            {
                reader.SkipField(wireType);
                continue;
            }
            switch (field)
            {
                case FieldPieces:
                    ReadPiece(reader.ReadLengthDelimited(), pieces, types, limits);
                    limits.CheckVocabularySize(pieces.Count);
                    break;
                case FieldTrainerSpec:
                    ReadTrainerSpec(reader.ReadLengthDelimited(), ref unkId, ref bosId, ref eosId, ref padId);
                    break;
                case FieldNormalizerSpec:
                    sawNormalizerSpec = true;
                    EnsureNormalizerIsReproduced(reader.ReadLengthDelimited());
                    break;
                default:
                    reader.SkipField(wireType);
                    break;
            }
        }

        if (pieces.Count == 0)
        {
            throw new InvalidDataException("The SentencePiece model declares no pieces.");
        }
        if (!sawNormalizerSpec)
        {
            // Every model sentencepiece writes carries one. Treating "absent" as
            // "identity" would make the normalizer check skippable by deleting a
            // field, which is the one thing it exists to prevent.
            throw Unsupported(
                "it declares no normalizer_spec",
                "the normalizer decides how text is preprocessed and cannot be assumed");
        }
        CheckSpecialId(unkId, pieces.Count, "unk_id", optional: false);
        CheckSpecialId(bosId, pieces.Count, "bos_id", optional: true);
        CheckSpecialId(eosId, pieces.Count, "eos_id", optional: true);
        CheckSpecialId(padId, pieces.Count, "pad_id", optional: true);
        return new SentencePieceVocabulary(pieces, types, unkId, bosId, eosId, padId);
    }

    /// <summary>
    /// Checks a special-token id against the vocabulary it indexes.
    /// </summary>
    /// <remarks>
    /// The tokenizer never indexes by <c>bos_id</c>, <c>eos_id</c> or <c>pad_id</c>,
    /// but callers reasonably do — <c>vocab.Pieces[vocab.BosId]</c> is the obvious
    /// way to name the sentence markers. An id a crafted <c>trainer_spec</c> put
    /// out of range would surface there, far from the file that carried it.
    /// <c>-1</c> is how the format spells "this model has none".
    /// </remarks>
    private static void CheckSpecialId(int id, int pieceCount, string name, bool optional)
    {
        if (optional && id == -1)
        {
            return;
        }
        if (id < 0 || id >= pieceCount)
        {
            throw new InvalidDataException(
                $"The SentencePiece model declares {name} {id}, outside its own vocabulary range [0, {pieceCount}).");
        }
    }

    private static void ReadPiece(
        ReadOnlySpan<byte> message,
        List<SentencePiece> pieces,
        List<SentencePieceType> types,
        in ArtifactLimits limits)
    {
        string? piece = null;
        double score = 0;
        var type = SentencePieceType.Normal;

        var reader = new ProtobufReader(message);
        while (reader.TryReadTag(out int field, out int wireType))
        {
            if (field == PieceFieldPiece && wireType == ProtobufReader.WireLengthDelimited)
            {
                ReadOnlySpan<byte> raw = reader.ReadLengthDelimited();
                // UTF-8 spends at most three bytes per UTF-16 code unit, so a run
                // this long cannot decode to a string within the limit. Checking it
                // here refuses the hostile case before the decode allocates it;
                // the exact check below is what the limit actually means.
                limits.CheckTokenLength((raw.Length + 2) / 3);
                piece = DecodeUtf8(raw);
                limits.CheckTokenLength(piece.Length);
            }
            else if (field == PieceFieldScore && wireType == ProtobufReader.WireFixed32)
            {
                // Stored as a 32-bit float; widening it is what the Python binding
                // does too, so the two agree bit for bit.
                score = reader.ReadFloat();
            }
            else if (field == PieceFieldType && wireType == ProtobufReader.WireVarint)
            {
                type = ToPieceType(reader.ReadInt32());
            }
            else
            {
                reader.SkipField(wireType);
            }
        }

        if (piece is null)
        {
            throw new InvalidDataException($"The SentencePiece model has a piece with no string at id {pieces.Count}.");
        }
        pieces.Add(new SentencePiece(piece, score, pieces.Count));
        types.Add(type);
    }

    private static void ReadTrainerSpec(ReadOnlySpan<byte> message, ref int unkId, ref int bosId, ref int eosId, ref int padId)
    {
        var reader = new ProtobufReader(message);
        while (reader.TryReadTag(out int field, out int wireType))
        {
            if (wireType != ProtobufReader.WireVarint)
            {
                reader.SkipField(wireType);
                continue;
            }
            switch (field)
            {
                case TrainerFieldModelType:
                    EnsureModelTypeIsReproduced(reader.ReadInt32());
                    break;
                case TrainerFieldByteFallback:
                    EnsureByteFallbackIsOff(reader.ReadVarint() != 0);
                    break;
                case TrainerFieldUnkId:
                    unkId = reader.ReadInt32();
                    break;
                case TrainerFieldBosId:
                    bosId = reader.ReadInt32();
                    break;
                case TrainerFieldEosId:
                    eosId = reader.ReadInt32();
                    break;
                case TrainerFieldPadId:
                    padId = reader.ReadInt32();
                    break;
                default:
                    reader.SkipField(wireType);
                    break;
            }
        }
    }

    /// <summary>
    /// Refuses a model trained with an algorithm other than unigram.
    /// </summary>
    /// <remarks>
    /// <see cref="SentencePieceTokenizer"/> segments by unigram Viterbi decoding
    /// over the piece scores. A BPE, WORD or CHAR model carries a piece table that
    /// decoding would happily consume and tokenize the wrong way — the most
    /// damaging kind of silent mismatch, because the vocabulary itself looks fine.
    /// </remarks>
    private static void EnsureModelTypeIsReproduced(int modelType)
    {
        if (modelType != ModelTypeUnigram)
        {
            throw Unsupported(
                $"it was trained as a {NameOfModelType(modelType)} model",
                "the tokenizer decodes by unigram Viterbi, which reproduces UNIGRAM models only");
        }
    }

    private static void EnsureByteFallbackIsOff(bool byteFallback)
    {
        if (byteFallback)
        {
            throw Unsupported(
                "it was trained with byte_fallback",
                "Python resolves an uncovered character into <0x..> byte pieces where this tokenizer emits the unknown piece");
        }
    }

    private static string NameOfModelType(int value) => value switch
    {
        2 => "BPE",
        3 => "WORD",
        4 => "CHAR",
        _ => $"type-{value}",
    };

    private static void EnsureNormalizerIsReproduced(ReadOnlySpan<byte> message)
    {
        string name = "identity";
        bool hasCharsMap = false;
        bool addDummyPrefix = true;
        bool removeExtraWhitespaces = true;
        bool escapeWhitespaces = true;

        var reader = new ProtobufReader(message);
        while (reader.TryReadTag(out int field, out int wireType))
        {
            switch (field)
            {
                case NormalizerFieldName when wireType == ProtobufReader.WireLengthDelimited:
                    name = DecodeUtf8(reader.ReadLengthDelimited());
                    break;
                case NormalizerFieldCharsMap when wireType == ProtobufReader.WireLengthDelimited:
                    hasCharsMap = reader.ReadLengthDelimited().Length > 0;
                    break;
                case NormalizerFieldAddDummyPrefix when wireType == ProtobufReader.WireVarint:
                    addDummyPrefix = reader.ReadVarint() != 0;
                    break;
                case NormalizerFieldRemoveExtraWhitespaces when wireType == ProtobufReader.WireVarint:
                    removeExtraWhitespaces = reader.ReadVarint() != 0;
                    break;
                case NormalizerFieldEscapeWhitespaces when wireType == ProtobufReader.WireVarint:
                    escapeWhitespaces = reader.ReadVarint() != 0;
                    break;
                default:
                    reader.SkipField(wireType);
                    break;
            }
        }

        if (!string.Equals(name, "identity", StringComparison.Ordinal) || hasCharsMap)
        {
            throw Unsupported(
                $"it was trained with the '{name}' normalizer",
                "only the identity normalizer is reproduced, so any other would tokenize differently here than in Python");
        }
        if (!addDummyPrefix)
        {
            throw Unsupported("add_dummy_prefix is off", "the tokenizer always prepends the meta symbol");
        }
        if (!removeExtraWhitespaces)
        {
            throw Unsupported("remove_extra_whitespaces is off", "the tokenizer always collapses whitespace runs");
        }
        if (!escapeWhitespaces)
        {
            throw Unsupported("escape_whitespaces is off", "the tokenizer always maps spaces to U+2581");
        }
    }

    private static InvalidDataException Unsupported(string found, string why) =>
        new($"This SentencePiece model cannot be loaded because {found}: {why}. " +
            "Loading it anyway would produce embeddings that do not match the model.");

    private static SentencePieceType ToPieceType(int value) => value switch
    {
        1 => SentencePieceType.Normal,
        2 => SentencePieceType.Unknown,
        3 => SentencePieceType.Control,
        4 => SentencePieceType.UserDefined,
        5 => SentencePieceType.Unused,
        6 => SentencePieceType.Byte,
        _ => throw new InvalidDataException($"The SentencePiece model declares the unknown piece type {value}."),
    };

    private static string DecodeUtf8(ReadOnlySpan<byte> bytes)
    {
        // JsonArtifact.Utf8NoBom throws on invalid bytes rather than substituting
        // U+FFFD, matching VocabTxtLoader: a piece that is not valid UTF-8 means the
        // file is corrupt, and a replacement character would silently become part of
        // the vocabulary.
#if NETSTANDARD2_0
        byte[] copy = bytes.ToArray();
        return JsonArtifact.Utf8NoBom.GetString(copy, 0, copy.Length);
#else
        return JsonArtifact.Utf8NoBom.GetString(bytes);
#endif
    }
}
