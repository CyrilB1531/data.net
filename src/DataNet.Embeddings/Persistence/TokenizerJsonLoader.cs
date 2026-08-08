using System.Text.Json;
using DataNet.Embeddings.Tokenization;
using DataNet.Internal.Persistence;

namespace DataNet.Embeddings.Persistence;

/// <summary>
/// Reads a HuggingFace <c>tokenizer.json</c> — the WordPiece or Unigram model it
/// declares, together with the settings that change tokenization.
/// </summary>
/// <remarks>
/// <para>
/// Matches the vocabulary side of
/// <c>tokenizers.Tokenizer.from_file("tokenizer.json")</c>. What it deliberately
/// does <em>not</em> do is interpret the whole normalizer / pre-tokenizer /
/// post-processor graph: DataNet's tokenizers implement one fixed pipeline each,
/// and a file describing a different one would tokenize differently here than in
/// Python.
/// </para>
/// <para>
/// So the graph is <em>checked</em> rather than ignored. A normalizer or
/// pre-tokenizer this library does not reproduce — <c>NFKC</c>, <c>Precompiled</c>,
/// <c>BertPreTokenizer</c>, a <c>post_processor</c> that inserts
/// <c>[CLS]</c>/<c>[SEP]</c> — makes loading fail with a message naming it. The
/// alternative is a vocabulary that loads cleanly and produces embeddings for a
/// model nobody trained.
/// </para>
/// <para>
/// The <c>decoder</c> section is the one part accepted unchecked: it affects
/// <c>decode</c> only, and DataNet's tokenizers encode.
/// </para>
/// <para>
/// Unrecognized <em>top-level</em> properties are likewise accepted in silence,
/// where an artifact DataNet itself wrote would reject them. The asymmetry is
/// deliberate: this is a foreign format that gains fields between <c>tokenizers</c>
/// releases, and failing on every one of them would refuse files that tokenize
/// identically. What is checked is the set of sections that <em>change
/// tokenization</em>. The file's own <c>version</c> property is not among them and
/// is not read.
/// </para>
/// <para>
/// One consequence worth stating plainly: a stock HuggingFace BERT
/// <c>tokenizer.json</c> — <c>BertPreTokenizer</c> plus a full
/// <c>BertNormalizer</c> — <strong>is refused</strong>. That is the correct
/// outcome, not a gap: DataNet does not reproduce those steps.
/// <see cref="VocabTxtLoader"/> is the route for BERT, and
/// <see cref="LoadWordPiece(string, ArtifactLoadOptions?)"/> is for files whose
/// pipeline matches DataNet's.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// WordPieceVocabulary vocab = TokenizerJsonLoader.LoadWordPiece("tokenizer.json");
/// var tokenizer = new WordPieceTokenizer(vocab);
/// </code>
/// </example>
public static class TokenizerJsonLoader
{
    private const string SourceName = "tokenizer.json";
    private const string AddedTokensProperty = "added_tokens";
    private const string UntypedName = "(untyped)";
    private const string MetaSymbol = "\u2581";

