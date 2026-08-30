using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Lodestar.Embeddings.Tokenization;
using Lodestar.Internal.Persistence;

namespace Lodestar.Embeddings.Persistence;

/// <summary>Reads a HuggingFace <c>tokenizer.json</c>: the WordPiece, Unigram or BPE model it declares, with the settings that change tokenization.</summary>
/// <remarks>
/// The vocabulary side of <c>tokenizers.Tokenizer.from_file</c>. Each tokenizer here
/// implements one fixed pipeline, so a file describing another is refused by name —
/// <c>docs/guides/embeddings.md</c>'s "Models that are refused" lists them, stock BERT
/// included, whose route is <see cref="VocabTxtLoader"/>. An unknown top-level property
/// is accepted in silence: <c>docs/equivalence.md</c>'s loader row.
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

    /// <summary>
    /// The property name read in five places: the three positions a <c>ByteLevel</c>
    /// block can appear in, and <c>Metaspace</c>'s pre-0.14 spelling of
    /// <c>prepend_scheme</c>, which the Unigram and the BPE path read differently.
    /// One spelling, so a typo cannot make one reader silently stop finding what
    /// the others find.
    /// </summary>
    private const string AddPrefixSpaceProperty = "add_prefix_space";
    private const string UntypedName = "(untyped)";

    /// <summary>The <c>type</c> a normalizer or a pre-tokenizer wrapping several steps declares.</summary>
    private const string SequenceName = "Sequence";
    private const string MetaSymbol = "\u2581";

    /// <summary>Reads the WordPiece model declared by <paramref name="source"/>.</summary>
    /// <param name="source">The <c>tokenizer.json</c> bytes; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="InvalidDataException">The file is malformed, exceeds a limit, declares a different model type, or describes a pipeline this library does not reproduce.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
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
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
    public static WordPieceVocabulary LoadWordPiece(string path, ArtifactLoadOptions? options = null)
    {
        using FileStream file = JsonArtifact.OpenRead(path);
        return LoadWordPiece(file, options);
    }

    /// <summary>Asynchronous counterpart of <see cref="LoadWordPiece(Stream, ArtifactLoadOptions?)"/>.</summary>
    /// <param name="source">The <c>tokenizer.json</c> bytes; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="InvalidDataException">The file is malformed, exceeds a limit, declares a different model type, or describes a pipeline this library does not reproduce.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
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
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
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
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
    public static SentencePieceVocabulary LoadUnigram(string path, ArtifactLoadOptions? options = null)
    {
        using FileStream file = JsonArtifact.OpenRead(path);
        return LoadUnigram(file, options);
    }

    /// <summary>Asynchronous counterpart of <see cref="LoadUnigram(Stream, ArtifactLoadOptions?)"/>.</summary>
    /// <param name="source">The <c>tokenizer.json</c> bytes; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="InvalidDataException">The file is malformed, exceeds a limit, declares a different model type, or describes a pipeline this library does not reproduce.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
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

    /// <summary>Reads the BPE model declared by <paramref name="source"/>.</summary>
    /// <remarks>
    /// Alone among the three, this checks the <c>decoder</c> against the model's own
    /// byte-level-ness: <see cref="BpeTokenizer"/> decodes, so a mismatch would corrupt
    /// output rather than go unused.
    /// </remarks>
    /// <param name="source">The <c>tokenizer.json</c> bytes; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="InvalidDataException">The file is malformed, exceeds a limit, declares a different model type, or describes a pipeline this library does not reproduce.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    public static BpeVocabulary LoadBpe(Stream source, ArtifactLoadOptions? options = null)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        using JsonDocument document = ParseDocument(JsonArtifact.ReadAllBytes(source, limits), limits);
        return ReadBpe(document.RootElement, limits);
    }

    /// <summary>Reads the BPE model declared by the file at <paramref name="path"/>.</summary>
    /// <param name="path">Path to a <c>tokenizer.json</c>.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="InvalidDataException">The file is malformed, exceeds a limit, declares a different model type, or describes a pipeline this library does not reproduce.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="path"/> is null.</exception>
    public static BpeVocabulary LoadBpe(string path, ArtifactLoadOptions? options = null)
    {
        using FileStream file = JsonArtifact.OpenRead(path);
        return LoadBpe(file, options);
    }

    /// <summary>Asynchronous counterpart of <see cref="LoadBpe(Stream, ArtifactLoadOptions?)"/>.</summary>
    /// <param name="source">The <c>tokenizer.json</c> bytes; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <exception cref="ArgumentNullException"><paramref name="source"/> is null.</exception>
    /// <exception cref="InvalidDataException">The file is malformed, exceeds a limit, declares a different model type, or describes a pipeline this library does not reproduce.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="cancellationToken"/> was cancelled.</exception>
    public static async Task<BpeVocabulary> LoadBpeAsync(
        Stream source,
        ArtifactLoadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArtifactLimits limits = ArtifactLoadOptions.LimitsOf(options);
        ReadOnlyMemory<byte> payload = await JsonArtifact.ReadAllBytesAsync(source, limits, cancellationToken).ConfigureAwait(false);
        using JsonDocument document = ParseDocument(payload, limits);
        return ReadBpe(document.RootElement, limits);
    }

    private static JsonDocument ParseDocument(ReadOnlyMemory<byte> payload, in ArtifactLimits limits)
    {
        // A node tree, not a single forward pass: "model"'s shape depends on its
        // own "type", and HuggingFace does not guarantee the key order to read it first.
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
        List<AddedToken> addedTokens = ReadAddedTokenTable(root, vocab, limits);
        if (!vocab.ContainsKey(unkToken))
        {
            // model.vocab, not added_tokens: docs/equivalence.md's WordPiece loader
            // row has why a table entry alone can't cover the fallback.
            throw new InvalidDataException($"The {SourceName} names '{unkToken}' as its unknown token but does not define it.");
        }
        return new WordPieceVocabulary(vocab, unkToken, continuationPrefix, ReadLowercase(root))
        {
            AddedTokens = addedTokens,
        };
    }

    private static SentencePieceVocabulary ReadUnigram(JsonElement root, in ArtifactLimits limits)
    {
        JsonElement model = RequireObject(root, "model");
        EnsureNotByteFallbackBpe(model);
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
            // Not a 32-bit integer (1.5, 10^20): GetInt32 would throw FormatException,
            // uncaught by a caller expecting only InvalidDataException.
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
        // A magnitude no double holds (1e999) parses as infinity since .NET Core 3.0
        // rather than failing, and an infinite log-probability would wreck Viterbi decoding silently.
        if (!scoreElement.TryGetDouble(out double score) || double.IsNaN(score) || double.IsInfinity(score))
        {
            throw new InvalidDataException(
                $"The {SourceName} Unigram vocabulary entry at id {id} has a score that is not a finite double.");
        }
        return new SentencePiece(piece, score, id);
    }

    /// <summary>
    /// Reads the whole <c>added_tokens</c> table: matched as literal text ahead of
    /// the model, with the four flags deciding where each entry matches.
    /// </summary>
    /// <remarks>
    /// See <c>docs/guides/embeddings.md</c>'s "Loading vocabularies" and
    /// <c>docs/equivalence.md</c>'s loader rows for why the <c>model.vocab</c>
    /// intersection stays in and what a contradicting entry does.
    /// </remarks>
    /// <param name="root">The <c>tokenizer.json</c> root object.</param>
    /// <param name="vocab">The model's vocabulary, read but never written.</param>
    /// <param name="limits">Bounds applied while reading.</param>
    private static List<AddedToken> ReadAddedTokenTable(
        JsonElement root,
        Dictionary<string, int> vocab,
        in ArtifactLimits limits)
    {
        var table = new List<AddedToken>();
        if (!root.TryGetProperty(AddedTokensProperty, out JsonElement added) || added.ValueKind != JsonValueKind.Array)
        {
            return table;
        }
        limits.CheckArrayLength(added.GetArrayLength(), AddedTokensProperty);

        var known = new Dictionary<string, int>(vocab, StringComparer.Ordinal);
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
            EnsureAddedTokenAgrees(content, id, known, limits);
            var parsed = new AddedToken(content, id)
            {
                Lstrip = OptionalBoolean(token, "lstrip") is true,
                Rstrip = OptionalBoolean(token, "rstrip") is true,
                SingleWord = OptionalBoolean(token, "single_word") is true,
                Special = OptionalBoolean(token, "special") is true,
            };
            // Absent falls to AddedToken's own !Special default, stated once, on the
            // type; docs/equivalence.md's WordPiece row has why tokenizers never reaches this.
            table.Add(OptionalBoolean(token, "normalized") is bool normalized
                ? parsed with { Normalized = normalized }
                : parsed);
        }
        return table;
    }

    /// <summary>Checks one entry against everything already known, and records it there.</summary>
    private static void EnsureAddedTokenAgrees(
        string content,
        int id,
        Dictionary<string, int> known,
        in ArtifactLimits limits)
    {
        if (id < 0)
        {
            // The id comes straight back out of Encode; see docs/guides/embeddings.md's
            // refused-files list for why a negative one is refused here.
            throw new InvalidDataException(
                $"The {SourceName} adds token '{content}' with the negative id {id}.");
        }
        if (known.TryGetValue(content, out int existing))
        {
            if (existing != id)
            {
                throw new InvalidDataException(
                    $"The {SourceName} adds token '{content}' as id {id} but its vocabulary already maps it to {existing}.");
            }
            return;
        }

        known[content] = id;
        limits.CheckVocabularySize(known.Count);
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
    /// Names byte_fallback ahead of the model-kind mismatch <see cref="EnsureModelType"/> would
    /// otherwise report first (#343): a Llama-2 or Mistral v0.1 file declares a BPE model with
    /// byte_fallback, and LoadBpe refuses that by name too, so "wrong loader" would send the
    /// reader on a round trip to a call that fails for the same reason anyway.
    /// </summary>
    private static void EnsureNotByteFallbackBpe(JsonElement model)
    {
        string type = OptionalString(model, "type") ?? UntypedName;
        if (string.Equals(type, "BPE", StringComparison.Ordinal) && OptionalBoolean(model, "byte_fallback") is true)
        {
            throw Unsupported(
                "it declares a 'BPE' model with byte_fallback",
                "LoadBpe is the call for a BPE model, and it refuses byte_fallback by name too -- neither loader reproduces this checkpoint");
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

    /// <summary>
    /// Reads a BPE model: its vocabulary, its ranked merge table, and the pipeline
    /// flags that decide how text reaches them.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="ReadWordPiece"/> and <see cref="ReadUnigram"/>, the pre-tokenizer
    /// here sets five fields, not one bit, differently for <c>ByteLevel</c>, <c>Whitespace</c>,
    /// a <c>Split</c>/<c>ByteLevel</c> <c>Sequence</c> and an absent one — see
    /// <c>docs/equivalence.md</c>'s BPE loader row.
    /// </remarks>
    private static BpeVocabulary ReadBpe(JsonElement root, in ArtifactLimits limits)
    {
        JsonElement model = RequireObject(root, "model");
        EnsureModelType(model, "BPE");
        EnsureByteFallbackIsOff(model);
        EnsureBpeModelSettingsAreReproduced(model);
        IReadOnlyList<NormalizationForm> normalizationForms =
            ReadBpeNormalizer(root, out MetaspaceEscape? normalizerEscape);
        RejectNonNull(root, "truncation", "Lodestar tokenizers do not truncate");
        RejectNonNull(root, "padding", "Lodestar tokenizers do not pad");
        RejectNonNull(root, "post_processor", "Lodestar tokenizers do not insert special tokens such as [CLS] and [SEP]");
        (bool byteLevel, bool addPrefixSpace, BpeSplitStep? preSplit, string? pattern, bool noPreTokenizer) =
            ReadBpePreTokenizer(root, out MetaspaceEscape? preTokenizerEscape);
        EnsureDecoderMatchesModel(root, byteLevel);
        EnsureContinuingPrefixIsNotByteLevel(model, byteLevel);

        Dictionary<string, int> vocab = ReadBpeVocab(model, limits);
        List<MergePair> merges = ReadBpeMerges(model, limits);
        List<AddedToken> addedTokens = ReadAddedTokenTable(root, vocab, limits);

        return new BpeVocabulary(vocab, merges)
        {
            AddedTokens = addedTokens,
            ByteLevel = byteLevel,
            AddPrefixSpace = addPrefixSpace,
            IgnoreMerges = OptionalBoolean(model, "ignore_merges") ?? false,
            FuseUnk = OptionalBoolean(model, "fuse_unk") ?? false,
            EndOfWordSuffix = OptionalString(model, "end_of_word_suffix"),
            // Normalises "" to null itself, as EndOfWordSuffix does; BpeContinuingPrefixTests
            // measures an empty prefix's token stream against a declared-none model's.
            ContinuingSubwordPrefix = OptionalString(model, "continuing_subword_prefix"),
            UnkToken = OptionalString(model, "unk_token"),
            PreSplit = preSplit,
            PreTokenizerPattern = pattern,
            NoPreTokenizer = noPreTokenizer,
            NormalizationForms = normalizationForms,
            Metaspace = OneEscape(normalizerEscape, preTokenizerEscape),
        };
    }

    /// <summary>Joins the two spellings of one escape, refusing a file that writes both.</summary>
    /// <remarks>
    /// Llama-2 writes the normalizer Sequence and Mistral v0.1 the pre-tokenizer block;
    /// neither of the files read for decision 0050 writes both, so nothing measured says
    /// which one a file meaning both would apply, and a precedence invented here would be
    /// a guess no reader could check.
    /// </remarks>
    private static MetaspaceEscape? OneEscape(MetaspaceEscape? fromNormalizer, MetaspaceEscape? fromPreTokenizer)
    {
        if (fromNormalizer is not null && fromPreTokenizer is not null)
        {
            throw Unsupported(
                "it declares a Metaspace pre_tokenizer and a Prepend plus Replace normalizer Sequence at once",
                "those are two writings of one whitespace escape, and which of the two such a file means has not been measured");
        }
        return fromNormalizer ?? fromPreTokenizer;
    }

    /// <summary>
    /// Refuses the <c>model</c> setting that changes what BPE produces and that
    /// <see cref="BpeTokenizer"/> does not apply.
    /// </summary>
    /// <remarks>
    /// <c>dropout</c> is training-time augmentation, and no model of the 23 read for
    /// decision 0034 declares one. Refused by name rather than tokenized plausibly
    /// and wrongly; 0034 also records why "no deterministic tokenizer reproduces it"
    /// is not the reason.
    /// </remarks>
    private static void EnsureBpeModelSettingsAreReproduced(JsonElement model)
    {
        // Zero skips no merge at all, so a numeric zero is accepted here, while anything
        // present that is not a number is malformed and reaches the throw below.
        // S1244 is suppressed because the exact value is the point: zero is what "no
        // dropout" round-trips to, and a tolerance would accept small dropouts that this
        // loader cannot reproduce.
#pragma warning disable S1244
        if (model.TryGetProperty("dropout", out JsonElement dropout)
            && dropout.ValueKind != JsonValueKind.Null
            && (dropout.ValueKind != JsonValueKind.Number || dropout.GetDouble() != 0.0))
#pragma warning restore S1244
        {
            throw Unsupported(
                "its model declares dropout",
                "that is a training-time regularizer; set it to null to load the file, "
                + "which changes nothing about what the model was trained to produce");
        }
    }

    /// <summary>
    /// Reads the BPE normalizer: the four Unicode forms, a <c>Sequence</c> of them, or
    /// nothing -- the counterpart of <see cref="ReadLowercaseFrom"/> and <see cref="ReadUnigramNormalizer"/>.
    /// </summary>
    /// <remarks>
    /// Five of fifteen surveyed <c>tokenizer.json</c> files declare a normalizer,
    /// four of them <c>NFC</c>; an empty <c>Sequence</c> (deepseek-coder) is
    /// accepted because it provably changes nothing, like <c>dropout: 0.0</c>. See
    /// docs/superpowers/specs/2026-08-13_0121_give-readbpe-the-normalizer-treatment.md.
    /// </remarks>
    private static List<NormalizationForm> ReadBpeNormalizer(JsonElement root, out MetaspaceEscape? escape)
    {
        escape = null;
        if (!root.TryGetProperty("normalizer", out JsonElement normalizer) || normalizer.ValueKind == JsonValueKind.Null)
        {
            return [];
        }

        escape = ReadNormalizerEscape(normalizer);
        if (escape is not null)
        {
            // The pair is the whole normalizer: it declares no Unicode form to collect.
            return [];
        }

        var forms = new List<NormalizationForm>();
        CollectNormalizationForms(normalizer, forms);
        return forms;
    }

    /// <summary>Reads a <c>Prepend</c> plus <c>Replace</c> Sequence as one escape.</summary>
    /// <remarks>
    /// Decision 0050 §4 refuses a Sequence naming either step in any other arrangement
    /// rather than reducing three steps to the two reproduced here — the third would
    /// silently not run. A Sequence naming neither reads back as <see langword="null"/>
    /// and reaches <see cref="CollectNormalizationForms"/> unchanged, so a bare
    /// <c>Replace</c> keeps the refusal it has always had.
    /// </remarks>
    private static MetaspaceEscape? ReadNormalizerEscape(JsonElement normalizer)
    {
        if (!string.Equals(OptionalString(normalizer, "type"), SequenceName, StringComparison.Ordinal)
            || !normalizer.TryGetProperty("normalizers", out JsonElement steps)
            || steps.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        bool namesEitherStep = false;
        foreach (JsonElement step in steps.EnumerateArray())
        {
            string stepType = OptionalString(step, "type") ?? UntypedName;
            namesEitherStep = namesEitherStep
                || string.Equals(stepType, "Prepend", StringComparison.Ordinal)
                || string.Equals(stepType, "Replace", StringComparison.Ordinal);
        }
        if (!namesEitherStep)
        {
            return null;
        }

        if (steps.GetArrayLength() != 2
            || !string.Equals(OptionalString(steps[0], "type"), "Prepend", StringComparison.Ordinal)
            || !string.Equals(OptionalString(steps[1], "type"), "Replace", StringComparison.Ordinal))
        {
            throw Unsupported(
                "its normalizer is a Sequence naming Prepend or Replace that is not exactly [Prepend, Replace]",
                "that pair is the whitespace escape Llama-2 spells out, and a Sequence around it carries steps this loader does not reproduce");
        }
        return ReadPrependReplaceEscape(steps[0], steps[1]);
    }

    /// <summary>Reads the <c>Prepend</c> and the <c>Replace</c> as one escape, or refuses the pair.</summary>
    /// <remarks>
    /// The two have to agree: <c>Prepend</c>'s string is the replacement, and
    /// <c>Replace</c> must map the literal space onto that same string, which is what
    /// Llama-2 declares. Anything else — a <c>Regex</c> pattern above all — keeps
    /// <see cref="CollectNormalizationForms"/>'s reason rather than being guessed at.
    /// </remarks>
    private static MetaspaceEscape ReadPrependReplaceEscape(JsonElement prepend, JsonElement replace)
    {
        string? prepended = OptionalString(prepend, "prepend");
        string? content = OptionalString(replace, "content");
        // ValueKind first: TryGetProperty throws InvalidOperationException on anything but
        // an object, and a hand-written "pattern": " " is a file, not a bug to surface.
        string? pattern = replace.TryGetProperty("pattern", out JsonElement patternElement)
            && patternElement.ValueKind == JsonValueKind.Object
            ? OptionalString(patternElement, "String")
            : null;
        if (prepended is null
            || !string.Equals(prepended, content, StringComparison.Ordinal)
            || !string.Equals(pattern, " ", StringComparison.Ordinal))
        {
            throw Unsupported(
                "its normalizer Sequence is a Prepend and a Replace that do not spell the whitespace escape",
                "only a Prepend whose string the Replace maps the literal ' ' onto is reproduced");
        }
        // A normalizer prepends to every gap the added tokens leave -- "always", not
        // "first" -- and unguarded, Prepend running before Replace (decision 0062).
        return new MetaspaceEscape(
            SingleReplacementChar(prepended, "normalizer Sequence's Prepend"),
            MetaspacePrependScheme.Always,
            removeExtraWhitespaces: false,
            skipPrependWhenAlreadyPrefixed: false);
    }

    /// <summary>The one character a replacement must be, or a refusal naming what was found.</summary>
    private static char SingleReplacementChar(string replacement, string where) =>
        replacement.Length == 1
            ? replacement[0]
            : throw Unsupported(
                $"its {where} replacement is '{replacement}'",
                "the escape substitutes one character for each space, so a replacement of another length is not reproduced");

    private static void CollectNormalizationForms(JsonElement normalizer, List<NormalizationForm> forms)
    {
        string type = OptionalString(normalizer, "type") ?? UntypedName;
        switch (type)
        {
            case "NFC":
                forms.Add(NormalizationForm.FormC);
                return;

            case "NFKC":
                forms.Add(NormalizationForm.FormKC);
                return;

            case "NFD":
                forms.Add(NormalizationForm.FormD);
                return;

            case "NFKD":
                forms.Add(NormalizationForm.FormKD);
                return;

            case SequenceName:
                if (!normalizer.TryGetProperty("normalizers", out JsonElement inner) || inner.ValueKind != JsonValueKind.Array)
                {
                    throw Unsupported("its normalizer is a Sequence with no 'normalizers' array", "the file is not usable");
                }
                foreach (JsonElement step in inner.EnumerateArray())
                {
                    CollectNormalizationForms(step, forms);
                }
                return;

            case "Replace":
                throw Unsupported(
                    "its normalizer is 'Replace'",
                    "a Replace pattern may be a Rust regex, whose flavour .NET does not share, so reproducing it needs a measurement nobody has made");

            default:
                throw Unsupported(
                    $"its normalizer is '{type}'",
                    "only NFC, NFKC, NFD, NFKD and a Sequence of those are understood");
        }
    }

    /// <summary>
    /// Refuses a byte-level model that also declares a non-empty
    /// <c>continuing_subword_prefix</c>.
    /// </summary>
    /// <remarks>
    /// See <c>docs/guides/embeddings.md</c>'s refused-files list for why
    /// <see cref="BpeTokenizer"/>'s two halves would silently disagree. An empty
    /// prefix normalises to absent and is not refused.
    /// </remarks>
    private static void EnsureContinuingPrefixIsNotByteLevel(JsonElement model, bool byteLevel)
    {
        if (byteLevel && !string.IsNullOrEmpty(OptionalString(model, "continuing_subword_prefix")))
        {
            throw Unsupported(
                "its pre_tokenizer is byte-level and its model declares a non-empty continuing_subword_prefix",
                "BpeTokenizer does not apply the prefix on the byte-level path while its merge loop still strips it "
                + "from a merge's right side, so the two would disagree — and the byte-level alphabet contains the "
                + "characters a prefix is typically spelled with, so that disagreement can land on another "
                + "existing id rather than raise");
        }
    }

    private static Dictionary<string, int> ReadBpeVocab(JsonElement model, in ArtifactLimits limits)
    {
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
        return vocab;
    }

    private static List<MergePair> ReadBpeMerges(JsonElement model, in ArtifactLimits limits)
    {
        if (!model.TryGetProperty("merges", out JsonElement mergesElement) || mergesElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException($"The {SourceName} BPE model has no 'merges' array.");
        }
        limits.CheckArrayLength(mergesElement.GetArrayLength(), "model.merges");

        var merges = new List<MergePair>();
        foreach (JsonElement entry in mergesElement.EnumerateArray())
        {
            merges.Add(ReadBpeMerge(entry, merges.Count));
        }
        return merges;
    }

    /// <summary>
    /// Reads one merge, in either encoding <c>tokenizers</c> has used: a
    /// <c>[left, right]</c> pair of strings, or a single <c>"left right"</c> string.
    /// </summary>
    /// <remarks>
    /// The string form must split into exactly one space: <c>TokenizerJsonLoaderTests</c>
    /// measures <c>tokenizers</c> 0.23.1 refusing <c>"a b c"</c> the same way. A
    /// trailing space is not an error — <c>"a "</c> still splits into two fields.
    /// </remarks>
    private static MergePair ReadBpeMerge(JsonElement entry, int index)
    {
        if (entry.ValueKind == JsonValueKind.String)
        {
            string line = entry.GetString()!;
            // CA1307: string.IndexOf(char, StringComparison) has no netstandard2.0
            // overload, and both calls are ordinal on every runtime that has it anyway.
#pragma warning disable CA1307
            int space = line.IndexOf(' ');
            if (space < 0)
            {
                throw new InvalidDataException(
                    $"The {SourceName} BPE merge at index {index} has no separator: '{line}'.");
            }
            if (line.IndexOf(' ', space + 1) >= 0)
            {
                throw new InvalidDataException(
                    $"The {SourceName} BPE merge at index {index} is not two space-separated symbols: '{line}'.");
            }
#pragma warning restore CA1307
            return new MergePair(line.Substring(0, space), line.Substring(space + 1));
        }
        if (entry.ValueKind == JsonValueKind.Array
            && entry.GetArrayLength() == 2
            && entry[0].ValueKind == JsonValueKind.String
            && entry[1].ValueKind == JsonValueKind.String)
        {
            return new MergePair(entry[0].GetString()!, entry[1].GetString()!);
        }
        throw new InvalidDataException(
            $"The {SourceName} BPE merge at index {index} is neither a [left, right] pair nor a space-separated string.");
    }

    /// <summary>
    /// Validates the pre-tokenizer and derives the five values <see cref="BpeVocabulary"/>
    /// carries independently: whether the model is byte-level, whether a space is
    /// prepended, the <c>Split</c> step text is split on first (its pattern,
    /// behavior and invert together, not a bare pattern), the pattern it is
    /// split on again, and whether it is split at all.
    /// </summary>
    private static (bool ByteLevel, bool AddPrefixSpace, BpeSplitStep? PreSplit, string? Pattern, bool NoPreTokenizer)
        ReadBpePreTokenizer(JsonElement root, out MetaspaceEscape? escape)
    {
        escape = null;
        if (!root.TryGetProperty("pre_tokenizer", out JsonElement pre) || pre.ValueKind == JsonValueKind.Null)
        {
            // Nothing is split: measured, "aZ Za" is ['a', '[UNK]', 'a'] here against the
            // Whitespace split's four -- tests/oracles/bpe_no_split.json, cases 0 and 3.
            return (false, false, null, null, true);
        }

        string type = OptionalString(pre, "type") ?? UntypedName;
        return type switch
        {
            "Whitespace" => (false, false, null, BpePatterns.Whitespace, false),
            "ByteLevel" => ReadByteLevelPreTokenizer(pre),
            SequenceName => ReadBpeSequencePreTokenizer(pre),
            "Metaspace" => ReadBpeMetaspacePreTokenizer(pre, out escape),
            _ => throw Unsupported(
                $"its pre_tokenizer is '{type}'",
                "BpeTokenizer reproduces ByteLevel, a Sequence of Split then ByteLevel, Whitespace and a non-splitting Metaspace only"),
        };
    }

    /// <summary>
    /// A <c>Metaspace</c> pre-tokenizer with <c>split</c> off — what Mistral v0.1
    /// declares: it replaces and prepends, and cuts nothing.
    /// </summary>
    /// <remarks>
    /// Splitting is refused rather than reproduced, since <see cref="BpeTokenizer"/> has
    /// no pattern for Metaspace's own segmentation and decision 0017 §3's rule is that
    /// what is not reproduced fails at load naming itself. What is left is a text
    /// transform, so nothing splits at all — which is what the last field carries.
    /// </remarks>
    private static (bool ByteLevel, bool AddPrefixSpace, BpeSplitStep? PreSplit, string? Pattern, bool NoPreTokenizer)
        ReadBpeMetaspacePreTokenizer(JsonElement pre, out MetaspaceEscape? escape)
    {
        if (OptionalBoolean(pre, "split") ?? true)
        {
            throw Unsupported(
                "its Metaspace pre_tokenizer has split on",
                "BpeTokenizer reproduces the replace-and-prepend transform only, not Metaspace's own segmentation");
        }
        escape = new MetaspaceEscape(
            SingleReplacementChar(OptionalString(pre, "replacement") ?? MetaSymbol, "Metaspace"),
            ReadBpePrependScheme(pre),
            removeExtraWhitespaces: false,
            skipPrependWhenAlreadyPrefixed: true);
        return (false, false, null, null, true);
    }

    /// <summary>Reads <c>prepend_scheme</c>, or the older <c>add_prefix_space</c> standing in for it.</summary>
    /// <remarks>
    /// <see cref="AddPrefixSpaceProperty"/> is the pre-0.14 spelling, and what it prepends
    /// to is the whole text: true reads as "always", false as "never", and both fields
    /// absent leaves "always". Absorbing that here is what keeps one escape for the
    /// spellings a file may use, as decision 0050 §2 asks.
    /// </remarks>
    private static MetaspacePrependScheme ReadBpePrependScheme(JsonElement pre)
    {
        if (OptionalString(pre, "prepend_scheme") is not string scheme)
        {
            return OptionalBoolean(pre, AddPrefixSpaceProperty) ?? true
                ? MetaspacePrependScheme.Always
                : MetaspacePrependScheme.Never;
        }
        return scheme switch
        {
            "always" => MetaspacePrependScheme.Always,
            "first" => MetaspacePrependScheme.First,
            "never" => MetaspacePrependScheme.Never,
            _ => throw Unsupported(
                $"its Metaspace prepend_scheme is '{scheme}'",
                "only 'always', 'first' and 'never' are defined there"),
        };
    }

    /// <summary>
    /// A bare <c>ByteLevel</c> pre-tokenizer -- what stock GPT-2 declares, with no
    /// <c>Split</c> node: <see cref="BpePatterns.Gpt2"/> is the split pattern.
    /// </summary>
    /// <remarks>
    /// <c>use_regex</c> defaults to <see langword="true"/> and stock GPT-2 omits it. Off,
    /// HuggingFace hands the model one piece and so does <see cref="BpeVocabulary.NoPreTokenizer"/>,
    /// which the corpus's <c>oĠ</c> merge measures: it spans the space the pattern would
    /// have cut at (<c>bpe_no_split.json</c>, cases 6 and 10).
    /// </remarks>
    private static (bool ByteLevel, bool AddPrefixSpace, BpeSplitStep? PreSplit, string? Pattern, bool NoPreTokenizer)
        ReadByteLevelPreTokenizer(JsonElement pre)
    {
        if (OptionalBoolean(pre, AddPrefixSpaceProperty) is not bool addPrefixSpace)
        {
            throw Unsupported(
                "its ByteLevel pre_tokenizer declares no add_prefix_space",
                "tokenizers 0.23.1 has no default for that field and refuses the file identically, where accepting it here would invent the value that decides whether a leading space is added");
        }
        // No Split step in front of it, so there is no first pattern to carry: ByteLevel's
        // own is the only split there could be, and use_regex says whether there is one.
        bool useRegex = OptionalBoolean(pre, "use_regex") ?? true;
        return (true, addPrefixSpace, null, useRegex ? BpePatterns.Gpt2 : null, !useRegex);
    }

    /// <summary>
    /// A <c>Sequence</c> of exactly a <c>Split</c> step then a <c>ByteLevel</c> step --
    /// what Llama-3 and Qwen2 declare. HuggingFace runs both: <c>Split</c> produces
    /// the pieces first, and <c>ByteLevel</c> re-splits each of them on its own
    /// pattern unless the step's <c>use_regex</c> is off.
    /// </summary>
    private static (bool ByteLevel, bool AddPrefixSpace, BpeSplitStep? PreSplit, string? Pattern, bool NoPreTokenizer)
        ReadBpeSequencePreTokenizer(JsonElement pre)
    {
        if (!pre.TryGetProperty("pretokenizers", out JsonElement steps)
            || steps.ValueKind != JsonValueKind.Array
            || steps.GetArrayLength() != 2)
        {
            throw Unsupported(
                "its pre_tokenizer is a Sequence that is not exactly [Split, ByteLevel]",
                "BpeTokenizer reproduces that shape only");
        }

        JsonElement split = steps[0];
        JsonElement byteLevelStep = steps[1];
        string splitType = OptionalString(split, "type") ?? UntypedName;
        string byteLevelType = OptionalString(byteLevelStep, "type") ?? UntypedName;
        if (!string.Equals(splitType, "Split", StringComparison.Ordinal)
            || !string.Equals(byteLevelType, "ByteLevel", StringComparison.Ordinal))
        {
            throw Unsupported(
                $"its pre_tokenizer is a Sequence of '{splitType}' then '{byteLevelType}'",
                "BpeTokenizer reproduces a Sequence of Split then ByteLevel only");
        }

        string pattern = ReadSplitPattern(split);

        // behavior/invert are required, not defaulted (docs/equivalence.md's Split
        // row). A wrongly-typed field gets its own message: Serde reports a mismatch, not a missing field.
        if (!split.TryGetProperty("behavior", out JsonElement behaviorElement))
        {
            throw Unsupported(
                "its Sequence's Split step declares no behavior",
                "tokenizers 0.23.1 has no default for that field and refuses the file identically");
        }
        if (behaviorElement.ValueKind != JsonValueKind.String)
        {
            throw Unsupported(
                "its Sequence's Split step's behavior is not a string",
                "tokenizers 0.23.1 expects one of five string values there and refuses a file whose type disagrees");
        }
        if (!split.TryGetProperty("invert", out JsonElement invertElement))
        {
            throw Unsupported(
                "its Sequence's Split step declares no invert",
                "tokenizers 0.23.1 has no default for that field and refuses the file identically");
        }
        if (invertElement.ValueKind is not (JsonValueKind.True or JsonValueKind.False))
        {
            throw Unsupported(
                "its Sequence's Split step's invert is not a boolean",
                "tokenizers 0.23.1 expects true or false there and refuses a file whose type disagrees");
        }
        bool invert = invertElement.ValueKind == JsonValueKind.True;
        // behavior_unknown freezes "Nonsense" refused as "unknown variant `Nonsense`, ..." --
        // a lowercase "isolated" refusing the same way is only spec 0145's D6, not frozen here.
        SplitBehavior behavior = behaviorElement.GetString() switch
        {
            "Isolated" => SplitBehavior.Isolated,
            "Removed" => SplitBehavior.Removed,
            "MergedWithPrevious" => SplitBehavior.MergedWithPrevious,
            "MergedWithNext" => SplitBehavior.MergedWithNext,
            "Contiguous" => SplitBehavior.Contiguous,
            _ => throw Unsupported(
                $"its Sequence's Split step declares behavior '{behaviorElement.GetString()}'",
                "tokenizers 0.23.1 accepts only Removed, Isolated, MergedWithPrevious, MergedWithNext and Contiguous, and refuses the file identically"),
        };

        if (OptionalBoolean(byteLevelStep, AddPrefixSpaceProperty) is not bool addPrefixSpace)
        {
            throw Unsupported(
                "its Sequence's ByteLevel step declares no add_prefix_space",
                "tokenizers 0.23.1 has no default for that field and refuses the file identically, where accepting it here would invent the value that decides whether a leading space is added");
        }

        // Re-splits what Split produced unless off (docs/equivalence.md's Sequence
        // row); absent means true, and before #143 this field went unread entirely.
        bool useRegex = OptionalBoolean(byteLevelStep, "use_regex") ?? true;
        var step = new BpeSplitStep(pattern, behavior, invert);
        // Never the mode, however useRegex reads: the Split step above is a split.
        return (true, addPrefixSpace, step, useRegex ? BpePatterns.Gpt2 : null, false);
    }

    /// <summary>
    /// The pattern a <c>Split</c> step declares, in either spelling: <c>{"Regex": ...}</c>
    /// as written, or <c>{"String": ...}</c> -- the literal <c>pre_tokenizers.Split("|",
    /// "isolated")</c> writes -- escaped into the regex matching exactly it.
    /// </summary>
    /// <remarks>
    /// Its own method so <see cref="ReadBpeSequencePreTokenizer"/> keeps the cognitive
    /// complexity the build enforces: it already reads behavior and invert, and three
    /// more branches in place take it past the limit.
    /// </remarks>
    private static string ReadSplitPattern(JsonElement split)
    {
        string? regex = null;
        string? literal = null;
        if (split.TryGetProperty("pattern", out JsonElement pattern) && pattern.ValueKind == JsonValueKind.Object)
        {
            // Both keys, not both readable: {"Regex": null, "String": "|"} names the two
            // spellings just as much, and Serde refuses that node rather than reading one.
            if (pattern.TryGetProperty("Regex", out _) && pattern.TryGetProperty("String", out _))
            {
                throw Unsupported(
                    "its Sequence's Split step declares both pattern.Regex and pattern.String",
                    "tokenizers writes exactly one of the two, so a file carrying both is not something the reference produces and choosing a winner would invent behaviour rather than reproduce it");
            }
            regex = OptionalString(pattern, "Regex");
            literal = OptionalString(pattern, "String");
        }

        if (regex is not null)
        {
            return regex;
        }
        if (literal is not null)
        {
            // A literal has no regex flavour to lose, so escaping reproduces it exactly
            // where a Replace normalizer's Rust pattern above cannot -- bpe_split_literal.json.
            return Regex.Escape(literal);
        }
        throw Unsupported(
            "its Sequence's Split step declares neither pattern.Regex nor pattern.String as a string",
            "tokenizers spells a Split pattern as one of those two and nothing else, so a node carrying neither -- or carrying one whose value is not a string -- names no pattern to reproduce");
    }

    /// <summary>
    /// Refuses a <c>decoder</c> that could not have produced this file's own
    /// pre-tokenizer: a byte-level model whose decoder is not <c>ByteLevel</c>, or
    /// the reverse.
    /// </summary>
    /// <remarks>
    /// Checked, not applied -- Lodestar only encodes -- but a mismatch means the file
    /// won't round trip through <c>tokenizers</c> either, so it is caught here
    /// rather than as corrupt text from <see cref="BpeTokenizer.Decode(IReadOnlyList{int}, bool)"/>.
    /// </remarks>
    private static void EnsureDecoderMatchesModel(JsonElement root, bool byteLevel)
    {
        if (!root.TryGetProperty("decoder", out JsonElement decoder) || decoder.ValueKind == JsonValueKind.Null)
        {
            return;
        }
        string type = OptionalString(decoder, "type") ?? UntypedName;
        bool decoderIsByteLevel = string.Equals(type, "ByteLevel", StringComparison.Ordinal);
        EnsureByteLevelDecoderDeclaresAddPrefixSpace(decoder, decoderIsByteLevel);
        if (decoderIsByteLevel == byteLevel)
        {
            return;
        }
        // Not Unsupported(...): its fixed "embeddings that do not match the model" tail
        // would misdescribe this -- Encode is unaffected, only Decode would corrupt.
        throw new InvalidDataException(
            $"The {SourceName} pre_tokenizer describes a {(byteLevel ? "byte-level" : "non-byte-level")} model but its decoder is '{type}', which would not decode the tokens it produces.");
    }

    /// <summary>
    /// The decoder-side counterpart of <see cref="ReadByteLevelPreTokenizer"/>'s own
    /// check -- see <c>docs/guides/embeddings.md</c>'s refused-files list for why
    /// <c>add_prefix_space</c> has no default in any of its three positions.
    /// </summary>
    /// <remarks>
    /// Not routed through <see cref="Unsupported(string, string)"/>: the reference
    /// cannot even parse the file, which is a different reason to refuse than
    /// tokenizing it differently.
    /// </remarks>
    private static void EnsureByteLevelDecoderDeclaresAddPrefixSpace(JsonElement decoder, bool decoderIsByteLevel)
    {
        if (decoderIsByteLevel && OptionalBoolean(decoder, AddPrefixSpaceProperty) is null)
        {
            throw new InvalidDataException(
                $"The {SourceName} decoder is 'ByteLevel' but declares no add_prefix_space, which tokenizers has no default for and refuses too.");
        }
    }

    private enum PipelineKind
    {
        WordPiece,
        Unigram,
    }

    private static void EnsurePipelineIsReproduced(JsonElement root, PipelineKind kind)
    {
        RejectNonNull(root, "truncation", "Lodestar tokenizers do not truncate");
        RejectNonNull(root, "padding", "Lodestar tokenizers do not pad");
        RejectNonNull(root, "post_processor", "Lodestar tokenizers do not insert special tokens such as [CLS] and [SEP]");
        EnsurePreTokenizerIsReproduced(root, kind);
    }

    private static void EnsurePreTokenizerIsReproduced(JsonElement root, PipelineKind kind)
    {
        if (!root.TryGetProperty("pre_tokenizer", out JsonElement pre) || pre.ValueKind == JsonValueKind.Null)
        {
            // Absent means tokenizers hands the whole string to the model; Lodestar
            // always segments it (Whitespace or Metaspace), so accepting it would tokenize differently.
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
        if (OptionalBoolean(pre, AddPrefixSpaceProperty) is false)
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
    /// See <c>docs/equivalence.md</c>'s Unigram loader row for what <c>Precompiled</c>
    /// shares with <c>spiece.model</c> (read since #75, through the same
    /// <see cref="PrecompiledNormalizer"/>) and why <c>NFKC</c> is still refused.
    /// <c>Lowercase</c>/<c>BertNormalizer</c> belong to the WordPiece pipeline
    /// instead; absent or <c>null</c> is identity.
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

        // `is null or { Length: 0 }`, not string.IsNullOrEmpty: netstandard2.0's reference
        // assembly doesn't annotate that method, so the compiler would still want a `!` after.
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

            case SequenceName:
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
            throw Unsupported("its BertNormalizer pads CJK characters", "Lodestar does not reproduce that step");
        }
        // tokenizers strips accents when strip_accents is true, or absent with lowercase
        // on -- reading an absent value as "off" would accept a file Python strips accents for.
        if (OptionalBoolean(normalizer, "strip_accents") ?? (OptionalBoolean(normalizer, "lowercase") ?? true))
        {
            throw Unsupported("its BertNormalizer strips accents", "Lodestar does not reproduce that step");
        }
        if (OptionalBoolean(normalizer, "clean_text") ?? true)
        {
            throw Unsupported("its BertNormalizer cleans control characters", "Lodestar does not reproduce that step");
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
