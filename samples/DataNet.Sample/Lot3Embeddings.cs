using System.Text;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Pooling;
using DataNet.Embeddings.Search;
using DataNet.Embeddings.Tokenization;

namespace DataNet.Sample;

/// <summary>
/// Lot 3 — tokenize, pool, index. ONNX inference is the one thing missing:
/// <c>OnnxTextEmbedder</c> needs model weights, and weights are deliberately
/// never committed, so the vectors pooled below are synthetic. That exclusion is
/// declared in PackagingGate rather than left implicit.
/// </summary>
internal static class Lot3Embeddings
{
    private const string Unknown = "[UNK]";

    /// <summary>Two words the four-entry vocabulary below covers: token + ##ize, text.</summary>
    private const string SampleText = "tokenize text";

    public static void Run()
    {
        Console.WriteLine("lot 3 — embeddings (tokenizers, pooling, search)");

        var bounds = new ArtifactLoadOptions
        {
            MaxTotalBytes = 8L * 1024 * 1024,
            MaxVocabularySize = 100_000,
            MaxTokenLength = 512,
            MaxArrayLength = 100_000,
            MaxJsonDepth = 32,
        };

        // WordPiece from a plain dictionary, the shortest path from nothing to tokens.
        var vocab = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [Unknown] = 0,
            ["token"] = 1,
            ["##ize"] = 2,
            ["text"] = 3,
        };
        var inline = new WordPieceTokenizer(vocab, unkToken: Unknown, continuationPrefix: "##", maxCharsPerWord: 100, lowercase: true);
        TokenizationResult encoded = inline.Encode(SampleText);
        Console.WriteLine($"  WordPiece inline : [{string.Join(", ", encoded.Tokens)}] -> [{string.Join(", ", encoded.Ids)}]");
        Console.WriteLine($"  EncodeToIds      : [{string.Join(", ", inline.EncodeToIds(SampleText))}]");

        // The same vocabulary as a consumer would actually get it: a vocab.txt.
        WordPieceVocabulary fromTxt = VocabTxtLoader.Load(
            Utf8("[UNK]\ntoken\n##ize\ntext"), bounds, unkToken: Unknown, continuationPrefix: "##", lowercase: true);
        Console.WriteLine($"  vocab.txt        : {fromTxt.Count} tokens, unk='{fromTxt.UnkToken}', "
            + $"prefix='{fromTxt.ContinuationPrefix}', lowercase={fromTxt.Lowercase}, dict={fromTxt.Vocab.Count}");
        Console.WriteLine($"  from vocabulary  : [{string.Join(", ", new WordPieceTokenizer(fromTxt, maxCharsPerWord: 100).Encode(SampleText).Tokens)}]");

        // …and as HuggingFace ships it: a tokenizer.json.
        WordPieceVocabulary fromJson = TokenizerJsonLoader.LoadWordPiece(Utf8(WordPieceJson), bounds);
        Console.WriteLine($"  tokenizer.json   : {fromJson.Count} WordPiece tokens");

        // SentencePiece, three ways in: hand-built pieces, tokenizer.json, spiece.model.
        SentencePiece[] pieces =
        [
            new SentencePiece("<unk>", 0.0, 0),
            new SentencePiece("<s>", 0.0, 1),
            new SentencePiece("\u2581alpha", -1.5, 2),
            new SentencePiece("\u2581beta", -2.5, 3),
        ];
        SentencePieceType[] types =
        [
            SentencePieceType.Unknown,
            SentencePieceType.Control,
            SentencePieceType.Normal,
            SentencePieceType.Normal,
        ];
        var handBuilt = new SentencePieceVocabulary(pieces, types, UnkId: 0, BosId: 1, EosId: -1, PadId: -1);
        var sp = new SentencePieceTokenizer(handBuilt);
        Console.WriteLine($"  SentencePiece    : [{string.Join(", ", sp.Encode("alpha beta").Tokens)}]");
        Console.WriteLine($"  vocabulary       : {handBuilt.Count} pieces, types[0]={handBuilt.Types[0]}, "
            + $"piece[2]='{handBuilt.Pieces[2].Piece}' score={handBuilt.Pieces[2].Score:F1} id={handBuilt.Pieces[2].Id}, "
            + $"matchable(0)={handBuilt.IsMatchable(0)}, unk={handBuilt.UnkId} bos={handBuilt.BosId} eos={handBuilt.EosId} pad={handBuilt.PadId}");

        SentencePieceVocabulary fromUnigramJson = TokenizerJsonLoader.LoadUnigram(Utf8(UnigramJson), bounds);
        Console.WriteLine($"  unigram json     : {fromUnigramJson.Count} pieces");

        SentencePieceVocabulary fromModel = SentencePieceModelLoader.Load(new MemoryStream(SpieceModel()), bounds);
        Console.WriteLine($"  spiece.model     : {fromModel.Count} pieces, unk={fromModel.UnkId}");

