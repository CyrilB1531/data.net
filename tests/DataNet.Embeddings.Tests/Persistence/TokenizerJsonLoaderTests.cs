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

    /// <summary>
    /// The added_tokens table does not satisfy the unknown-token requirement, and
    /// this is the case that says so: it loaded while WordPiece folded the table
    /// into its vocabulary. The table is matched as literal text ahead of the model,
    /// so an unknown token declared only there is one the model can never fall back
    /// to — an [UNK] emitted for an uncovered word would carry an id nothing defines.
    /// </summary>
    [Fact]
    public void An_unknown_token_defined_only_in_added_tokens_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadWordPieceFrom(SyntheticWordPiece(
                vocab: "{\"alpha\":0}",
                addedTokens: "[{\"id\":1,\"content\":\"[UNK]\",\"special\":true}]")));

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
    public void An_added_token_outside_the_model_vocabulary_is_matched_as_text_not_folded_in()
    {
        WordPieceVocabulary vocabulary = LoadWordPieceFrom(SyntheticWordPiece(
            addedTokens: "[{\"id\":3,\"content\":\"[EXTRA]\",\"special\":true}]"));

        // model.vocab is what the file declared, entry for entry. [EXTRA] reaches
        // the tokenizer through AddedTokens, which is scanned as literal text ahead
        // of the model — folded in, it would be matchable as a whole word only.
        Assert.Equal(3, vocabulary.Count);
        Assert.False(vocabulary.Vocab.ContainsKey("[EXTRA]"));
        Assert.Equal(new AddedToken("[EXTRA]", 3) { Special = true }, Assert.Single(vocabulary.AddedTokens));
    }

    [Fact]
    public void An_added_token_already_in_the_vocabulary_at_the_same_id_is_still_recorded()
    {
        // What every stock BERT tokenizer.json looks like: the special tokens are
        // listed in both tables, at the same ids. The overlap is not subtracted —
        // the scan reads nothing but AddedTokens, so an entry left out of it is an
        // entry that reaches the model as ordinary text.
        WordPieceVocabulary vocabulary = LoadWordPieceFrom(SyntheticWordPiece(
            addedTokens: "[{\"id\":0,\"content\":\"[UNK]\",\"special\":true}]"));

        Assert.Equal(3, vocabulary.Count);
        Assert.Equal(0, vocabulary.Vocab["[UNK]"]);
        Assert.Equal(0, Assert.Single(vocabulary.AddedTokens).Id);
    }

    [Fact]
    public void An_added_token_that_contradicts_the_vocabulary_is_rejected()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadWordPieceFrom(SyntheticWordPiece(
                addedTokens: "[{\"id\":7,\"content\":\"[UNK]\",\"special\":true}]")));

        Assert.Contains("already maps it to 0", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// <c>normalized</c> decides which pass an entry matches in, and an absent one
    /// defaults to <c>!special</c> — what Rust's <c>AddedToken::from</c> does, so
    /// every table <c>add_tokens</c> or <c>add_special_tokens</c> wrote keeps its
    /// meaning. The field is read, not inferred: the last case sets it against the
    /// grain of <c>special</c> and the loader must carry that through.
    /// </summary>
    [Theory]
    [InlineData("\"special\":true", false)]
    [InlineData("\"special\":false", true)]
    [InlineData("\"special\":true,\"normalized\":true", true)]
    [InlineData("\"special\":false,\"normalized\":false", false)]
    public void An_added_token_defaults_normalized_to_the_opposite_of_special(string flags, bool expected)
    {
        WordPieceVocabulary vocabulary = LoadWordPieceFrom(SyntheticWordPiece(
            addedTokens: $"[{{\"id\":3,\"content\":\"[EXTRA]\",{flags}}}]"));

        Assert.Equal(expected, Assert.Single(vocabulary.AddedTokens).Normalized);
    }

    /// <summary>
    /// A token read from a file and the same token written out by hand compare
    /// equal, which is the comparison a caller asserting a loaded vocabulary
    /// actually makes. Real files always spell <c>normalized</c> out — tokenizers'
    /// own serializer writes all five flags — so this is the ordinary case, not a
    /// corner: the hand-built token leaves the flag to its default and the loaded
    /// one states it.
    /// </summary>
    [Fact]
    public void An_added_token_read_from_a_file_equals_the_same_token_written_by_hand()
    {
        WordPieceVocabulary vocabulary = LoadWordPieceFrom(SyntheticWordPiece(
            addedTokens: "[{\"id\":3,\"content\":\"[EXTRA]\",\"single_word\":false,\"lstrip\":false,"
                + "\"rstrip\":false,\"normalized\":false,\"special\":true}]"));

        Assert.Equal(new AddedToken("[EXTRA]", 3) { Special = true }, Assert.Single(vocabulary.AddedTokens));
    }

    [Fact]
    public void An_added_token_with_a_negative_id_is_rejected()
    {
        // The id comes straight back out of Encode, into the caller's embedding
        // lookup. A negative one is an out-of-range index in their code, blamed on
        // them.
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => LoadWordPieceFrom(SyntheticWordPiece(
                addedTokens: "[{\"id\":-5,\"content\":\"[EXTRA]\"}]")));

        Assert.Contains("-5", error.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("lstrip")]
    [InlineData("rstrip")]
    [InlineData("single_word")]
    public void An_added_token_carries_the_matching_flag_the_file_set(string flag)
    {
        WordPieceVocabulary vocabulary = LoadWordPieceFrom(SyntheticWordPiece(
            addedTokens: $"[{{\"id\":3,\"content\":\"[EXTRA]\",\"{flag}\":true}}]"));

        AddedToken added = Assert.Single(vocabulary.AddedTokens);
        Assert.Equal(
            flag,
            (added.Lstrip ? "lstrip" : null)
                ?? (added.Rstrip ? "rstrip" : null)
                ?? (added.SingleWord ? "single_word" : null));
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

    private static MemoryStream Bytes(string json) => new MemoryStream(Encoding.UTF8.GetBytes(json));

    /// <summary>
    /// A minimal BPE <c>tokenizer.json</c> document with one extra <c>model</c>
    /// property appended -- e.g. <c>BpeJson(@"""dropout"": 0.0")</c> -- for the
    /// acceptance/refusal pairs below that vary a single model setting.
    /// </summary>
    private static string BpeJson(string extraModelProperty) =>
        "{\"model\":{\"type\":\"BPE\",\"vocab\":{\"a\":0},\"merges\":[]," + extraModelProperty + "}}";

    /// <summary>
    /// The path to <c>tests/oracles/roberta_shaped_model.json</c>, not its parsed
    /// content: <see cref="TokenizerJsonLoader.LoadBpe(string, ArtifactLoadOptions?)"/>
    /// takes a file path, where <see cref="OracleLoader.Load(string)"/> returns an
    /// already-parsed <see cref="JsonDocument"/> — a type mismatch, not a
    /// metadata-shape one, so this reads the file straight off disk instead.
    /// </summary>
    private static readonly string RobertaShapedFixturePath =
        Path.Combine(AppContext.BaseDirectory, "oracles", "roberta_shaped_model.json");

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
                Bytes(c.GetProperty("tokenizer_json").GetString()!), OracleReplay.BpeBounds());
            int[] actual = [.. new BpeTokenizer(vocab).Encode(text).Ids];

            if (!expected.SequenceEqual(actual))
            {
                failures.Add($"[{name}] exp [{string.Join(", ", expected)}] got [{string.Join(", ", actual)}]");
            }
        }

        Assert.True(failures.Count == 0, string.Join("\n", failures));
    }

    /// <summary>
    /// Replays GPT-2's own shape with the two settings the rest of the BPE corpus
    /// structurally cannot see: <c>&lt;|endoftext|&gt;</c> registered as an added token
    /// at the id <c>model.vocab</c> already gives it, and <c>add_prefix_space</c> on,
    /// which is HuggingFace's <c>ByteLevel</c> default.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The two interact, which is why one corpus carries both. Fixing the added-token
    /// table is what makes <c>Encode</c> cut the input into segments at all, and the
    /// prefix space goes on each of those segments rather than once on the whole
    /// input — <c>"hi&lt;|endoftext|&gt;bye"</c> is <c>['Ġhi', '&lt;|endoftext|&gt;',
    /// 'Ġbye']</c>, three pieces each carrying their own space.
    /// </para>
    /// <para>
    /// Every other BPE fixture on this branch either registers no added token at all
    /// (<c>BPE.from_file</c> does not) or never names one in its text, and every one
    /// of them was generated with <c>add_prefix_space=False</c>.
    /// </para>
    /// </remarks>
    [Fact]
    public void LoadBpe_matches_tokenizers_with_added_tokens_and_a_prefix_space_both_live()
    {
        using JsonDocument doc = OracleLoader.Load("bpe_added_tokens.json");
        var tokenizer = new BpeTokenizer(TokenizerJsonLoader.LoadBpe(
            Bytes(doc.RootElement.GetProperty("metadata").GetProperty("tokenizer_json").GetString()!),
            OracleReplay.BpeBounds()));

        OracleReplay.AssertEncodings(doc, tokenizer.Encode, "tokens");
    }

    /// <summary>
    /// The decode direction of the same corpus, which is what proves
    /// <c>skipSpecialTokens</c> does anything at all through the loader: before the
    /// added-token table was read whole, <c>BpeTokenizer</c>'s set of added ids came
    /// out empty and the flag skipped nothing.
    /// </summary>
    [Fact]
    public void LoadBpe_decodes_an_added_token_the_way_tokenizers_does()
    {
        using JsonDocument doc = OracleLoader.Load("bpe_added_tokens.json");
        var tokenizer = new BpeTokenizer(TokenizerJsonLoader.LoadBpe(
            Bytes(doc.RootElement.GetProperty("metadata").GetProperty("tokenizer_json").GetString()!),
            OracleReplay.BpeBounds()));

        OracleReplay.AssertDecodes(doc, (ids, skip) => tokenizer.Decode(ids, skipSpecialTokens: skip));
    }

    /// <summary>
    /// A token listed in <c>added_tokens</c> <em>and</em> in <c>model.vocab</c> at the
    /// same id — which is how HuggingFace writes every special token, GPT-2's
    /// <c>&lt;|endoftext|&gt;</c> at id 50256 included — must still reach
    /// <see cref="BpeVocabulary.AddedTokens"/>.
    /// </summary>
    /// <remarks>
    /// That property is the only input to <c>BpeTokenizer</c>'s pre-merge scan, so
    /// treating the table as "the tokens model.vocab lacks" and subtracting the
    /// intersection removes exactly the tokens the scan exists for. The encode
    /// assertion below is what tells the two apart: with the token matched literally
    /// the whole marker is one id, and without it the marker's characters are not in
    /// this vocabulary at all and are dropped one by one, leaving [2, 2].
    /// </remarks>
    [Fact]
    public void LoadBpe_keeps_an_added_token_that_model_vocab_also_declares()
    {
        const string Json = """
        {"added_tokens":[{"id":3,"content":"<|eot|>","single_word":false,"lstrip":false,"rstrip":false,"special":true}],
         "pre_tokenizer":{"type":"Whitespace"},
         "model":{"type":"BPE","vocab":{"a":0,"b":1,"ab":2,"<|eot|>":3},"merges":[["a","b"]]}}
        """;

        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds());

        Assert.Contains(vocabulary.AddedTokens, t => t.Content == "<|eot|>" && t.Id == 3);
        // model.vocab is left as the file declared it: the added table is a property
        // of its own, not a fold into the vocabulary.
        Assert.Equal(4, vocabulary.Vocab.Count);

        var tokenizer = new BpeTokenizer(vocabulary);
        TokenizationResult encoded = tokenizer.Encode("ab<|eot|>ab");

        // tokenizers 0.23.1 over the same file: ['ab', '<|eot|>', 'ab'], [2, 3, 2].
        Assert.Equal(["ab", "<|eot|>", "ab"], encoded.Tokens);
        Assert.Equal([2, 3, 2], encoded.Ids);
        Assert.Equal("abab", tokenizer.Decode(encoded.Ids, skipSpecialTokens: true));
    }

    /// <summary>
    /// An added token that <c>model.vocab</c> also declares still goes through the
    /// matching-flags read -- the same one a token added for the first time goes
    /// through. Fixture shaped after <c>roberta-base</c>'s own <c>tokenizer.json</c>,
    /// whose <c>&lt;mask&gt;</c> is id 50264 in both tables with <c>lstrip: true</c>.
    /// </summary>
    /// <remarks>
    /// Before the fold-in check moved onto this branch, that file loaded with
    /// <c>lstrip</c> silently ignored. Now that <see cref="AddedTokenScanner"/>
    /// applies the flag, this fixture pins the fold-in branch specifically: it is
    /// separate code from the "new id" branch <c>LoadBpe_reads_the_added_token_matching_flags</c>
    /// exercises.
    /// </remarks>
    [Fact]
    public void LoadBpe_reads_lstrip_on_an_added_token_that_model_vocab_also_declares()
    {
        const string Json = """
        {"added_tokens":[{"id":3,"content":"<mask>","single_word":false,"lstrip":true,"rstrip":false,"special":true}],
         "model":{"type":"BPE","vocab":{"a":0,"b":1,"ab":2,"<mask>":3},"merges":[["a","b"]]}}
        """;

        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds());

        AddedToken mask = Assert.Single(vocabulary.AddedTokens, t => t.Content == "<mask>");
        Assert.True(mask.Lstrip);
        Assert.False(mask.Rstrip);
        Assert.False(mask.SingleWord);
        Assert.True(mask.Special);
    }

    /// <summary>
    /// Issue #104's own acceptance criterion: the <c>added_tokens</c> table
    /// <c>roberta-base</c> actually ships, loaded from a committed fixture
    /// rather than asserted by hand against a string built inline.
    /// <c>tests/oracles/roberta_shaped_model.json</c> is
    /// <c>tools/build_tiny_models.py</c>'s <c>build_roberta_shaped()</c>,
    /// reproducing the file's five entries verbatim: ids 0-3 for
    /// <c>&lt;s&gt;</c>, <c>&lt;pad&gt;</c>, <c>&lt;/s&gt;</c>, <c>&lt;unk&gt;</c>
    /// with every matching flag <see langword="false"/>, and id 50264 for
    /// <c>&lt;mask&gt;</c> with <c>lstrip=true</c>. All five also sit in
    /// <c>model.vocab</c> at the same ids, as <c>roberta-base</c> itself writes
    /// them — 50264 nowhere near contiguous with the tiny vocabulary's own
    /// handful of ids, which is deliberate: the loader must not assume an
    /// added token's id sits next to the rest of the vocabulary just because
    /// this fixture's does.
    /// </summary>
    [Fact]
    public void LoadBpe_accepts_the_roberta_added_token_table()
    {
        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(RobertaShapedFixturePath);

        Assert.Equal(5, vocabulary.AddedTokens.Count);
        AddedToken mask = Assert.Single(vocabulary.AddedTokens, t => t.Content == "<mask>");
        Assert.True(mask.Lstrip);
        Assert.True(mask.Special);
        Assert.All(vocabulary.AddedTokens.Where(t => t.Content != "<mask>"), t => Assert.False(t.Lstrip));
    }

    /// <summary>
    /// The added-token matching flags reach <see cref="BpeVocabulary.AddedTokens"/>
    /// for a token that is new to <c>model.vocab</c> too, not only the fold-in
    /// branch above. Issue #104: <c>roberta-base</c> declares <c>lstrip=true</c> on
    /// <c>&lt;mask&gt;</c>, and this loader used to refuse it outright.
    /// </summary>
    [Fact]
    public void LoadBpe_reads_the_added_token_matching_flags()
    {
        const string Json = """
        {
          "added_tokens": [
            { "id": 2, "content": "<mask>", "lstrip": true, "rstrip": false, "single_word": false, "special": true }
          ],
          "pre_tokenizer": { "type": "ByteLevel", "add_prefix_space": false },
          "model": { "type": "BPE", "vocab": { "a": 0, "b": 1, "<mask>": 2 }, "merges": [] }
        }
        """;

        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds());

        AddedToken mask = Assert.Single(vocabulary.AddedTokens, t => t.Content == "<mask>");
        Assert.True(mask.Lstrip);
        Assert.False(mask.Rstrip);
        Assert.False(mask.SingleWord);
        Assert.True(mask.Special);
    }

    /// <summary>
    /// <see cref="TokenizerJsonLoader.LoadWordPiece(Stream, ArtifactLoadOptions?)"/>
    /// reads the matching flags rather than refusing them. It refused them for as
    /// long as WordPiece folded an added token into its vocabulary, matchable as a
    /// whole word only; now that <see cref="WordPieceTokenizer"/> scans the table as
    /// literal text, the flags have somewhere to be applied.
    /// </summary>
    [Fact]
    public void LoadWordPiece_reads_the_matching_flags_of_an_added_token()
    {
        const string Json = """
        {
          "added_tokens": [
            { "id": 2, "content": "<mask>", "lstrip": true, "rstrip": false, "single_word": false, "special": true }
          ],
          "pre_tokenizer": { "type": "Whitespace" },
          "model": { "type": "WordPiece", "unk_token": "[UNK]", "vocab": { "[UNK]": 0, "a": 1 } }
        }
        """;

        WordPieceVocabulary vocabulary = TokenizerJsonLoader.LoadWordPiece(Bytes(Json));

        AddedToken mask = Assert.Single(vocabulary.AddedTokens);
        Assert.Equal(2, mask.Id);
        Assert.True(mask.Lstrip);
        Assert.False(mask.Rstrip);
        Assert.False(mask.SingleWord);
        Assert.True(mask.Special);
        // And the model's own table is untouched: <mask> is matched as text, so
        // folding it in would make it a whole word the model could produce.
        Assert.Equal(2, vocabulary.Count);
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
            () => TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds()));
        Assert.Contains("byte_fallback", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadBpe_refuses_a_unigram_model()
    {
        const string Json = """{"model":{"type":"Unigram","vocab":[]}}""";
        Assert.Throws<InvalidDataException>(() => TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds()));
    }

    [Fact]
    public void LoadBpe_refuses_a_pre_tokenizer_it_does_not_reproduce()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[]},
         "pre_tokenizer":{"type":"BertPreTokenizer"}}
        """;
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds()));
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
            TokenizerJsonLoader.LoadBpe(Bytes(Pairs), OracleReplay.BpeBounds()).Merges,
            TokenizerJsonLoader.LoadBpe(Bytes(Lines), OracleReplay.BpeBounds()).Merges);
    }

    /// <summary>
    /// A string-form merge with more than one space is refused, not split on the
    /// first one -- the same rule <see cref="BpeFilesLoaderTests"/> pins for the
    /// classic-lineage merges file.
    /// </summary>
    /// <remarks>
    /// Python splits the whole line and refuses it unless it yields exactly two
    /// fields, checked against <c>tokenizers</c> 0.23.1: <c>Tokenizer.from_str</c>
    /// on a BPE model whose merges are <c>["a b c"]</c> reports "Merges text file
    /// invalid at line 1", where <c>["a b"]</c> loads. Splitting on the first space
    /// instead would silently load <c>"a b c"</c> as <c>("a", "b c")</c>.
    /// </remarks>
    [Fact]
    public void LoadBpe_refuses_a_string_merge_that_is_not_two_symbols()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0,"b":1,"c":2},"merges":["a b c"]}}
        """;
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds()));
        Assert.Contains("not two space-separated symbols", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void LoadBpe_reads_a_well_formed_two_field_string_merge()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0,"b":1,"ab":2},"merges":["a b"]}}
        """;
        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds());

        Assert.Single(vocabulary.Merges);
        Assert.Equal(new MergePair("a", "b"), vocabulary.Merges[0]);
    }

    [Fact]
    public void LoadBpe_reads_ignore_merges()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[],"ignore_merges":true}}
        """;
        Assert.True(TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds()).IgnoreMerges);
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

        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds());

        Assert.True(vocabulary.ByteLevel);
        Assert.Null(vocabulary.PreSplitPattern);
        Assert.Equal(BpePatterns.Gpt2, vocabulary.PreTokenizerPattern);
    }

    /// <summary>
    /// The other side of <c>use_regex</c>. HuggingFace's <c>use_regex=false</c> hands
    /// the whole normalized string to the model as a single piece; the null pattern
    /// this used to be read as means <c>BpePreTokenizer</c>'s word-boundary fallback,
    /// which drops the whitespace between the words outright and so cannot round trip.
    /// Refused rather than silently tokenized as something else.
    /// </summary>
    [Fact]
    public void LoadBpe_refuses_a_byte_level_pre_tokenizer_with_use_regex_off()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[]},
         "pre_tokenizer":{"type":"ByteLevel","use_regex":false}}
        """;

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds()));

        Assert.Contains("use_regex", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The same flag inside a <c>Sequence</c> of <c>Split</c> then <c>ByteLevel</c> is
    /// the opposite case and stays accepted, unlike a bare <c>ByteLevel</c>: the
    /// <c>Split</c> step still carries a pattern of its own -- read into
    /// <see cref="BpeVocabulary.PreSplitPattern"/> -- so <c>use_regex: false</c> only
    /// means <c>ByteLevel</c> contributes no second pattern of its own, not that
    /// nothing is split at all. Issue #143.
    /// </summary>
    [Fact]
    public void LoadBpe_accepts_use_regex_off_on_the_byte_level_step_of_a_split_sequence()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[]},
         "pre_tokenizer":{"type":"Sequence","pretokenizers":[
            {"type":"Split","pattern":{"Regex":"\\w+"},"behavior":"Isolated"},
            {"type":"ByteLevel","add_prefix_space":false,"use_regex":false}]},
         "decoder":{"type":"ByteLevel","add_prefix_space":true}}
        """;

        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds());

        Assert.True(vocabulary.ByteLevel);
        Assert.Equal("\\w+", vocabulary.PreSplitPattern);
        Assert.Null(vocabulary.PreTokenizerPattern);
    }

    /// <summary>
    /// A <c>Sequence</c>'s <c>ByteLevel</c> step that omits <c>use_regex</c>
    /// entirely -- what a hand-written <c>tokenizer.json</c> may do, leaning on
    /// the same default the reference relies on. Absent means on: both patterns
    /// are carried, the <c>Split</c> step's into <see cref="BpeVocabulary.PreSplitPattern"/>
    /// and <see cref="BpePatterns.Gpt2"/> into <see cref="BpeVocabulary.PreTokenizerPattern"/>.
    /// Issue #143.
    /// </summary>
    [Fact]
    public void LoadBpe_defaults_a_sequence_byte_level_steps_missing_use_regex_to_on()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[]},
         "pre_tokenizer":{"type":"Sequence","pretokenizers":[
            {"type":"Split","pattern":{"Regex":"\\w+"},"behavior":"Isolated"},
            {"type":"ByteLevel","add_prefix_space":false}]},
         "decoder":{"type":"ByteLevel","add_prefix_space":true}}
        """;

        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds());

        Assert.True(vocabulary.ByteLevel);
        Assert.Equal("\\w+", vocabulary.PreSplitPattern);
        Assert.Equal(BpePatterns.Gpt2, vocabulary.PreTokenizerPattern);
    }

    /// <summary>
    /// `tokenizers` has no default for this field: it refuses the file, in every
    /// position a ByteLevel block can appear. Accepting it would mean inventing a
    /// value that changes the token stream. Issue #118.
    /// </summary>
    [Fact]
    public void LoadBpe_refuses_a_top_level_byte_level_without_add_prefix_space()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[]},
         "pre_tokenizer":{"type":"ByteLevel"}}
        """;

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds()));

        Assert.Contains("add_prefix_space", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The same rule inside a <c>Sequence</c> of <c>Split</c> then <c>ByteLevel</c>:
    /// the <c>ByteLevel</c> step is the same struct `tokenizers` deserializes at the
    /// top level, so it has no default for <c>add_prefix_space</c> there either.
    /// Issue #118.
    /// </summary>
    [Fact]
    public void LoadBpe_refuses_a_sequence_byte_level_step_without_add_prefix_space()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[]},
         "pre_tokenizer":{"type":"Sequence","pretokenizers":[
            {"type":"Split","pattern":{"Regex":"\\w+"},"behavior":"Isolated"},
            {"type":"ByteLevel","use_regex":true}]}}
        """;

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds()));

        Assert.Contains("add_prefix_space", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The same rule again in the <c>decoder</c> slot -- worded through an untagged
    /// enum on the Rust side, but the same field, the same absent default. Issue #118.
    /// </summary>
    [Fact]
    public void LoadBpe_refuses_a_byte_level_decoder_without_add_prefix_space()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[]},
         "pre_tokenizer":{"type":"ByteLevel","add_prefix_space":true},
         "decoder":{"type":"ByteLevel"}}
        """;

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds()));

        Assert.Contains("add_prefix_space", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// The regression guard for the three refusals above: a <c>ByteLevel</c> block
    /// that does declare <c>add_prefix_space</c> still loads and still encodes the way
    /// it did before this task. The leading <c>Ġ</c> in the token stream is what proves
    /// the flag was actually read, not merely that no exception was thrown.
    /// </summary>
    [Fact]
    public void LoadBpe_encodes_a_byte_level_block_that_declares_add_prefix_space()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"Ġ":0,"h":1,"i":2},"merges":[]},
         "pre_tokenizer":{"type":"ByteLevel","add_prefix_space":true,"use_regex":true}}
        """;

        var tokenizer = new BpeTokenizer(TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds()));
        TokenizationResult encoded = tokenizer.Encode("hi");

        Assert.Equal(["Ġ", "h", "i"], encoded.Tokens);
        Assert.Equal([0, 1, 2], encoded.Ids);
    }

    /// <summary>
    /// <c>continuing_subword_prefix</c> is carried into <see cref="BpeVocabulary"/>
    /// and applied by <see cref="BpeTokenizer"/>: HuggingFace prefixes every
    /// non-initial symbol with it before merging, which is not a no-op. Verified
    /// against <c>tokenizers</c> 0.23.1: with the vocabulary below and the merge
    /// <c>("a", "##b")</c>, Python encodes "ab" to <c>[4]</c> where an
    /// implementation ignoring the prefix gives <c>[1, 2]</c>.
    /// </summary>
    [Fact]
    public void LoadBpe_applies_a_continuing_subword_prefix()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":1,"b":2,"##b":3,"ab":4,"a##b":5},
                  "merges":[["a","##b"]],"continuing_subword_prefix":"##"}}
        """;

        var tokenizer = new BpeTokenizer(TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds()));
        TokenizationResult encoded = tokenizer.Encode("ab");

        Assert.Equal([4], encoded.Ids);
    }

    /// <summary>
    /// <c>dropout</c> drops merges at random, so a file declaring it does not have one
    /// tokenization to match. Rare in shipped files and read by nothing here, which is
    /// exactly why it went unrejected.
    /// </summary>
    [Fact]
    public void LoadBpe_refuses_dropout()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[],"dropout":0.1}}
        """;

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds()));

        Assert.Contains("dropout", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A null <c>dropout</c> is what every shipped file writes when it has none, and
    /// is not the same thing as declaring one.
    /// </summary>
    [Fact]
    public void LoadBpe_accepts_a_null_dropout()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[],"dropout":null,
                  "continuing_subword_prefix":null,"fuse_unk":false}}
        """;

        Assert.Single(TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds()).Vocab);
    }

    [Fact]
    public void LoadBpe_accepts_an_empty_continuing_subword_prefix()
    {
        // An empty prefix prefixes nothing, so it reads as absent rather than as a
        // second spelling of "none" -- see BpeContinuingPrefixTests for the value.
        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(Bytes(BpeJson(@"""continuing_subword_prefix"": """"")));

        Assert.NotNull(vocabulary);
    }

    [Fact]
    public void LoadBpe_accepts_a_zero_dropout()
    {
        // At 0.0 no merge is ever skipped, which is the determinism the refusal protects.
        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(Bytes(BpeJson(@"""dropout"": 0.0")));

        Assert.NotNull(vocabulary);
    }

    [Fact]
    public void LoadBpe_still_refuses_a_non_zero_dropout()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(Bytes(BpeJson(@"""dropout"": 0.1"))));

        Assert.Contains("dropout", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A <c>dropout</c> that is present, non-null and not a number is malformed
    /// rather than a reproduced zero -- a JSON string such as <c>"0.0"</c> is not the
    /// numeric zero <see cref="LoadBpe_accepts_a_zero_dropout"/> pins, so it still
    /// falls into the refusal rather than being let through.
    /// </summary>
    [Fact]
    public void LoadBpe_still_refuses_a_string_dropout()
    {
        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(Bytes(BpeJson(@"""dropout"": ""0.0"""))));

        Assert.Contains("dropout", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// <c>ReadBpe</c> never looked at the normalizer at all, where the WordPiece and
    /// Unigram readers both do, so a BPE file declaring <c>NFC</c> or <c>NFKC</c>
    /// loaded and skipped the normalization silently. <see cref="BpeTokenizer"/>
    /// normalizes nothing, so any normalizer is refused by name.
    /// </summary>
    [Fact]
    public void LoadBpe_refuses_a_normalizer()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[]},
         "normalizer":{"type":"NFKC"}}
        """;

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds()));

        Assert.Contains("NFKC", error.Message, StringComparison.Ordinal);
    }

    /// <summary>
    /// A byte-level model whose decoder is not <c>ByteLevel</c> would decode its own
    /// output back into corrupt text -- the file will not round trip through
    /// <c>tokenizers</c> itself either, so this is refused at load time.
    /// </summary>
    [Fact]
    public void LoadBpe_refuses_a_byte_level_model_with_a_mismatched_decoder()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[]},
         "pre_tokenizer":{"type":"ByteLevel","add_prefix_space":true},
         "decoder":{"type":"BPEDecoder"}}
        """;

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds()));

        Assert.Contains("BPEDecoder", error.Message, StringComparison.Ordinal);
    }

    /// <summary>The reverse direction: a non-byte-level model with a <c>ByteLevel</c> decoder.</summary>
    [Fact]
    public void LoadBpe_refuses_a_classic_model_with_a_byte_level_decoder()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":0},"merges":[]},
         "decoder":{"type":"ByteLevel","add_prefix_space":true}}
        """;

        InvalidDataException error = Assert.Throws<InvalidDataException>(
            () => TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds()));

        Assert.Contains("ByteLevel", error.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void A_model_declaring_fuse_unk_loads_and_carries_the_flag()
    {
        const string Json = """
        {"model":{"type":"BPE","vocab":{"[UNK]":0,"a":1,"b":2,"ab":3},"merges":[["a","b"]],
         "unk_token":"[UNK]","fuse_unk":true}}
        """;

        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds());

        Assert.True(vocabulary.FuseUnk);
    }

    [Fact]
    public void A_model_that_is_silent_about_fuse_unk_does_not_fuse()
    {
        // The default has to be false rather than "unset": every corpus this
        // repository committed before issue #119 was generated without the
        // field, and they are the regression proof for the untouched path.
        const string Json = """
        {"model":{"type":"BPE","vocab":{"[UNK]":0,"a":1},"merges":[],"unk_token":"[UNK]"}}
        """;

        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds());

        Assert.False(vocabulary.FuseUnk);
    }

    [Fact]
    public void Fuse_unk_without_an_unk_token_is_accepted_rather_than_refused()
    {
        // Measured: tokenizers accepts {'unk_token': None, 'fuse_unk': True},
        // serializes it, and the flag then has no observable effect. Refusing it
        // here would be a divergence invented rather than reproduced.
        const string Json = """
        {"model":{"type":"BPE","vocab":{"a":1},"merges":[],"fuse_unk":true}}
        """;

        BpeVocabulary vocabulary = TokenizerJsonLoader.LoadBpe(Bytes(Json), OracleReplay.BpeBounds());

        Assert.True(vocabulary.FuseUnk);
        Assert.Null(vocabulary.UnkToken);
    }
}