    /// <summary>Reads the WordPiece model declared by <paramref name="source"/>.</summary>
    /// <param name="source">The <c>tokenizer.json</c> bytes; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="InvalidDataException">The file is malformed, exceeds a limit, declares a different model type, or describes a pipeline this library does not reproduce.</exception>
    public static WordPieceVocabulary LoadWordPiece(Stream source, ArtifactLoadOptions? options = null)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        using JsonDocument document = ParseDocument(JsonArtifact.ReadAllBytes(source, limits), limits);
        return ReadWordPiece(document.RootElement, limits);
    }

    /// <summary>Reads the WordPiece model declared by the file at <paramref name="path"/>.</summary>
    /// <param name="path">Path to a <c>tokenizer.json</c>.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="InvalidDataException">The file is malformed, exceeds a limit, declares a different model type, or describes a pipeline this library does not reproduce.</exception>
    public static WordPieceVocabulary LoadWordPiece(string path, ArtifactLoadOptions? options = null)
    {
        using FileStream file = JsonArtifact.OpenRead(path);
        return LoadWordPiece(file, options);
    }

    /// <summary>Asynchronous counterpart of <see cref="LoadWordPiece(Stream, ArtifactLoadOptions?)"/>.</summary>
    /// <param name="source">The <c>tokenizer.json</c> bytes; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    public static async Task<WordPieceVocabulary> LoadWordPieceAsync(
        Stream source,
        ArtifactLoadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        ReadOnlyMemory<byte> payload = await JsonArtifact.ReadAllBytesAsync(source, limits, cancellationToken).ConfigureAwait(false);
        using JsonDocument document = ParseDocument(payload, limits);
        return ReadWordPiece(document.RootElement, limits);
    }

    /// <summary>Reads the Unigram (SentencePiece) model declared by <paramref name="source"/>.</summary>
    /// <param name="source">The <c>tokenizer.json</c> bytes; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="InvalidDataException">The file is malformed, exceeds a limit, declares a different model type, or describes a pipeline this library does not reproduce.</exception>
    public static SentencePieceVocabulary LoadUnigram(Stream source, ArtifactLoadOptions? options = null)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        using JsonDocument document = ParseDocument(JsonArtifact.ReadAllBytes(source, limits), limits);
        return ReadUnigram(document.RootElement, limits);
    }

    /// <summary>Reads the Unigram model declared by the file at <paramref name="path"/>.</summary>
    /// <param name="path">Path to a <c>tokenizer.json</c>.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="InvalidDataException">The file is malformed, exceeds a limit, declares a different model type, or describes a pipeline this library does not reproduce.</exception>
    public static SentencePieceVocabulary LoadUnigram(string path, ArtifactLoadOptions? options = null)
    {
        using FileStream file = JsonArtifact.OpenRead(path);
        return LoadUnigram(file, options);
    }

    /// <summary>Asynchronous counterpart of <see cref="LoadUnigram(Stream, ArtifactLoadOptions?)"/>.</summary>
    /// <param name="source">The <c>tokenizer.json</c> bytes; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    public static async Task<SentencePieceVocabulary> LoadUnigramAsync(
        Stream source,
        ArtifactLoadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        ReadOnlyMemory<byte> payload = await JsonArtifact.ReadAllBytesAsync(source, limits, cancellationToken).ConfigureAwait(false);
        using JsonDocument document = ParseDocument(payload, limits);
        return ReadUnigram(document.RootElement, limits);
    }

    private static JsonDocument ParseDocument(ReadOnlyMemory<byte> payload, in ArtifactLimits limits)
    {
        // A node tree rather than a single reader pass: the shape of "model"
        // depends on the "type" inside it, and HuggingFace does not guarantee an
        // order that would let a forward-only reader decide in time. The tree is
        // bounded by MaxTotalBytes, which is what keeps it affordable.
        var documentOptions = new JsonDocumentOptions
        {
            MaxDepth = limits.MaxJsonDepth,
            CommentHandling = JsonCommentHandling.Disallow,
            AllowTrailingCommas = false,
        };
        try
        {
            JsonDocument document = JsonDocument.Parse(payload, documentOptions);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                document.Dispose();
                throw new InvalidDataException($"A {SourceName} must be a JSON object.");
            }
            return document;
        }
        catch (JsonException e)
        {
            throw new InvalidDataException($"The {SourceName} is not well-formed JSON: {e.Message}", e);
        }
    }

    private static WordPieceVocabulary ReadWordPiece(JsonElement root, in ArtifactLimits limits)
    {
        // Model type first: "you asked for the wrong loader" is more useful than a
        // complaint about a pipeline the caller was never going to use.
        JsonElement model = RequireObject(root, "model");
        EnsureModelType(model, "WordPiece");
        EnsurePipelineIsReproduced(root, PipelineKind.WordPiece);

        string unkToken = RequireString(model, "unk_token");
        string continuationPrefix = OptionalString(model, "continuing_subword_prefix") ?? "##";
        EnsureMaxInputCharsPerWord(model);

        var vocab = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (JsonProperty entry in RequireObject(model, "vocab").EnumerateObject())
        {
            if (entry.Value.ValueKind != JsonValueKind.Number || !entry.Value.TryGetInt32(out int id))
            {
                throw new InvalidDataException($"The {SourceName} maps token '{entry.Name}' to a value that is not an integer id.");
            }
            limits.CheckTokenLength(entry.Name.Length);
            vocab[entry.Name] = id;
            limits.CheckVocabularySize(vocab.Count);
        }

        if (vocab.Count == 0)
        {
            throw new InvalidDataException($"The {SourceName} declares an empty vocabulary.");
        }
        ReadAddedTokens(root, vocab, limits);
        if (!vocab.ContainsKey(unkToken))
        {
            throw new InvalidDataException($"The {SourceName} names '{unkToken}' as its unknown token but does not define it.");
        }
        return new WordPieceVocabulary(vocab, unkToken, continuationPrefix, ReadLowercase(root));
    }

    private static SentencePieceVocabulary ReadUnigram(JsonElement root, in ArtifactLimits limits)
    {
        JsonElement model = RequireObject(root, "model");
        EnsureModelType(model, "Unigram");
        EnsureByteFallbackIsOff(model);
        EnsurePipelineIsReproduced(root, PipelineKind.Unigram);

        if (!model.TryGetProperty("vocab", out JsonElement vocabElement) || vocabElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"The {SourceName} Unigram model has no 'vocab' array.");
        }
        limits.CheckArrayLength(vocabElement.GetArrayLength(), "model.vocab");

        var pieces = new List<SentencePiece>();
        foreach (JsonElement pair in vocabElement.EnumerateArray())
        {
            pieces.Add(ReadUnigramPiece(pair, pieces.Count, limits));
            limits.CheckVocabularySize(pieces.Count);
        }
        if (pieces.Count == 0)
        {
            throw new InvalidDataException($"The {SourceName} declares an empty vocabulary.");
        }

        int unkId = 0;
        if (model.TryGetProperty("unk_id", out JsonElement unk) && unk.ValueKind == JsonValueKind.Number
            && !unk.TryGetInt32(out unkId))
        {
            // A JSON number that is not a 32-bit integer — 1.5, or 10^20. Left to
            // GetInt32 this surfaces as FormatException, which the loader does not
            // document and a caller catching InvalidDataException would not see.
            throw new InvalidDataException($"The {SourceName} declares a 'unk_id' that is not a 32-bit integer.");
        }
        if (unkId < 0 || unkId >= pieces.Count)
        {
            throw new InvalidDataException(
                $"The {SourceName} declares unk_id {unkId}, outside its own vocabulary range [0, {pieces.Count}).");
        }

        SentencePieceType[] types = ReadUnigramTypes(root, pieces, unkId, limits);
        return new SentencePieceVocabulary(
            pieces,
            types,
            unkId,
            FindSpecialId(pieces, types, "<s>"),
            FindSpecialId(pieces, types, "</s>"),
            FindSpecialId(pieces, types, "<pad>"))
        {
            Normalizer = ReadUnigramNormalizer(root),
        };
    }

    private static SentencePiece ReadUnigramPiece(JsonElement pair, int id, in ArtifactLimits limits)
    {
        // HuggingFace stores each entry as the two-element array [piece, score].
        if (pair.ValueKind != JsonValueKind.Array || pair.GetArrayLength() != 2)
        {
            throw new InvalidDataException($"The {SourceName} Unigram vocabulary entry at id {id} is not a [piece, score] pair.");
        }

        JsonElement pieceElement = pair[0];
        JsonElement scoreElement = pair[1];
        if (pieceElement.ValueKind != JsonValueKind.String || scoreElement.ValueKind != JsonValueKind.Number)
        {
            throw new InvalidDataException($"The {SourceName} Unigram vocabulary entry at id {id} is not a [string, number] pair.");
        }

        string piece = pieceElement.GetString()!;
        limits.CheckTokenLength(piece.Length);
        // A magnitude no double holds (1e999) does not fail to parse — since .NET Core
        // 3.0 it widens to infinity — and an infinite log-probability silently wrecks
        // the Viterbi decode rather than failing, so it is refused here.
        if (!scoreElement.TryGetDouble(out double score) || double.IsNaN(score) || double.IsInfinity(score))
        {
            throw new InvalidDataException(
                $"The {SourceName} Unigram vocabulary entry at id {id} has a score that is not a finite double.");
        }
        return new SentencePiece(piece, score, id);
    }

    /// <summary>
    /// Folds the <c>added_tokens</c> table into a WordPiece vocabulary.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Tokenizer.add_tokens</c> assigns ids <em>after</em> the model's own
    /// vocabulary, so those entries appear nowhere in <c>model.vocab</c>. Left
    /// unread they are silently lost, and a token the caller added deliberately
    /// tokenizes to the unknown token instead.
    /// </para>
    /// <para>
    /// Standard BERT files list their special tokens in both tables at the same
    /// ids, which folds to a no-op. A table that contradicts <c>model.vocab</c>,
    /// or that asks for matching semantics DataNet does not reproduce, is refused
    /// rather than resolved by guesswork.
    /// </para>
    /// </remarks>
    private static void ReadAddedTokens(JsonElement root, Dictionary<string, int> vocab, in ArtifactLimits limits)
    {
        if (!root.TryGetProperty(AddedTokensProperty, out JsonElement added) || added.ValueKind != JsonValueKind.Array)
        {
            return;
        }
        limits.CheckArrayLength(added.GetArrayLength(), AddedTokensProperty);

        foreach (JsonElement token in added.EnumerateArray())
        {
            if (token.ValueKind != JsonValueKind.Object
                || !token.TryGetProperty("content", out JsonElement contentElement)
                || contentElement.ValueKind != JsonValueKind.String
                || !token.TryGetProperty("id", out JsonElement idElement)
                || idElement.ValueKind != JsonValueKind.Number
                || !idElement.TryGetInt32(out int id))
            {
                continue;
            }

            string content = contentElement.GetString()!;
            limits.CheckTokenLength(content.Length);
            if (id < 0)
            {
                // The id is folded into the vocabulary and comes straight back out of
                // Encode, into the caller's embedding lookup. A negative one is an
                // out-of-range index in their code, blamed on them.
                throw new InvalidDataException(
                    $"The {SourceName} adds token '{content}' with the negative id {id}.");
            }
            if (vocab.TryGetValue(content, out int existing))
            {
                if (existing != id)
                {
                    throw new InvalidDataException(
                        $"The {SourceName} adds token '{content}' as id {id} but its vocabulary already maps it to {existing}.");
                }
                continue;
            }

            EnsureAddedTokenMatchesPlainly(content, token);
            vocab[content] = id;
            limits.CheckVocabularySize(vocab.Count);
        }
    }

    /// <summary>
    /// Refuses an added token whose matching rules WordPiece cannot reproduce.
    /// </summary>
    /// <remarks>
    /// HuggingFace matches added tokens ahead of the model, anywhere in the text
    /// and with optional whitespace stripping. Folding one into the vocabulary
    /// makes it matchable as a whole word only, which is the same thing whenever
    /// these flags sit at their defaults — and a different tokenizer when they do not.
    /// </remarks>
    private static void EnsureAddedTokenMatchesPlainly(string content, JsonElement token)
    {
        EnsureAddedTokenFlagIsOff(content, token, "lstrip");
        EnsureAddedTokenFlagIsOff(content, token, "rstrip");
        EnsureAddedTokenFlagIsOff(content, token, "single_word");
    }

    private static void EnsureAddedTokenFlagIsOff(string content, JsonElement token, string flag)
    {
        if (OptionalBoolean(token, flag) is true)
        {
            throw Unsupported(
                $"it adds token '{content}' with {flag} on",
                "a folded-in token is matched as a whole word, so that flag would tokenize differently here");
        }
    }

    private static void EnsureByteFallbackIsOff(JsonElement model)
    {
        if (OptionalBoolean(model, "byte_fallback") is true)
        {
            throw Unsupported(
                "its model declares byte_fallback",
                "Python resolves an uncovered character into <0x..> byte pieces where this tokenizer emits the unknown piece");
        }
    }

    /// <summary>
    /// Derives piece types, which <c>tokenizer.json</c> does not record, from the
    /// <c>added_tokens</c> table.
    /// </summary>
    /// <remarks>
    /// A <c>spiece.model</c> states the type of every piece; a
    /// <c>tokenizer.json</c> states only which tokens were added as special. That
    /// is enough: a special token is a control marker, and the piece at
    /// <c>unk_id</c> is the unknown one.
    /// </remarks>
    private static SentencePieceType[] ReadUnigramTypes(
        JsonElement root,
        List<SentencePiece> pieces,
        int unkId,
        in ArtifactLimits limits)
    {
        var types = new SentencePieceType[pieces.Count];
        for (int i = 0; i < types.Length; i++)
        {
            types[i] = SentencePieceType.Normal;
        }
        types[unkId] = SentencePieceType.Unknown;

        if (!root.TryGetProperty(AddedTokensProperty, out JsonElement added) || added.ValueKind != JsonValueKind.Array)
        {
            return types;
        }
        limits.CheckArrayLength(added.GetArrayLength(), AddedTokensProperty);

        foreach (JsonElement token in added.EnumerateArray())
        {
            if (token.ValueKind != JsonValueKind.Object
                || !token.TryGetProperty("id", out JsonElement idElement)
                || idElement.ValueKind != JsonValueKind.Number
                || !idElement.TryGetInt32(out int id))
            {
                continue;
            }
            bool special = token.TryGetProperty("special", out JsonElement specialElement)
                && specialElement.ValueKind == JsonValueKind.True;
            if (special && id >= 0 && id < types.Length && id != unkId)
            {
                types[id] = SentencePieceType.Control;
            }
        }
        return types;
    }

    private static int FindSpecialId(List<SentencePiece> pieces, SentencePieceType[] types, string piece)
    {
        for (int i = 0; i < pieces.Count; i++)
        {
            if (types[i] == SentencePieceType.Control && string.Equals(pieces[i].Piece, piece, StringComparison.Ordinal))
            {
                return i;
            }
        }
        return -1;
    }

    private enum PipelineKind
    {
        WordPiece,
        Unigram,
    }

    private static void EnsurePipelineIsReproduced(JsonElement root, PipelineKind kind)
    {
        RejectNonNull(root, "truncation", "DataNet tokenizers do not truncate");
        RejectNonNull(root, "padding", "DataNet tokenizers do not pad");
        RejectNonNull(root, "post_processor", "DataNet tokenizers do not insert special tokens such as [CLS] and [SEP]");
        EnsurePreTokenizerIsReproduced(root, kind);
    }

    private static void EnsurePreTokenizerIsReproduced(JsonElement root, PipelineKind kind)
    {
        if (!root.TryGetProperty("pre_tokenizer", out JsonElement pre) || pre.ValueKind == JsonValueKind.Null)
        {
            // Absent means tokenizers hands the whole string to the model. DataNet
            // segments it — Whitespace or Metaspace — so accepting the file would
            // tokenize differently, which is the failure this method exists to catch.
            throw Unsupported(
                "it declares no pre_tokenizer",
                kind == PipelineKind.WordPiece
                    ? "WordPieceTokenizer always splits on whitespace, where an absent pre_tokenizer passes the whole input to the model"
                    : "SentencePieceTokenizer always applies Metaspace segmentation, where an absent pre_tokenizer passes the whole input to the model");
        }

        string type = OptionalString(pre, "type") ?? UntypedName;
        bool accepted = kind == PipelineKind.WordPiece
            ? string.Equals(type, "Whitespace", StringComparison.Ordinal)
            : string.Equals(type, "Metaspace", StringComparison.Ordinal);
        if (!accepted)
        {
            throw Unsupported($"its pre_tokenizer is '{type}'", kind == PipelineKind.WordPiece
                ? "WordPieceTokenizer reproduces the Whitespace pre-tokenizer only"
                : "SentencePieceTokenizer reproduces the Metaspace pre-tokenizer only");
        }

        if (kind == PipelineKind.Unigram)
        {
            EnsureMetaspaceIsReproduced(pre);
        }
    }

    /// <summary>
    /// Refuses a Metaspace pre-tokenizer configured away from what the tokenizer does.
    /// </summary>
    /// <remarks>
    /// Every setting here changes segmentation, and every one defaults to the value
    /// <see cref="SentencePieceTokenizer"/> reproduces — so a file that sets one is a
    /// file that would tokenize differently here, silently, if it went unread.
    /// </remarks>
    private static void EnsureMetaspaceIsReproduced(JsonElement pre)
    {
        string replacement = OptionalString(pre, "replacement") ?? MetaSymbol;
        if (!string.Equals(replacement, MetaSymbol, StringComparison.Ordinal))
        {
            throw Unsupported($"its Metaspace replacement is '{replacement}'", "the tokenizer always uses U+2581");
        }

        string prependScheme = OptionalString(pre, "prepend_scheme") ?? "always";
        if (!string.Equals(prependScheme, "always", StringComparison.Ordinal))
        {
            throw Unsupported(
                $"its Metaspace prepend_scheme is '{prependScheme}'",
                "the tokenizer always prepends the meta symbol to the input");
        }
        if (OptionalBoolean(pre, "add_prefix_space") is false)
        {
            // The pre-0.14 spelling of prepend_scheme, still found in the wild.
            throw Unsupported(
                "its Metaspace has add_prefix_space off",
                "the tokenizer always prepends the meta symbol to the input");
        }
        if (OptionalBoolean(pre, "split") is false)
        {
            throw Unsupported(
                "its Metaspace has split off",
                "only Metaspace's default segmentation is reproduced");
        }
    }

    /// <summary>
    /// Reads the Unigram normalizer: a <c>Precompiled</c> character map, or nothing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <c>Precompiled</c> is how <c>tokenizers</c> writes the very map a
    /// <c>spiece.model</c> carries in <c>normalizer_spec.precompiled_charsmap</c> —
    /// base64 instead of raw bytes, same blob. Since #75 that is read rather than
    /// refused, by the same <see cref="PrecompiledNormalizer"/>, which is what keeps
    /// the two formats from disagreeing about the same model.
    /// </para>
    /// <para>
    /// Every other named type is still refused. <c>NFKC</c> asks for the runtime's
    /// Unicode tables where the model asked for a frozen map;
    /// <c>Lowercase</c> and <c>BertNormalizer</c> belong to the WordPiece pipeline
    /// and mean something different there. Absent or <c>null</c> is identity.
    /// </para>
    /// </remarks>
    private static PrecompiledNormalizer? ReadUnigramNormalizer(JsonElement root)
    {
        if (!root.TryGetProperty("normalizer", out JsonElement normalizer)
            || normalizer.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        string type = OptionalString(normalizer, "type") ?? UntypedName;
        if (!string.Equals(type, "Precompiled", StringComparison.Ordinal))
        {
            throw Unsupported(
                $"its normalizer is '{type}'",
                "SentencePieceTokenizer applies the model's own precompiled character map, and reproduces no other normalization");
        }

        // `is null or { Length: 0 }` rather than string.IsNullOrEmpty: the netstandard2.0
        // reference assembly does not annotate that method, so the compiler still
        // believes the value may be null afterwards and the fix would be a `!`.
        string? encoded = OptionalString(normalizer, "precompiled_charsmap");
        if (encoded is null or { Length: 0 })
        {
            throw Unsupported(
                "its normalizer is 'Precompiled' with no precompiled_charsmap",
                "the rules are applied from the compiled map, never from the name, so there is nothing here to apply");
        }

        byte[] charsMap;
        try
        {
            charsMap = Convert.FromBase64String(encoded);
        }
        catch (FormatException e)
        {
            throw new InvalidDataException(
                $"The {SourceName} carries a precompiled_charsmap that is not valid base64.", e);
        }
        return PrecompiledNormalizer.FromCharsMap(charsMap);
    }

    private static bool ReadLowercase(JsonElement root)
    {
        if (!root.TryGetProperty("normalizer", out JsonElement normalizer) || normalizer.ValueKind == JsonValueKind.Null)
        {
            return false;
        }
        return ReadLowercaseFrom(normalizer);
    }

    private static bool ReadLowercaseFrom(JsonElement normalizer)
    {
        string type = OptionalString(normalizer, "type") ?? UntypedName;
        switch (type)
        {
            case "Lowercase":
                return true;

            case "Sequence":
                return ReadLowercaseFromSequence(normalizer);

            case "BertNormalizer":
                EnsureBertNormalizerIsReproduced(normalizer);
                return OptionalBoolean(normalizer, "lowercase") ?? true;

            default:
                throw Unsupported(
                    $"its normalizer is '{type}'",
                    "only Lowercase, a Sequence of reproduced normalizers, and a plain BertNormalizer are understood");
        }
    }

    private static bool ReadLowercaseFromSequence(JsonElement normalizer)
    {
        if (!normalizer.TryGetProperty("normalizers", out JsonElement inner) || inner.ValueKind != JsonValueKind.Array)
        {
            throw Unsupported("its normalizer is a Sequence with no 'normalizers' array", "the file is not usable");
        }

        bool lowercase = false;
        foreach (JsonElement step in inner.EnumerateArray())
        {
            lowercase |= ReadLowercaseFrom(step);
        }
        return lowercase;
    }

    private static void EnsureBertNormalizerIsReproduced(JsonElement normalizer)
    {
        if (OptionalBoolean(normalizer, "handle_chinese_chars") ?? true)
        {
            throw Unsupported("its BertNormalizer pads CJK characters", "DataNet does not reproduce that step");
        }
        // tokenizers strips accents when strip_accents is Some(true), and also when it
        // is absent and lowercase is on — reading the absent case as "off" would accept
        // a file that strips accents in Python and does not here.
        if (OptionalBoolean(normalizer, "strip_accents") ?? (OptionalBoolean(normalizer, "lowercase") ?? true))
        {
            throw Unsupported("its BertNormalizer strips accents", "DataNet does not reproduce that step");
        }
        if (OptionalBoolean(normalizer, "clean_text") ?? true)
        {
            throw Unsupported("its BertNormalizer cleans control characters", "DataNet does not reproduce that step");
        }
    }

    private static void EnsureModelType(JsonElement model, string expected)
    {
        string type = OptionalString(model, "type") ?? UntypedName;
        if (!string.Equals(type, expected, StringComparison.Ordinal))
        {
            throw new InvalidDataException($"The {SourceName} declares a '{type}' model; this loader reads '{expected}'.");
        }
    }

    private static void EnsureMaxInputCharsPerWord(JsonElement model)
    {
        if (model.TryGetProperty("max_input_chars_per_word", out JsonElement value)
            && value.ValueKind == JsonValueKind.Number
            && value.TryGetInt32(out int maxChars)
            && maxChars != 100)
        {
            throw Unsupported(
                $"its max_input_chars_per_word is {maxChars}",
                $"WordPieceVocabulary does not carry it — pass maxCharsPerWord: {maxChars} to the WordPieceTokenizer constructor");
        }
    }

    private static void RejectNonNull(JsonElement root, string propertyName, string why)
    {
        if (root.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind != JsonValueKind.Null)
        {
            throw Unsupported($"it declares a '{propertyName}' section", why);
        }
    }

    private static JsonElement RequireObject(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value) || value.ValueKind != JsonValueKind.Object)
        {
            throw new InvalidDataException($"The {SourceName} has no '{propertyName}' object.");
        }
        return value;
    }

    private static string RequireString(JsonElement parent, string propertyName) =>
        OptionalString(parent, propertyName)
        ?? throw new InvalidDataException($"The {SourceName} has no '{propertyName}' string.");

    private static string? OptionalString(JsonElement parent, string propertyName) =>
        parent.TryGetProperty(propertyName, out JsonElement value) && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    private static bool? OptionalBoolean(JsonElement parent, string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out JsonElement value))
        {
            return null;
        }
        return value.ValueKind switch
        {
            JsonValueKind.True => true,
            JsonValueKind.False => false,
            _ => null,
        };
    }

    private static InvalidDataException Unsupported(string found, string why) =>
        new($"This {SourceName} cannot be loaded because {found}: {why}. " +
            "Loading it anyway would produce embeddings that do not match the model.");
}
