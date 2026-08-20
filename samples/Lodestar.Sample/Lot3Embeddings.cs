using System.Text;
using Lodestar.Embeddings.Persistence;
using Lodestar.Embeddings.Pooling;
using Lodestar.Embeddings.Search;
using Lodestar.Embeddings.Tokenization;

namespace Lodestar.Sample;

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

    /// <summary>The word three of the four vocabularies below spell out.</summary>
    private const string Token = "token";

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
            [Token] = 1,
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
        Console.WriteLine($"  WP token_to_id   : 'text'={(inline.TryGetId("text", out int wpId) ? wpId : -1)} "
            + "(on the class; the interface call later reaches ISubwordTokenizer instead)");
        Console.WriteLine($"  vocab.txt        : {fromTxt.Count} tokens, unk='{fromTxt.UnkToken}', "
            + $"prefix='{fromTxt.ContinuationPrefix}', lowercase={fromTxt.Lowercase}, dict={fromTxt.Vocab.Count}");
        Console.WriteLine($"  from vocabulary  : [{string.Join(", ", new WordPieceTokenizer(fromTxt, maxCharsPerWord: 100).Encode(SampleText).Tokens)}]");

        // …and as HuggingFace ships it: a tokenizer.json.
        WordPieceVocabulary fromJson = TokenizerJsonLoader.LoadWordPiece(Utf8(WordPieceJson), bounds);
        Console.WriteLine($"  tokenizer.json   : {fromJson.Count} WordPiece tokens");

        // added_tokens matches literal text before the model ever sees it, like
        // BERT's own [MASK]; lstrip absorbs the space into the token string, not the ids.
        AddedToken maskToken = fromJson.AddedTokens[0];
        Console.WriteLine($"  added token      : '{maskToken.Content}'->{maskToken.Id}, special={maskToken.Special}, "
            + $"lstrip={maskToken.Lstrip}, rstrip={maskToken.Rstrip}, singleWord={maskToken.SingleWord}, normalized={maskToken.Normalized}");
        TokenizationResult maskEncoded = new WordPieceTokenizer(fromJson, maxCharsPerWord: 100).Encode("token [MASK]");
        Console.WriteLine($"  lstrip in action : \"token [MASK]\" -> "
            + $"[{string.Join(", ", maskEncoded.Tokens.Select(t => $"'{t}'"))}] -> [{string.Join(", ", maskEncoded.Ids)}]");

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
            + $"piece[2]='{handBuilt.Pieces[2].Piece}' score={Inv.F1(handBuilt.Pieces[2].Score)} id={handBuilt.Pieces[2].Id}, "
            + $"matchable(0)={handBuilt.IsMatchable(0)}, unk={handBuilt.UnkId} bos={handBuilt.BosId} eos={handBuilt.EosId} pad={handBuilt.PadId}");

        Console.WriteLine($"  SP token_to_id   : '\u2581alpha'={(sp.TryGetId("\u2581alpha", out int spId) ? spId : -1)}, "
            + $"bare 'alpha' present={sp.TryGetId("alpha", out _)}");

        BpeVocabulary fromBpeJson = TokenizerJsonLoader.LoadBpe(Utf8(BpeJson), bounds);
        Console.WriteLine($"  BPE tokenizer.json: {fromBpeJson.Count} tokens, {fromBpeJson.Merges.Count} merge");

        SentencePieceVocabulary fromUnigramJson = TokenizerJsonLoader.LoadUnigram(Utf8(UnigramJson), bounds);
        Console.WriteLine($"  unigram json     : {fromUnigramJson.Count} pieces");

        SentencePieceVocabulary fromModel = SentencePieceModelLoader.Load(new MemoryStream(SpieceModel()), bounds);
        Console.WriteLine($"  spiece.model     : {fromModel.Count} pieces, unk={fromModel.UnkId}");

        // A stock T5/ALBERT/camemBERT/XLM-R ships a charsmap in normalizer_spec; this
        // model has none (no artifacts committed), so null means the text reaches the tokenizer unchanged.
        PrecompiledNormalizer? normalizer = fromModel.Normalizer;
        Console.WriteLine($"  normalizer       : {(normalizer is null ? "identity, no charsmap" : $"{normalizer.CharsMapLength} bytes")}");

        // BPE, the decoder-model side of the library. Byte-level is the variant
        // GPT-2, Llama-3 and Qwen2 use, and the one that round-trips exactly.
        var bpeVocab = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["Ġ"] = 0, ["t"] = 1, ["o"] = 2, ["k"] = 3, ["e"] = 4, ["n"] = 5,
            ["to"] = 6, ["ken"] = 7, [Token] = 8, ["Ġtoken"] = 9, ["ke"] = 10,
        };
        var bpeMerges = new List<MergePair> { new("t", "o"), new("k", "e"), new("ke", "n") };
        var bpeModel = new BpeVocabulary(bpeVocab, bpeMerges)
        {
            ByteLevel = true,
            PreTokenizerPattern = BpePatterns.Gpt2,
            // Stock GPT-2 declares a bare ByteLevel with no Split step in front; a
            // Llama-3/Qwen2 file sets one, whose behavior decides the text between matches.
            PreSplit = null,
        };
        var bpe = new BpeTokenizer(bpeModel);
        TokenizationResult bpeEncoded = bpe.Encode(Token);
        Console.WriteLine($"  BPE byte-level   : [{string.Join(", ", bpeEncoded.Tokens)}] -> [{string.Join(", ", bpeEncoded.Ids)}]");
        Console.WriteLine($"  BPE round trip   : \"{bpe.Decode(bpeEncoded.Ids)}\"");
        Console.WriteLine($"  merge rank 0     : {bpeModel.Merges[0].Left} + {bpeModel.Merges[0].Right}");
        Console.WriteLine($"  BPE normalizer   : {bpeModel.NormalizationForms.Count} forms");

        // A Llama-3/Qwen2 file's Sequence puts a Split step ahead of ByteLevel
        // (behavior + invert, read since #145); this only demonstrates what PreSplit carries.
        var llamaSplit = new BpeSplitStep(BpePatterns.Llama3, SplitBehavior.Isolated, Invert: false);
        Console.WriteLine($"  BpeSplitStep     : behavior={llamaSplit.Behavior}, invert={llamaSplit.Invert}");

        // A file declaring no pre-tokenizer -- absent, or a ByteLevel with
        // use_regex off -- hands the merge loop the whole text (issue #122).
        var spanningVocab = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["a"] = 0, [" "] = 1, ["b"] = 2, ["a "] = 3, ["a b"] = 4,
        };
        var spanningMerges = new List<MergePair> { new("a", " "), new("a ", "b") };
        var noSplit = new BpeVocabulary(spanningVocab, spanningMerges) { NoPreTokenizer = true };
        // The merge spanning the space is what tells the two modes apart: the
        // classic split drops that space before the loop can ever see the pair.
        var classicSplit = new BpeVocabulary(spanningVocab, spanningMerges)
        {
            PreTokenizerPattern = BpePatterns.Whitespace,
        };
        Console.WriteLine($"  no pre-tokenizer : \"a b\" -> "
            + $"[{string.Join(", ", new BpeTokenizer(noSplit).Encode("a b").Tokens.Select(t => $"'{t}'"))}]");
        Console.WriteLine($"  Whitespace split : \"a b\" -> "
            + $"[{string.Join(", ", new BpeTokenizer(classicSplit).Encode("a b").Tokens.Select(t => $"'{t}'"))}]");

        // continuing_subword_prefix: the marker a non-initial piece opens with, as
        // WordPiece's "##" does. A classic BPE model may declare it and be tokenized.
        var classicVocab = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["a"] = 0, ["b"] = 1, ["##b"] = 2, ["ab"] = 3,
        };
        var prefixed = new BpeVocabulary(classicVocab, [new MergePair("a", "##b")])
        {
            ContinuingSubwordPrefix = "##",
            PreTokenizerPattern = BpePatterns.Whitespace,
        };
        Console.WriteLine($"  continuing prefix: '{prefixed.ContinuingSubwordPrefix}', \"ab\" -> "
            + $"[{string.Join(", ", new BpeTokenizer(prefixed).Encode("ab").Tokens.Select(t => $"'{t}'"))}]");

        // Pairing it with ByteLevel is refused: there the space is already a token
        // character, so a continuation marker would describe a split that never happens.
        var byteLevelPrefixed = new BpeVocabulary(classicVocab, [new MergePair("a", "##b")])
        {
            ContinuingSubwordPrefix = "##",
            ByteLevel = true,
            PreTokenizerPattern = BpePatterns.Gpt2,
        };
        try
        {
            _ = new BpeTokenizer(byteLevelPrefixed);
        }
        catch (ArgumentException refused)
        {
            Console.WriteLine($"  byte-level + it  : refused — {refused.Message.Split('.')[0]}.");
        }

        // Everything a tokenizer.json can declare about a BPE model, read back off the
        // record: these are the flags that decide whether two files tokenize alike.
        Console.WriteLine($"  BPE model flags  : {bpeModel.Vocab.Count} entries, "
            + $"{bpeModel.AddedTokens.Count} added, prefix space={bpeModel.AddPrefixSpace}, "
            + $"ignore merges={bpeModel.IgnoreMerges}, fuse unk={bpeModel.FuseUnk}");
        Console.WriteLine($"  BPE model markers: end-of-word='{bpeModel.EndOfWordSuffix ?? "(none)"}', "
            + $"unknown='{bpeModel.UnkToken ?? "(none)"}'");
        Console.WriteLine($"  BPE token_to_id  : 'token'={(bpe.TryGetId("token", out int bpeId) ? bpeId : -1)}");

        // The fourth pre-tokenizer pattern, and the pattern a Split step carries.
        Console.WriteLine($"  Qwen2 pattern    : {BpePatterns.Qwen2.Length} characters, "
            + $"a Split step's own is {llamaSplit.Pattern.Length}");

        // The same model as a consumer gets it: vocab.json + merges.txt.
        BpeVocabulary fromFiles = BpeFilesLoader.Load(
            Utf8("""{"Ġ":0,"t":1,"o":2,"k":3,"e":4,"n":5,"to":6,"ken":7,"token":8,"Ġtoken":9,"ke":10}"""),
            Utf8("#version: 0.2\nt o\nk e\nke n\n"));
        Console.WriteLine($"  BPE from files   : {fromFiles.Count} tokens, {fromFiles.Merges.Count} merges");

        // Special tokens, truncation and padding come from the template and the
        // vocabulary — nothing hardcoded, which is why [CLS] can sit at id 4 here.
        var batchVocab = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            [Unknown] = 0,
            [Token] = 1,
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
        Console.WriteLine($"  encoder options  : max {batchEncoder.Options.MaxLength}, "
            + $"prefix [{string.Join(", ", options.Template.PrefixTokens)}], "
            + $"suffix [{string.Join(", ", options.Template.SuffixTokens)}]");

        // The templates a model family expects, and the one that wraps nothing.
        Console.WriteLine($"  templates        : Bert {SpecialTokenTemplate.Bert.SpecialTokenCount}, "
            + $"Roberta {SpecialTokenTemplate.Roberta.SpecialTokenCount}, "
            + $"T5 {SpecialTokenTemplate.T5.SpecialTokenCount}, "
            + $"None {SpecialTokenTemplate.None.SpecialTokenCount}");

        // Encoding through the interface, which is what BatchEncoder is given.
        Console.WriteLine($"  via interface    : [{string.Join(", ", subword.Encode(SampleText).Tokens)}]");

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
        Console.WriteLine($"  MeanPool         : {Inv.List(pooled)}");
        Console.WriteLine($"  MeanPool+L2      : {Inv.List(normalized)}");
        Console.WriteLine($"  VectorMath       : dot={Inv.F3(VectorMath.Dot(pooled, normalized))}, l2={Inv.F3(VectorMath.L2Norm(pooled))}");

        // Each row of the [batch, seq, dim] tensor pools against its own mask
        // slice, so a shorter sequence's padding cannot reach its vector.
        float[] batchedEmbeddings = [1f, 0f, 0f, 9f, 9f, 9f, 0f, 1f, 0f, 0f, 0f, 1f];
        long[] batchedMask = [1, 0, 1, 1];
        float[][] batchPooled = Pooler.MeanPoolAndNormalizeBatch(
            batchedEmbeddings, batchSize: 2, seqLen: 2, dim: 3, batchedMask);
        Console.WriteLine($"  MeanPoolBatch    : {batchPooled.Length} vectors, "
            + string.Join(" | ", batchPooled.Select(v => $"[{string.Join(", ", v.Select(Inv.F3))}]")));

        // The same batch without the normalization, which is the other overload.
        float[][] rawBatch = Pooler.MeanPoolBatch(batchedEmbeddings, batchSize: 2, seqLen: 2, dim: 3, batchedMask);
        Console.WriteLine($"  MeanPoolBatch raw: "
            + string.Join(" | ", rawBatch.Select(v => $"[{string.Join(", ", v.Select(Inv.F3))}]")));

        // Nearest-neighbour search over those vectors, with the ids a reloaded
        // index is queried by.
        var index = new EmbeddingIndex(dimension: 3, normalize: true);
        index.Add([1f, 0f, 0f], "east");
        index.Add([0f, 1f, 0f], "north");
        index.Add([0.9f, 0.1f, 0f], "east-north-east");
        IReadOnlyList<SearchResult> hits = index.Search([1f, 0f, 0f], k: 2);
        Console.WriteLine($"  EmbeddingIndex   : {index.Count} vectors of {index.Dimension} dims, "
            + $"ids present={index.HasIds}");
        foreach (SearchResult hit in hits)
        {
            Console.WriteLine($"    #{hit.Index} {index.GetId(hit.Index)} score={Inv.F4(hit.Score)}");
        }

        // Embed once, query for as long as the artifact lasts.
        using var artifact = new MemoryStream();
        index.Save(artifact);
        artifact.Position = 0;
        EmbeddingIndex reloaded = EmbeddingIndex.Load(artifact);
        SearchResult best = reloaded.Search([1f, 0f, 0f], k: 1)[0];
        Console.WriteLine($"  Reloaded index   : {reloaded.Count} vectors, "
            + $"best '{reloaded.GetId(best.Index)}' score={Inv.F4(best.Score)}");

        // Already holding the bytes -- a blob, a cache, an embedded resource -- there is
        // nothing for a stream to do but copy them, so this overload reads them in place.
        EmbeddingIndex fromMemory = EmbeddingIndex.Load(artifact.ToArray().AsMemory());
        Console.WriteLine($"  From memory      : {fromMemory.Count} vectors, "
            + $"same best '{fromMemory.GetId(fromMemory.Search([1f, 0f, 0f], k: 1)[0].Index)}'");
        Console.WriteLine();
    }

    /// <summary>A minimal BPE tokenizer.json, the shape HuggingFace ships.</summary>
    private const string BpeJson =
        """
        {"version":"1.0","truncation":null,"padding":null,"added_tokens":[],
         "normalizer":null,"pre_tokenizer":{"type":"Whitespace"},"post_processor":null,
         "decoder":null,
         "model":{"type":"BPE","dropout":null,"unk_token":null,
                  "continuing_subword_prefix":null,"end_of_word_suffix":null,
                  "fuse_unk":false,"byte_fallback":false,"ignore_merges":false,
                  "vocab":{"t":0,"o":1,"to":2},"merges":["t o"]}}
        """;

    private const string WordPieceJson =
        "{\"version\":\"1.0\",\"truncation\":null,\"padding\":null," +
        "\"added_tokens\":[{\"id\":4,\"content\":\"[MASK]\",\"lstrip\":true,\"special\":true}]," +
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