        // A stock T5, ALBERT, camemBERT or XLM-R arrives with a character map in its
        // normalizer_spec, and Normalizer is what applies it before segmentation.
        // The model built below has none — a charsmap is compiled by sentencepiece,
        // and this sample commits no artifacts — so what it shows is the shape:
        // null means the text reaches the tokenizer as it was written.
        PrecompiledNormalizer? normalizer = fromModel.Normalizer;
        Console.WriteLine($"  normalizer       : {(normalizer is null ? "identity, no charsmap" : $"{normalizer.CharsMapLength} bytes")}");

        // Batch encoding: the special tokens, the truncation and the padding the
        // caller used to have to reproduce from memory. Everything the model is
        // fed comes out of the template and the vocabulary — nothing is a
        // hardcoded id, which is why [CLS] can sit at 4 here.
        var batchVocab = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [Unknown] = 0,
            ["token"] = 1,
            ["##ize"] = 2,
            ["text"] = 3,
            ["[CLS]"] = 4,
            ["[SEP]"] = 5,
            ["[PAD]"] = 6,
        };
        // CA1859 (use the concrete type for performance): declaring this as the
        // interface is the point. `BatchEncoder` takes an `ISubwordTokenizer`, and
        // PackagingGate demands a *member* reference to every exported type —
        // calling TryGetId on the concrete class emits one to WordPieceTokenizer
        // instead, and the gate fails with "ISubwordTokenizer has no member
        // referenced". Checked, not assumed. A sample is also the wrong place to
        // trade a demonstrated abstraction for an interface dispatch.
#pragma warning disable CA1859
        ISubwordTokenizer subword = new WordPieceTokenizer(batchVocab, unkToken: Unknown, lowercase: true);
