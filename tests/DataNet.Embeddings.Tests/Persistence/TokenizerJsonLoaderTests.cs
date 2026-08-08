using System.Text;
using System.Text.Json;
using DataNet.Embeddings.Persistence;
using DataNet.Embeddings.Tokenization;
using Xunit;

namespace DataNet.Embeddings.Tests.Persistence;

/// <summary>
/// Replays <c>tokenizer_json.json</c>, which carries two whole HuggingFace
/// tokenizer documents — a WordPiece one and a Unigram one — and the encodings
/// <c>tokenizers</c> produced from them.
/// </summary>
public sealed class TokenizerJsonLoaderTests
{
    [Fact]
    public void LoadWordPiece_reads_the_vocabulary_and_its_settings()
    {
        using JsonDocument doc = OracleLoader.Load("tokenizer_json.json");
        JsonElement meta = doc.RootElement.GetProperty("metadata");

        WordPieceVocabulary vocabulary = LoadWordPiece(meta);

        Assert.Equal(meta.GetProperty("wordpiece_unk_token").GetString(), vocabulary.UnkToken);
        Assert.Equal("##", vocabulary.ContinuationPrefix);
        Assert.Equal(meta.GetProperty("wordpiece_lowercase").GetBoolean(), vocabulary.Lowercase);

        foreach (JsonProperty expected in meta.GetProperty("wordpiece_vocab").EnumerateObject())
        {
            Assert.Equal(expected.Value.GetInt32(), vocabulary.Vocab[expected.Name]);
        }
    }

    [Fact]
    public void The_loaded_wordpiece_vocabulary_reproduces_the_reference_encoding()
    {
        using JsonDocument doc = OracleLoader.Load("tokenizer_json.json");
        var tokenizer = new WordPieceTokenizer(LoadWordPiece(doc.RootElement.GetProperty("metadata")));

        OracleReplay.AssertEncodings(doc, tokenizer.Encode, "tokens", modelFilter: "WordPiece");
    }

    [Fact]
    public void LoadUnigram_reads_the_pieces_and_derives_their_types()
    {
        using JsonDocument doc = OracleLoader.Load("tokenizer_json.json");
        JsonElement meta = doc.RootElement.GetProperty("metadata");

        SentencePieceVocabulary vocabulary = LoadUnigram(meta);

        Assert.Equal(meta.GetProperty("unigram_unk_id").GetInt32(), vocabulary.UnkId);
        Assert.Equal(SentencePieceType.Unknown, vocabulary.Types[vocabulary.UnkId]);

        // tokenizer.json records no piece types; the special entries of
        // added_tokens are what tells the loader which pieces are markers.
        Assert.Equal(SentencePieceType.Control, vocabulary.Types[1]);
        Assert.Equal(SentencePieceType.Control, vocabulary.Types[2]);
        Assert.Equal(SentencePieceType.Normal, vocabulary.Types[3]);
        Assert.Equal(1, vocabulary.BosId);
        Assert.Equal(2, vocabulary.EosId);
        Assert.Equal(-1, vocabulary.PadId);
    }

    [Fact]
    public void The_loaded_unigram_vocabulary_reproduces_the_reference_encoding()
    {
        using JsonDocument doc = OracleLoader.Load("tokenizer_json.json");
        var tokenizer = new SentencePieceTokenizer(LoadUnigram(doc.RootElement.GetProperty("metadata")));

        OracleReplay.AssertEncodings(doc, tokenizer.Encode, "tokens", modelFilter: "Unigram");
    }

    [Fact]
    public void Asking_for_the_wrong_model_type_is_rejected()
    {
        using JsonDocument doc = OracleLoader.Load("tokenizer_json.json");
        string json = doc.RootElement.GetProperty("metadata").GetProperty("wordpiece_tokenizer_json").GetRawText();

        InvalidDataException error = Assert.Throws<InvalidDataException>(() => LoadUnigramFrom(json));

        Assert.Contains("declares a 'WordPiece' model; this loader reads 'Unigram'", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("{\"type\":\"NFKC\"}", "its normalizer is 'NFKC'")]
    [InlineData("{\"type\":\"Precompiled\",\"precompiled_charsmap\":\"AA\"}", "its normalizer is 'Precompiled'")]
    [InlineData("{\"type\":\"Replace\",\"pattern\":{\"String\":\"a\"},\"content\":\"b\"}", "its normalizer is 'Replace'")]
    public void A_normalizer_that_is_not_reproduced_is_rejected(string normalizer, string expectedMessage)
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadWordPieceFrom(SyntheticWordPiece(normalizer: normalizer)));

