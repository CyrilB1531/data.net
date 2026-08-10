using System.Text.Json;
using DataNet.Embeddings.Tokenization;
using DataNet.Internal.Persistence;

namespace DataNet.Embeddings.Persistence;

/// <summary>
/// Reads a HuggingFace <c>tokenizer.json</c> — the WordPiece, Unigram or BPE
/// model it declares, together with the settings that change tokenization.
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
/// The <c>decoder</c> section is accepted unchecked for WordPiece and Unigram:
/// it affects <c>decode</c> only, and <see cref="WordPieceTokenizer"/> and
/// <see cref="SentencePieceTokenizer"/> only encode. <see cref="BpeTokenizer"/>
/// does decode, so <see cref="LoadBpe(string, ArtifactLoadOptions?)"/> is the
/// exception: it refuses a <c>decoder</c> whose byte-level-ness disagrees with
/// the model's own, which would silently corrupt <see cref="BpeTokenizer.Decode(System.Collections.Generic.IReadOnlyList{int}, bool)"/>
/// rather than merely go unused.
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
/// <para>
/// <see cref="LoadBpe(string, ArtifactLoadOptions?)"/> reads the third model
/// type this file format can declare: GPT-2's byte-level BPE, the classic
/// (non-byte-level) BPE lineage, and the <c>Split</c>-then-<c>ByteLevel</c>
/// shape Llama-3 and Qwen2 use. See <see cref="BpeTokenizer"/> and
/// <c>docs/decisions/0017-bpe-parity-scope.md</c> for what is and is not
/// proven for each.
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

    /// <summary>Reads the BPE model declared by <paramref name="source"/>.</summary>
    /// <param name="source">The <c>tokenizer.json</c> bytes; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <exception cref="InvalidDataException">The file is malformed, exceeds a limit, declares a different model type, or describes a pipeline this library does not reproduce.</exception>
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
    public static BpeVocabulary LoadBpe(string path, ArtifactLoadOptions? options = null)
    {
        using FileStream file = JsonArtifact.OpenRead(path);
        return LoadBpe(file, options);
    }

    /// <summary>Asynchronous counterpart of <see cref="LoadBpe(Stream, ArtifactLoadOptions?)"/>.</summary>
    /// <param name="source">The <c>tokenizer.json</c> bytes; never disposed by this method.</param>
    /// <param name="options">Bounds applied while reading, or <c>null</c> for the defaults.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
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
        ReadAddedTokens(root, vocab, limits, reproducesFlags: false);
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
    /// <param name="root">The <c>tokenizer.json</c> root object.</param>
    /// <param name="vocab">The model's vocabulary; entries it lacks are folded in.</param>
    /// <param name="limits">Bounds applied while reading.</param>
    /// <param name="reproducesFlags">
    /// Whether the caller's tokenizer applies <c>lstrip</c>, <c>rstrip</c> and
    /// <c>single_word</c> itself — the BPE path, whose <see cref="AddedTokenScanner"/>
    /// matches the table as literal text ahead of the model and reads the flags
    /// rather than needing them to sit at their defaults. WordPiece merely folds an
    /// added token into its vocabulary, matchable as a whole word only, so a flag
    /// that would change that is refused instead.
    /// </param>
    /// <param name="matchedLiterally">
    /// When non-<see langword="null"/>, <em>every</em> entry of the table is recorded
    /// here — the BPE path, where the table is scanned as literal text ahead of the
    /// model rather than merely folded into the vocabulary, so an entry
    /// <c>model.vocab</c> already declares is not the no-op it is for WordPiece.
    /// </param>
    private static void ReadAddedTokens(
        JsonElement root,
        Dictionary<string, int> vocab,
        in ArtifactLimits limits,
        bool reproducesFlags,
        List<AddedToken>? matchedLiterally = null)
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
            FoldAddedToken(token, content, id, vocab, limits, reproducesFlags);
            matchedLiterally?.Add(new AddedToken(content, id)
            {
                Lstrip = OptionalBoolean(token, "lstrip") is true,
                Rstrip = OptionalBoolean(token, "rstrip") is true,
                SingleWord = OptionalBoolean(token, "single_word") is true,
                Special = OptionalBoolean(token, "special") is true,
            });
        }
    }

    /// <summary>Folds one <c>added_tokens</c> entry into <paramref name="vocab"/>, or checks it agrees with what is there.</summary>
    private static void FoldAddedToken(
        JsonElement token,
        string content,
        int id,
        Dictionary<string, int> vocab,
        in ArtifactLimits limits,
        bool reproducesFlags)
    {
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
            // Folding is a no-op here regardless of caller: the id is already what
            // model.vocab gives this text, so there is nothing this branch adds that
            // the matching flags could change. A caller that reproduces them (BPE)
            // reads them from the recorded AddedToken instead; one that does not
            // (WordPiece) never applies them either way.
            return;
        }

        if (!reproducesFlags)
        {
            EnsureAddedTokenMatchesPlainly(content, token);
        }
        vocab[content] = id;
        limits.CheckVocabularySize(vocab.Count);
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

    /// <summary>
    /// Reads a BPE model: its vocabulary, its ranked merge table, and the pipeline
    /// flags that decide how text reaches them.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="ReadWordPiece"/> and <see cref="ReadUnigram"/>, the
    /// pre-tokenizer here is not merely accepted or refused: <c>ByteLevel</c>,
    /// <c>Whitespace</c>, and a <c>Sequence</c> of <c>Split</c> then <c>ByteLevel</c>
    /// each set <see cref="BpeVocabulary.ByteLevel"/>, <see cref="BpeVocabulary.AddPrefixSpace"/>
    /// and <see cref="BpeVocabulary.PreTokenizerPattern"/> differently, because stock
    /// GPT-2 declares a bare <c>ByteLevel</c> node with no <c>Split</c> at all.
    /// </remarks>
    private static BpeVocabulary ReadBpe(JsonElement root, in ArtifactLimits limits)
    {
        JsonElement model = RequireObject(root, "model");
        EnsureModelType(model, "BPE");
        EnsureByteFallbackIsOff(model);
        EnsureBpeModelSettingsAreReproduced(model);
        EnsureBpeNormalizerIsAbsent(root);
        RejectNonNull(root, "truncation", "DataNet tokenizers do not truncate");
        RejectNonNull(root, "padding", "DataNet tokenizers do not pad");
        RejectNonNull(root, "post_processor", "DataNet tokenizers do not insert special tokens such as [CLS] and [SEP]");
        (bool byteLevel, bool addPrefixSpace, string? pattern) = ReadBpePreTokenizer(root);
        EnsureDecoderMatchesModel(root, byteLevel);

        Dictionary<string, int> vocab = ReadBpeVocab(model, limits);
        List<MergePair> merges = ReadBpeMerges(model, limits);
        int skippedMerges = merges.Count(pair => !vocab.ContainsKey(pair.Left) || !vocab.ContainsKey(pair.Right));
        List<AddedToken> addedTokens = ReadBpeAddedTokens(root, vocab, limits);

        return new BpeVocabulary(vocab, merges)
        {
            AddedTokens = addedTokens,
            ByteLevel = byteLevel,
            AddPrefixSpace = addPrefixSpace,
            IgnoreMerges = OptionalBoolean(model, "ignore_merges") ?? false,
            SkippedMerges = skippedMerges,
            EndOfWordSuffix = OptionalString(model, "end_of_word_suffix"),
            // ContinuingSubwordPrefix is deliberately not carried across: a non-null one
            // is refused above, so reading it here could only ever restate that null,
            // and a property that is read but never applied is what made this a bug.
            UnkToken = OptionalString(model, "unk_token"),
            PreTokenizerPattern = pattern,
        };
    }

    /// <summary>
    /// Refuses the three <c>model</c> settings that change what BPE produces and that
    /// <see cref="BpeTokenizer"/> does not apply.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Each of these is read out of a shipped <c>tokenizer.json</c> today and each is
    /// measurably not a no-op, so accepting one is a file that tokenizes differently
    /// here than in Python without saying so. Verified against <c>tokenizers</c>
    /// 0.23.1: with <c>continuing_subword_prefix="##"</c>, the vocabulary
    /// <c>{a, b, ##b, ab, a##b}</c> and the single merge <c>("a", "##b")</c>, Python
    /// encodes "ab" to the one id of <c>ab</c> where the same model without the prefix
    /// gives two; <c>fuse_unk</c> collapses a run of uncovered characters into one
    /// unknown token where this tokenizer always emits one per code point.
    /// <c>dropout</c> is a training-time regularizer that drops merges at random,
    /// which no deterministic tokenizer can reproduce at all.
    /// </para>
    /// <para>
    /// Refused by name rather than implemented: support is a feature, and a file
    /// naming one of these deserves to be told which one rather than to be tokenized
    /// plausibly and wrongly.
    /// </para>
    /// </remarks>
    private static void EnsureBpeModelSettingsAreReproduced(JsonElement model)
    {
        if (OptionalString(model, "continuing_subword_prefix") is { } prefix)
        {
            throw Unsupported(
                $"its model declares continuing_subword_prefix '{prefix}'",
                "HuggingFace prefixes every non-initial symbol with it before merging, where BpeTokenizer merges the symbols as they stand");
        }
        if (OptionalBoolean(model, "fuse_unk") is true)
        {
            throw Unsupported(
                "its model declares fuse_unk",
                "HuggingFace then collapses a run of uncovered characters into a single unknown token, where BpeTokenizer emits one per code point");
        }
        if (model.TryGetProperty("dropout", out JsonElement dropout) && dropout.ValueKind != JsonValueKind.Null)
        {
            throw Unsupported(
                "its model declares dropout",
                "that drops merges at random during tokenization, which no deterministic tokenizer reproduces");
        }
    }

    /// <summary>
    /// Refuses any normalizer, since <see cref="BpeTokenizer"/> applies none.
    /// </summary>
    /// <remarks>
    /// The counterpart of <see cref="ReadLowercaseFrom"/> for WordPiece and
    /// <see cref="ReadUnigramNormalizer"/> for Unigram, and the one this reader was
    /// missing: a BPE file declaring <c>NFC</c>, <c>NFKC</c>, <c>Replace</c> or a
    /// <c>Sequence</c> of them would otherwise load and skip the normalization in
    /// silence. Absent or <c>null</c> is identity and is what every model in scope
    /// declares.
    /// </remarks>
    private static void EnsureBpeNormalizerIsAbsent(JsonElement root)
    {
        if (!root.TryGetProperty("normalizer", out JsonElement normalizer) || normalizer.ValueKind == JsonValueKind.Null)
        {
            return;
        }
        string type = OptionalString(normalizer, "type") ?? UntypedName;
        throw Unsupported(
            $"its normalizer is '{type}'",
            "BpeTokenizer normalizes nothing, so every rule this one declares would go unapplied");
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
    private static MergePair ReadBpeMerge(JsonElement entry, int index)
    {
        if (entry.ValueKind == JsonValueKind.String)
        {
            string line = entry.GetString()!;
            // Exactly one space, not merely a first one. Python splits the whole line
            // and refuses it unless it yields two fields, so "a b c" and " a b" are
            // errors there where splitting on the first space would silently load
            // them as ("a", "b c") and ("", "a b"). A trailing space is not an error:
            // "a " splits into two fields, the second empty, and Python takes it.
            // CA1307 (specify StringComparison): the overload it asks for —
            // string.IndexOf(char, StringComparison) / string.Replace(string, string?,
            // StringComparison) — does not exist on netstandard2.0, which this assembly
            // targets. Both calls are ordinal on every runtime that has them, so the
            // suggestion would change nothing but the compilation.
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
    /// Reads the whole <c>added_tokens</c> table into
    /// <see cref="BpeVocabulary.AddedTokens"/>, which is a property of its own rather
    /// than a fold into <see cref="BpeVocabulary.Vocab"/> because
    /// <see cref="BpeTokenizer"/> matches added tokens as literal text before any
    /// merging happens.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The <em>whole</em> table, intersection with <c>model.vocab</c> included. That
    /// intersection is not redundant here the way it is for WordPiece: HuggingFace
    /// lists a special token in both tables — <c>&lt;|endoftext|&gt;</c> is id 50256 in
    /// GPT-2's own <c>model.vocab</c> <em>and</em> in its <c>added_tokens</c> — and
    /// <see cref="BpeTokenizer"/>'s pre-merge scan reads nothing but this property.
    /// Subtracting the intersection would therefore drop exactly the tokens the scan
    /// exists for, and <c>&lt;|endoftext|&gt;</c> would tokenize as the eight ordinary
    /// pieces its characters merge into.
    /// </para>
    /// <para>
    /// <paramref name="vocab"/> is copied rather than written to, so
    /// <see cref="BpeVocabulary.Vocab"/> stays what <c>model.vocab</c> declared; the
    /// copy is what gives the id-agreement check something to check against.
    /// </para>
    /// </remarks>
    private static List<AddedToken> ReadBpeAddedTokens(JsonElement root, Dictionary<string, int> vocab, in ArtifactLimits limits)
    {
        var withAdded = new Dictionary<string, int>(vocab, StringComparer.Ordinal);
        var added = new List<AddedToken>();
        ReadAddedTokens(root, withAdded, limits, reproducesFlags: true, matchedLiterally: added);
        return added;
    }

    /// <summary>
    /// Validates the pre-tokenizer and derives the three flags <see cref="BpeVocabulary"/>
    /// carries independently: whether the model is byte-level, whether a space is
    /// prepended, and the pattern text is split on.
    /// </summary>
    private static (bool ByteLevel, bool AddPrefixSpace, string? Pattern) ReadBpePreTokenizer(JsonElement root)
    {
        if (!root.TryGetProperty("pre_tokenizer", out JsonElement pre) || pre.ValueKind == JsonValueKind.Null)
        {
            // Absent is the classic (non-byte-level) lineage's own default: BpeTokenizer
            // falls back to word-boundary splitting when PreTokenizerPattern is null.
            return (false, false, null);
        }

        string type = OptionalString(pre, "type") ?? UntypedName;
        return type switch
        {
            "Whitespace" => (false, false, null),
            "ByteLevel" => ReadByteLevelPreTokenizer(pre),
            "Sequence" => ReadBpeSequencePreTokenizer(pre),
            _ => throw Unsupported(
                $"its pre_tokenizer is '{type}'",
                "BpeTokenizer reproduces ByteLevel, a Sequence of Split then ByteLevel, and Whitespace only"),
        };
    }

    /// <summary>
    /// A bare <c>ByteLevel</c> pre-tokenizer -- what stock GPT-2 declares, with no
    /// <c>Split</c> node of its own: <c>ByteLevel</c> does the splitting, on the
    /// pattern <see cref="BpePatterns.Gpt2"/> states.
    /// </summary>
    /// <remarks>
    /// <c>use_regex</c> defaults to <see langword="true"/> and stock GPT-2 omits it, so
    /// that pattern is what stock GPT-2 gets. Turned off, HuggingFace hands the whole
    /// normalized string to the model as one piece -- refused here, because the nearest
    /// thing this library has is <see cref="BpePreTokenizer"/>'s word-boundary
    /// fallback, which discards the whitespace between the words and so cannot round
    /// trip, the one guarantee byte-level BPE exists to make.
    /// </remarks>
    private static (bool ByteLevel, bool AddPrefixSpace, string? Pattern) ReadByteLevelPreTokenizer(JsonElement pre)
    {
        if (OptionalBoolean(pre, "use_regex") is false)
        {
            throw Unsupported(
                "its ByteLevel pre_tokenizer has use_regex off",
                "HuggingFace then passes the whole text to the model as one piece, where BpeTokenizer would split it on word boundaries and drop the whitespace between them");
        }
        bool addPrefixSpace = OptionalBoolean(pre, "add_prefix_space") ?? true;
        return (true, addPrefixSpace, BpePatterns.Gpt2);
    }

    /// <summary>
    /// A <c>Sequence</c> of exactly a <c>Split</c> step then a <c>ByteLevel</c> step --
    /// what Llama-3 and Qwen2 declare, splitting on their own pattern before the bytes
    /// are mapped rather than delegating the split to <c>ByteLevel</c> itself.
    /// </summary>
    private static (bool ByteLevel, bool AddPrefixSpace, string? Pattern) ReadBpeSequencePreTokenizer(JsonElement pre)
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

        if (!split.TryGetProperty("pattern", out JsonElement pattern)
            || !pattern.TryGetProperty("Regex", out JsonElement regexElement)
            || regexElement.ValueKind != JsonValueKind.String)
        {
            throw Unsupported(
                "its Sequence's Split step has no pattern.Regex",
                "BpeTokenizer reproduces a regex Split pattern only");
        }

        bool addPrefixSpace = OptionalBoolean(byteLevelStep, "add_prefix_space") ?? false;
        return (true, addPrefixSpace, regexElement.GetString());
    }

    /// <summary>
    /// Refuses a <c>decoder</c> that could not have produced the pre-tokenizer this
    /// file also declares: a byte-level model whose decoder is not <c>ByteLevel</c>,
    /// or the reverse.
    /// </summary>
    /// <remarks>
    /// <c>DataNet</c>'s tokenizers encode, not decode, so this is checked rather than
    /// applied -- but a mismatch here means the file will not round trip through
    /// <c>tokenizers</c> itself either, which makes it worth catching at load time
    /// rather than as corrupt text out of <see cref="BpeTokenizer.Decode(IReadOnlyList{int}, bool)"/>.
    /// An absent <c>decoder</c> is fine: it is what <c>models.BPE</c> built in code
    /// produces.
    /// </remarks>
    private static void EnsureDecoderMatchesModel(JsonElement root, bool byteLevel)
    {
        if (!root.TryGetProperty("decoder", out JsonElement decoder) || decoder.ValueKind == JsonValueKind.Null)
        {
            return;
        }
        string type = OptionalString(decoder, "type") ?? UntypedName;
        bool decoderIsByteLevel = string.Equals(type, "ByteLevel", StringComparison.Ordinal);
        if (decoderIsByteLevel == byteLevel)
        {
            return;
        }
        // Not routed through Unsupported(...): its fixed "would produce embeddings that
        // do not match the model" tail would misdescribe this failure. Every other
        // Unsupported(...) call in this file refuses something that changes what Encode
        // produces; a decoder mismatch does not -- Encode is unaffected, only Decode
        // would corrupt -- so this gets its own, accurate message instead.
        throw new InvalidDataException(
            $"The {SourceName} pre_tokenizer describes a {(byteLevel ? "byte-level" : "non-byte-level")} model but its decoder is '{type}', which would not decode the tokens it produces.");
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