#pragma warning restore CA1859
        Console.WriteLine($"  token_to_id      : [CLS]={(subword.TryGetId("[CLS]", out int clsId) ? clsId : -1)}, "
            + $"[MASK] present={subword.TryGetId("[MASK]", out _)}");

        var options = new EncodingOptions
        {
            Template = SpecialTokenTemplate.Bert,
            MaxLength = 8,
            Truncation = TruncationStrategy.LongestFirst,
            BatchSize = 32,
            SortByLength = true,
        };
        var batchEncoder = new BatchEncoder(subword, options);
        Console.WriteLine($"  template         : {options.Template.SpecialTokenCount} special tokens, "
            + $"pad='{options.Template.PadToken}', truncation={options.Truncation}, batch={options.BatchSize}");
        Console.WriteLine($"  Encode           : [{string.Join(", ", batchEncoder.Encode(SampleText))}]");

        // Two texts of different lengths: the batch is padded to the longer of
        // the two, never to MaxLength, and the mask marks what is padding.
        EncodedBatch batch = batchEncoder.EncodeBatch(["text", SampleText]);
        Console.WriteLine($"  EncodeBatch      : {batch.Count} sequences padded to {batch.SequenceLength}, "
            + $"lengths=[{string.Join(", ", batch.Lengths)}]");
        for (int row = 0; row < batch.Count; row++)
        {
            ReadOnlySpan<long> ids = batch.InputIds.Slice(row * batch.SequenceLength, batch.SequenceLength);
            ReadOnlySpan<long> mask = batch.AttentionMask.Slice(row * batch.SequenceLength, batch.SequenceLength);
            Console.WriteLine($"    ids=[{string.Join(", ", ids.ToArray())}] mask=[{string.Join(", ", mask.ToArray())}] "
                + $"unpadded=[{string.Join(", ", batch.Sequence(row).ToArray())}]");
        }

        // Pooling. Two tokens of three dimensions, the second one padding — the
        // attention mask is what keeps the padding out of the mean.
        float[] tokenEmbeddings = [1f, 0f, 0f, 9f, 9f, 9f];
        long[] attentionMask = [1, 0];
        float[] pooled = Pooler.MeanPool(tokenEmbeddings, seqLen: 2, dim: 3, attentionMask);
        Pooler.L2Normalize(pooled);
        float[] normalized = Pooler.MeanPoolAndNormalize(tokenEmbeddings, seqLen: 2, dim: 3, attentionMask);
        Console.WriteLine($"  MeanPool         : [{string.Join(", ", pooled.Select(v => v.ToString("F3")))}]");
        Console.WriteLine($"  MeanPool+L2      : [{string.Join(", ", normalized.Select(v => v.ToString("F3")))}]");
        Console.WriteLine($"  VectorMath       : dot={VectorMath.Dot(pooled, normalized):F3}, l2={VectorMath.L2Norm(pooled):F3}");

        // The batched form, over the [batch, seq, dim] tensor an encoder returns:
        // each row pooled against its own slice of the mask, so a shorter
        // sequence's padding cannot reach its vector.
        float[] batchedEmbeddings = [1f, 0f, 0f, 9f, 9f, 9f, 0f, 1f, 0f, 0f, 0f, 1f];
        long[] batchedMask = [1, 0, 1, 1];
        float[][] batchPooled = Pooler.MeanPoolAndNormalizeBatch(
            batchedEmbeddings, batchSize: 2, seqLen: 2, dim: 3, batchedMask);
        Console.WriteLine($"  MeanPoolBatch    : {batchPooled.Length} vectors, "
            + string.Join(" | ", batchPooled.Select(v => $"[{string.Join(", ", v.Select(c => c.ToString("F3")))}]")));

        // Nearest-neighbour search over those vectors.
        var index = new EmbeddingIndex(dimension: 3, normalize: true);
        index.Add([1f, 0f, 0f]);
        index.Add([0f, 1f, 0f]);
        index.Add([0.9f, 0.1f, 0f]);
        IReadOnlyList<SearchResult> hits = index.Search([1f, 0f, 0f], k: 2);
        Console.WriteLine($"  EmbeddingIndex   : {index.Count} vectors of {index.Dimension} dims");
        foreach (SearchResult hit in hits)
        {
            Console.WriteLine($"    #{hit.Index} score={hit.Score:F4}");
        }
        Console.WriteLine();
    }

    private const string WordPieceJson =
        "{\"version\":\"1.0\",\"truncation\":null,\"padding\":null,\"added_tokens\":[]," +
        "\"normalizer\":{\"type\":\"Lowercase\"},\"pre_tokenizer\":{\"type\":\"Whitespace\"}," +
        "\"post_processor\":null,\"decoder\":null," +
        "\"model\":{\"type\":\"WordPiece\",\"unk_token\":\"[UNK]\",\"continuing_subword_prefix\":\"##\"," +
        "\"max_input_chars_per_word\":100,\"vocab\":{\"[UNK]\":0,\"token\":1,\"##ize\":2,\"text\":3}}}";

    private const string UnigramJson =
        "{\"version\":\"1.0\",\"truncation\":null,\"padding\":null,\"added_tokens\":[]," +
        "\"normalizer\":null,\"pre_tokenizer\":{\"type\":\"Metaspace\",\"replacement\":\"\\u2581\"}," +
        "\"post_processor\":null,\"decoder\":null," +
        "\"model\":{\"type\":\"Unigram\",\"unk_id\":0,\"byte_fallback\":false," +
        "\"vocab\":[[\"<unk>\",0.0],[\"\\u2581alpha\",-1.5],[\"\\u2581beta\",-2.5]]}}";

    private static MemoryStream Utf8(string text) => new(Encoding.UTF8.GetBytes(text));

    /// <summary>
    /// Builds the smallest <c>spiece.model</c> the loader accepts, rather than
    /// committing one: a real model is a trained artifact, and this file only has
    /// to prove the loader is reachable and parses what sentencepiece writes.
    /// </summary>
    private static byte[] SpieceModel()
    {
        var model = new MemoryStream();

        // ModelProto.pieces — repeated SentencePiece { piece = 1, score = 2, type = 3 }.
        WritePiece(model, "<unk>", 0f, type: 2);      // SentencePieceType.Unknown
        WritePiece(model, "<s>", 0f, type: 3);        // SentencePieceType.Control
        WritePiece(model, "\u2581alpha", -1.5f, type: 1);
        WritePiece(model, "\u2581beta", -2.5f, type: 1);

        // ModelProto.trainer_spec — the algorithm, then the special-token ids.
        var trainer = new MemoryStream();
        WriteVarintField(trainer, field: 3, value: 1);    // model_type = UNIGRAM
        WriteVarintField(trainer, field: 35, value: 0);   // byte_fallback = false
        WriteVarintField(trainer, field: 40, value: 0);   // unk_id
        WriteVarintField(trainer, field: 41, value: 1);   // bos_id
        WriteLengthDelimited(model, field: 2, payload: trainer.ToArray());

        // ModelProto.normalizer_spec — absent would be refused, and rightly.
        var normalizer = new MemoryStream();
        WriteLengthDelimited(normalizer, field: 1, payload: Encoding.UTF8.GetBytes("identity"));
        WriteLengthDelimited(model, field: 3, payload: normalizer.ToArray());

        return model.ToArray();
    }

    private static void WritePiece(Stream destination, string piece, float score, int type)
    {
        var message = new MemoryStream();
        WriteLengthDelimited(message, field: 1, payload: Encoding.UTF8.GetBytes(piece));
        WriteTag(message, field: 2, wireType: 5);
        message.Write(BitConverter.GetBytes(score));
        WriteVarintField(message, field: 3, value: type);
        WriteLengthDelimited(destination, field: 1, payload: message.ToArray());
    }

    private static void WriteLengthDelimited(Stream destination, int field, byte[] payload)
    {
        WriteTag(destination, field, wireType: 2);
        WriteVarint(destination, (ulong)payload.Length);
        destination.Write(payload);
    }

    private static void WriteVarintField(Stream destination, int field, int value)
    {
        WriteTag(destination, field, wireType: 0);
        WriteVarint(destination, (ulong)value);
    }

    private static void WriteTag(Stream destination, int field, int wireType) =>
        WriteVarint(destination, ((ulong)field << 3) | (uint)wireType);

    private static void WriteVarint(Stream destination, ulong value)
    {
        while (value >= 0x80)
        {
            destination.WriteByte((byte)(value | 0x80));
            value >>= 7;
        }
        destination.WriteByte((byte)value);
    }
}