        Assert.Contains(expectedMessage, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_sequence_normalizer_of_reproduced_steps_is_accepted()
    {
        WordPieceVocabulary vocabulary = LoadWordPieceFrom(
            SyntheticWordPiece(normalizer: "{\"type\":\"Sequence\",\"normalizers\":[{\"type\":\"Lowercase\"}]}"));

        Assert.True(vocabulary.Lowercase);
    }

    [Fact]
    public void A_pre_tokenizer_that_is_not_reproduced_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadWordPieceFrom(SyntheticWordPiece(preTokenizer: "{\"type\":\"BertPreTokenizer\"}")));

        Assert.Contains("its pre_tokenizer is 'BertPreTokenizer'", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_post_processor_that_would_insert_special_tokens_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadWordPieceFrom(SyntheticWordPiece(postProcessor: "{\"type\":\"TemplateProcessing\"}")));

        Assert.Contains("post_processor", error.Message, StringComparison.Ordinal);
        Assert.Contains("[CLS]", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("\"strip_accents\":true,\"handle_chinese_chars\":false,\"clean_text\":false", "strips accents")]
    [InlineData("\"strip_accents\":false,\"handle_chinese_chars\":true,\"clean_text\":false", "pads CJK characters")]
    [InlineData("\"strip_accents\":false,\"handle_chinese_chars\":false,\"clean_text\":true", "cleans control characters")]
    public void A_bert_normalizer_doing_more_than_lowercasing_is_rejected(string flags, string expectedMessage)
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadWordPieceFrom(SyntheticWordPiece(normalizer: $"{{\"type\":\"BertNormalizer\",{flags},\"lowercase\":true}}")));

        Assert.Contains(expectedMessage, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_plain_bert_normalizer_yields_its_lowercase_flag()
    {
        WordPieceVocabulary vocabulary = LoadWordPieceFrom(SyntheticWordPiece(
            normalizer: "{\"type\":\"BertNormalizer\",\"clean_text\":false,\"handle_chinese_chars\":false,\"strip_accents\":false,\"lowercase\":false}"));

        Assert.False(vocabulary.Lowercase);
    }

    [Fact]
    public void An_unusual_max_input_chars_per_word_is_reported_rather_than_dropped()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadWordPieceFrom(SyntheticWordPiece(maxInputCharsPerWord: 42)));

        Assert.Contains("maxCharsPerWord: 42", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_model_that_names_an_undefined_unknown_token_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadWordPieceFrom(SyntheticWordPiece(vocab: "{\"alpha\":0}")));

        Assert.Contains("names '[UNK]' as its unknown token but does not define it", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_empty_vocabulary_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadWordPieceFrom(SyntheticWordPiece(vocab: "{}")));

        Assert.Contains("empty vocabulary", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A minimal <c>tokenizer.json</c> whose pipeline sections can each be varied
    /// independently. Written out rather than patched into the oracle document,
    /// which is stored re-indented and would make string surgery fragile.
    /// </summary>
    private static string SyntheticWordPiece(
        string normalizer = "{\"type\":\"Lowercase\"}",
        string preTokenizer = "{\"type\":\"Whitespace\"}",
        string postProcessor = "null",
        int maxInputCharsPerWord = 100,
        string addedTokens = "[]",
        string vocab = "{\"[UNK]\":0,\"alpha\":1,\"##beta\":2}") =>
        $"{{\"version\":\"1.0\",\"truncation\":null,\"padding\":null,\"added_tokens\":{addedTokens}," +
        $"\"normalizer\":{normalizer},\"pre_tokenizer\":{preTokenizer},\"post_processor\":{postProcessor},\"decoder\":null," +
        "\"model\":{\"type\":\"WordPiece\",\"unk_token\":\"[UNK]\",\"continuing_subword_prefix\":\"##\"," +
        $"\"max_input_chars_per_word\":{maxInputCharsPerWord.ToString(System.Globalization.CultureInfo.InvariantCulture)},\"vocab\":{vocab}}}}}";

    [Fact]
    public void Input_that_is_not_json_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(() => LoadWordPieceFrom("not json"));
        Assert.Contains("not well-formed JSON", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_document_over_the_vocabulary_limit_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadWordPieceFrom(WordPieceDocument(), new ArtifactLoadOptions { MaxVocabularySize = 3 }));

        Assert.Contains("MaxVocabularySize", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task LoadWordPieceAsync_reads_the_same_vocabulary()
    {
        using JsonDocument doc = OracleLoader.Load("tokenizer_json.json");
        JsonElement meta = doc.RootElement.GetProperty("metadata");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(meta.GetProperty("wordpiece_tokenizer_json").GetRawText()));

        WordPieceVocabulary vocabulary = await TokenizerJsonLoader.LoadWordPieceAsync(stream);

        Assert.Equal(LoadWordPiece(meta).Count, vocabulary.Count);
    }

    [Fact]
    public async Task LoadUnigramAsync_reads_the_same_vocabulary()
    {
        using JsonDocument doc = OracleLoader.Load("tokenizer_json.json");
        JsonElement meta = doc.RootElement.GetProperty("metadata");
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(meta.GetProperty("unigram_tokenizer_json").GetRawText()));

        SentencePieceVocabulary vocabulary = await TokenizerJsonLoader.LoadUnigramAsync(stream);

        Assert.Equal(LoadUnigram(meta).Count, vocabulary.Count);
    }

    private static string WordPieceDocument()
    {
        using JsonDocument doc = OracleLoader.Load("tokenizer_json.json");
        return doc.RootElement.GetProperty("metadata").GetProperty("wordpiece_tokenizer_json").GetRawText();
    }

    private static WordPieceVocabulary LoadWordPiece(JsonElement meta) =>
        LoadWordPieceFrom(meta.GetProperty("wordpiece_tokenizer_json").GetRawText());

    private static SentencePieceVocabulary LoadUnigram(JsonElement meta) =>
        LoadUnigramFrom(meta.GetProperty("unigram_tokenizer_json").GetRawText());

    private static WordPieceVocabulary LoadWordPieceFrom(string json, ArtifactLoadOptions? options = null)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return TokenizerJsonLoader.LoadWordPiece(stream, options);
    }

    private static SentencePieceVocabulary LoadUnigramFrom(string json, ArtifactLoadOptions? options = null)
    {
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return TokenizerJsonLoader.LoadUnigram(stream, options);
    }

    // ---- Configurations that change tokenization must be refused, not ignored ----
    //
    // Every one of these loaded cleanly before, and every one produces embeddings
    // that do not match the model the file describes. The frozen corpora all sit on
    // the accepting side of these settings, so only a synthetic document catches them.

    [Theory]
    [InlineData("{\"type\":\"NFKC\"}", "NFKC")]
    [InlineData("{\"type\":\"Lowercase\"}", "Lowercase")]
    [InlineData("{\"type\":\"Sequence\",\"normalizers\":[{\"type\":\"NFKC\"}]}", "Sequence")]
    public void A_unigram_model_with_a_normalizer_that_is_not_precompiled_is_rejected(string normalizer, string expectedName)
    {
        // NFKC is the interesting refusal now: it asks for the runtime's Unicode
        // tables where the model asked for a map frozen at the version that compiled
        // it. The two already disagree on 181 code points and drift apart with every
        // Unicode release — see docs/decisions/0014.
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadUnigramFrom(SyntheticUnigram(normalizer: normalizer)));

        Assert.Contains(expectedName, error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The refusal #75 turned into a load: <c>Precompiled</c> is how
    /// <c>tokenizers</c> writes the map a <c>spiece.model</c> carries as raw bytes,
    /// so the two formats now describe the same model the same way.
    /// </summary>
    /// <remarks>
    /// The map is the one from <c>custom_norm.model</c>, taken from the oracle in
    /// the encoding <c>tokenizer.json</c> itself uses rather than pasted in as a
    /// constant. Its three rules are hand-written — <c>ß</c> to <c>ss</c>,
    /// <c>①</c> to <c>1</c>, <c>¤</c> to nothing — and no built-in specification
    /// performs them, so a normalizer that produced this output by any other route
    /// would be a surprise.
    /// </remarks>
    [Fact]
    public void A_unigram_model_with_a_precompiled_normalizer_loads_and_applies_it()
    {
        using JsonDocument oracle = OracleLoader.Load("normalizer.json");
        string charsMap = oracle.RootElement
            .GetProperty("metadata").GetProperty("models").EnumerateArray()
            .First(m => m.GetProperty("model").GetString() == "custom_norm.model")
            .GetProperty("charsmap_base64").GetString()!;

        SentencePieceVocabulary vocabulary = LoadUnigramFrom(SyntheticUnigram(
            normalizer: $"{{\"type\":\"Precompiled\",\"precompiled_charsmap\":\"{charsMap}\"}}"));

        Assert.NotNull(vocabulary.Normalizer);
        Assert.Equal("ss 1 ", vocabulary.Normalizer.Normalize("ß ① ¤"));
    }

    [Theory]
    [InlineData("{\"type\":\"Precompiled\",\"precompiled_charsmap\":\"AAAAAA==\"}", "empty trie")]  // four zero bytes: a header declaring no trie
    [InlineData("{\"type\":\"Precompiled\",\"precompiled_charsmap\":\"AAAA\"}", "too short")]        // three bytes: not even a header
    [InlineData("{\"type\":\"Precompiled\",\"precompiled_charsmap\":\"\"}", "no precompiled_charsmap")]
    [InlineData("{\"type\":\"Precompiled\",\"precompiled_charsmap\":\"not base64!\"}", "not valid base64")]
    public void A_precompiled_normalizer_that_carries_no_usable_map_is_rejected(string normalizer, string expected)
    {
        // Accepting the type but not the map would normalize nothing while claiming
        // to normalize — the silent-wrong-embeddings failure, one level in.
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadUnigramFrom(SyntheticUnigram(normalizer: normalizer)));

        Assert.Contains(expected, error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_bert_normalizer_that_strips_accents_by_omission_is_rejected()
    {
        // tokenizers strips accents when strip_accents is Some(true) OR when it is
        // absent and lowercase is on. Reading the absent case as "off" accepted a file
        // that strips accents in Python and does not here.
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadWordPieceFrom(SyntheticWordPiece(
                normalizer: "{\"type\":\"BertNormalizer\",\"handle_chinese_chars\":false,\"clean_text\":false,\"lowercase\":true}")));

        Assert.Contains("strips accents", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_bert_normalizer_with_accent_stripping_off_and_no_lowercasing_still_loads()
    {
        WordPieceVocabulary vocabulary = LoadWordPieceFrom(SyntheticWordPiece(
            normalizer: "{\"type\":\"BertNormalizer\",\"handle_chinese_chars\":false,\"clean_text\":false,\"lowercase\":false}"));

        Assert.False(vocabulary.Lowercase);
    }

    [Theory]
    [InlineData(PipelineKindUnderTest.WordPiece)]
    [InlineData(PipelineKindUnderTest.Unigram)]
    public void A_file_with_no_pre_tokenizer_is_rejected(PipelineKindUnderTest kind)
    {
        // No pre_tokenizer means tokenizers hands the whole string to the model.
        // DataNet instead applies Whitespace or Metaspace segmentation, so accepting
        // the file would tokenize differently — quietly.
        InvalidDataException error = Assert.Throws<InvalidDataException>(() => kind switch
        {
            PipelineKindUnderTest.WordPiece => (object)LoadWordPieceFrom(SyntheticWordPiece(preTokenizer: "null")),
            _ => LoadUnigramFrom(SyntheticUnigram(preTokenizer: "null")),
        });

        Assert.Contains("pre_tokenizer", error.Message, StringComparison.Ordinal);
    }

    /// <summary>Which loader a shared test case exercises.</summary>
    public enum PipelineKindUnderTest
    {
        /// <summary>The WordPiece loader.</summary>
        WordPiece,

        /// <summary>The Unigram loader.</summary>
        Unigram,
    }

    [Fact]
    public void A_unigram_model_with_no_normalizer_still_loads()
    {
        // Absent or null is the identity normalizer, which is what the tokenizer does.
        SentencePieceVocabulary vocabulary = LoadUnigramFrom(SyntheticUnigram(normalizer: "null"));

        Assert.Equal(3, vocabulary.Count);
    }

    [Fact]
    public void A_unigram_model_with_byte_fallback_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadUnigramFrom(SyntheticUnigram(byteFallback: "true")));

        Assert.Contains("byte_fallback", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_unigram_model_that_declares_byte_fallback_false_still_loads()
    {
        // The oracle carries the property explicitly; reading it must not make the
        // common case fail.
        SentencePieceVocabulary vocabulary = LoadUnigramFrom(SyntheticUnigram(byteFallback: "false"));

        Assert.Equal(3, vocabulary.Count);
    }

    [Theory]
    [InlineData("\"never\"")]
    [InlineData("\"first\"")]
    public void A_metaspace_that_does_not_always_prepend_is_rejected(string prependScheme)
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadUnigramFrom(SyntheticUnigram(
                preTokenizer: $"{{\"type\":\"Metaspace\",\"replacement\":\"\\u2581\",\"prepend_scheme\":{prependScheme}}}")));

        Assert.Contains("prepend_scheme", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_metaspace_with_add_prefix_space_off_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadUnigramFrom(SyntheticUnigram(
                preTokenizer: "{\"type\":\"Metaspace\",\"replacement\":\"\\u2581\",\"add_prefix_space\":false}")));

        Assert.Contains("add_prefix_space", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_metaspace_with_split_off_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadUnigramFrom(SyntheticUnigram(
                preTokenizer: "{\"type\":\"Metaspace\",\"replacement\":\"\\u2581\",\"split\":false}")));

        Assert.Contains("split", error.Message, StringComparison.Ordinal);
    }

    // ---- added_tokens are read rather than dropped ----

    [Fact]
    public void An_added_token_outside_the_model_vocabulary_joins_it()
    {
        WordPieceVocabulary vocabulary = LoadWordPieceFrom(SyntheticWordPiece(
            addedTokens: "[{\"id\":3,\"content\":\"[EXTRA]\",\"special\":true}]"));

        Assert.Equal(4, vocabulary.Count);
        Assert.Equal(3, vocabulary.Vocab["[EXTRA]"]);
    }

    [Fact]
    public void An_added_token_already_in_the_vocabulary_at_the_same_id_is_a_no_op()
    {
        // What every stock BERT tokenizer.json looks like: the special tokens are
        // listed in both tables.
        WordPieceVocabulary vocabulary = LoadWordPieceFrom(SyntheticWordPiece(
            addedTokens: "[{\"id\":0,\"content\":\"[UNK]\",\"special\":true}]"));

        Assert.Equal(3, vocabulary.Count);
        Assert.Equal(0, vocabulary.Vocab["[UNK]"]);
    }

    [Fact]
    public void An_added_token_that_contradicts_the_vocabulary_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadWordPieceFrom(SyntheticWordPiece(
                addedTokens: "[{\"id\":7,\"content\":\"[UNK]\",\"special\":true}]")));

        Assert.Contains("already maps it to 0", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_added_token_with_a_negative_id_is_rejected()
    {
        // The id is folded into the vocabulary and comes back out of Encode, straight
        // into the caller's embedding lookup. A negative one is an out-of-range index
        // in their code, blamed on them.
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadWordPieceFrom(SyntheticWordPiece(
                addedTokens: "[{\"id\":-5,\"content\":\"[EXTRA]\"}]")));

        Assert.Contains("-5", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("lstrip")]
    [InlineData("rstrip")]
    [InlineData("single_word")]
    public void An_added_token_with_matching_flags_is_rejected(string flag)
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadWordPieceFrom(SyntheticWordPiece(
                addedTokens: $"[{{\"id\":3,\"content\":\"[EXTRA]\",\"{flag}\":true}}]")));

        Assert.Contains(flag, error.Message, StringComparison.Ordinal);
    }

    // ---- Bounds that were declared but never proven ----

    [Fact]
    public void A_document_deeper_than_the_depth_limit_is_rejected()
    {
        // Nested normalizer sequences are the recursive path through the loader.
        // MaxJsonDepth is what keeps that recursion finite.
        string normalizer = "{\"type\":\"Lowercase\"}";
        for (int i = 0; i < 12; i++)
        {
            normalizer = $"{{\"type\":\"Sequence\",\"normalizers\":[{normalizer}]}}";
        }

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadWordPieceFrom(SyntheticWordPiece(normalizer: normalizer), new ArtifactLoadOptions { MaxJsonDepth = 8 }));

        Assert.Contains("not well-formed JSON", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_token_longer_than_the_token_limit_is_rejected()
    {
        string longToken = new('a', 40);

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadWordPieceFrom(
                SyntheticWordPiece(vocab: $"{{\"[UNK]\":0,\"{longToken}\":1}}"),
                new ArtifactLoadOptions { MaxTokenLength = 8 }));

        Assert.Contains("MaxTokenLength", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_unigram_piece_longer_than_the_token_limit_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadUnigramFrom(
                SyntheticUnigram(vocab: $"[[\"<unk>\",0.0],[\"{new string('a', 40)}\",-1.5]]"),
                new ArtifactLoadOptions { MaxTokenLength = 8 }));

        Assert.Contains("MaxTokenLength", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_vocabulary_array_over_the_array_limit_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadUnigramFrom(SyntheticUnigram(), new ArtifactLoadOptions { MaxArrayLength = 2 }));

        Assert.Contains("MaxArrayLength", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void An_added_tokens_array_over_the_array_limit_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadWordPieceFrom(
                SyntheticWordPiece(addedTokens: "[{\"id\":3,\"content\":\"a\"},{\"id\":4,\"content\":\"b\"}]"),
                new ArtifactLoadOptions { MaxArrayLength = 1 }));

        Assert.Contains("MaxArrayLength", error.Message, StringComparison.Ordinal);
    }

    // ---- JSON numbers that are well-formed but unusable ----

    [Theory]
    [InlineData("1.5")]
    [InlineData("99999999999999999999")]
    public void A_unk_id_that_is_not_a_32_bit_integer_is_rejected(string unkId)
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadUnigramFrom(SyntheticUnigram(unkId: unkId)));

        Assert.Contains("not a 32-bit integer", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_score_no_double_can_hold_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadUnigramFrom(SyntheticUnigram(vocab: "[[\"<unk>\",0.0],[\"a\",-1e999]]")));

        Assert.Contains("not a finite double", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A minimal Unigram <c>tokenizer.json</c>, varied one section at a time —
    /// the counterpart of <see cref="SyntheticWordPiece"/>.
    /// </summary>
    private static string SyntheticUnigram(
        string preTokenizer = "{\"type\":\"Metaspace\",\"replacement\":\"\\u2581\"}",
        string byteFallback = "false",
        string unkId = "0",
        string normalizer = "null",
        string vocab = "[[\"<unk>\",0.0],[\"\\u2581alpha\",-1.5],[\"\\u2581beta\",-2.5]]") =>
        "{\"version\":\"1.0\",\"truncation\":null,\"padding\":null,\"added_tokens\":[]," +
        $"\"normalizer\":{normalizer},\"pre_tokenizer\":{preTokenizer},\"post_processor\":null,\"decoder\":null," +
        $"\"model\":{{\"type\":\"Unigram\",\"unk_id\":{unkId},\"byte_fallback\":{byteFallback},\"vocab\":{vocab}}}}}";

    // ---- BPE ----

    private static Stream Bytes(string json) => new MemoryStream(Encoding.UTF8.GetBytes(json));

    private static ArtifactLoadOptions BpeBounds() => new()
    {
        MaxTotalBytes = 8L * 1024 * 1024,
        MaxVocabularySize = 100_000,
        MaxArrayLength = 100_000,
        MaxTokenLength = 512,
    };

    [Fact]
    public void LoadBpe_reproduces_every_frozen_pipeline()
    {
        using JsonDocument doc = OracleLoader.Load("bpe_tokenizer_json.json");

        var failures = new List<string>();
        foreach (JsonElement c in doc.RootElement.GetProperty("cases").EnumerateArray())
        {
            string name = c.GetProperty("name").GetString()!;
            string text = c.GetProperty("text").GetString()!;
            int[] expected = c.GetProperty("ids").EnumerateArray().Select(e => e.GetInt32()).ToArray();

            BpeVocabulary vocab = TokenizerJsonLoader.LoadBpe(
                Bytes(c.GetProperty("tokenizer_json").GetString()!), BpeBounds());
            int[] actual = [.. new BpeTokenizer(vocab).Encode(text).Ids];

            if (!expected.SequenceEqual(actual))
            {
                failures.Add($"[{name}] exp [{string.Join(", ", expected)}] got [{string.Join(", ", actual)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// byte_fallback is the Llama-2 / Mistral v0.1 pipeline (ADR 0017). Loading it
    /// anyway would produce a tokenization that looks right and embeddings that
    /// are not, so it is refused by name.
    /// </summary>
    [Fact]
    public void LoadBpe_refuses_byte_fallback()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[],"byte_fallback":true}}
        """;
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(Bytes(Json), BpeBounds()));
        Assert.Contains("byte_fallback", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadBpe_refuses_a_unigram_model()
    {
        const string Json = """{"model":{"type":"Unigram","vocab":[]}}""";
        Assert.Throws<InvalidDataException>(() => TokenizerJsonLoader.LoadBpe(Bytes(Json), BpeBounds()));
    }

    [Fact]
    public void LoadBpe_refuses_a_pre_tokenizer_it_does_not_reproduce()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[]},
         "pre_tokenizer":{"type":"BertPreTokenizer"}}
        """;
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(Bytes(Json), BpeBounds()));
        Assert.Contains("BertPreTokenizer", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadBpe_reads_both_merge_encodings()
    {
        const string Pairs = """
        {"model":{"type":"BPE","vocab":{"a":0,"b":1,"ab":2},"merges":[["a","b"]]}}
        """;
        const string Lines = """
        {"model":{"type":"BPE","vocab":{"a":0,"b":1,"ab":2},"merges":["a b"]}}
        """;
        Assert.Equal(
            TokenizerJsonLoader.LoadBpe(Bytes(Pairs), BpeBounds()).Merges,
            TokenizerJsonLoader.LoadBpe(Bytes(Lines), BpeBounds()).Merges);
    }

    [Fact]
    public void LoadBpe_reads_ignore_merges()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[],"ignore_merges":true}}
        """;
        Assert.True(TokenizerJsonLoader.LoadBpe(Bytes(Json), BpeBounds()).IgnoreMerges);
    }

    /// <summary>
    /// Stock GPT-2 declares a bare <c>ByteLevel</c> pre-tokenizer with no
    /// <c>Split</c> node at all. <see cref="BpeVocabulary.ByteLevel"/> and
    /// <see cref="BpeVocabulary.PreTokenizerPattern"/> are independent flags for
    /// exactly this shape: an earlier attempt on this branch inferred one from the
    /// other and broke GPT-2 as a result. This pins both values for it directly,
    /// rather than relying only on the end-to-end oracle replay to catch a regression.
    /// </summary>
    [Fact]
    public void LoadBpe_a_bare_byte_level_pre_tokenizer_gets_the_gpt2_pattern()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[]},
         "pre_tokenizer":{"type":"ByteLevel","add_prefix_space":false,"use_regex":true}}
        """;

        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(Bytes(Json), BpeBounds());

        Assert.True(vocabulary.ByteLevel);
        Assert.Equal(BpePatterns.Gpt2, vocabulary.PreTokenizerPattern);
    }

    /// <summary>The other side of <c>use_regex</c>: off leaves the pattern null without turning ByteLevel off.</summary>
    [Fact]
    public void LoadBpe_a_byte_level_pre_tokenizer_with_use_regex_off_has_no_pattern()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[]},
         "pre_tokenizer":{"type":"ByteLevel","use_regex":false}}
        """;

        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(Bytes(Json), BpeBounds());

        Assert.True(vocabulary.ByteLevel);
        Assert.Null(vocabulary.PreTokenizerPattern);
    }
}
