using System.Buffers;
using System.Text;
using Lodestar.Internal.Persistence;

namespace Lodestar.Embeddings.Tokenization;

/// <summary>Byte-pair-encoding tokenizer, character-level and byte-level, reproducing HuggingFace <c>tokenizers</c>' <c>models.BPE</c>.</summary>
/// <remarks>
/// The algorithm — symbol assignment, the added-token pre-pass, the merge loop — is the
/// guide's BPE section and equivalence.md's BPE rows; this type does not restate it. Merge
/// pairs are resolved to pairs of ids once, at construction, so the loop compares integers
/// rather than looking candidates up by string. Thread-safe after construction.
/// </remarks>
public sealed class BpeTokenizer : ISubwordTokenizer
{
    private const int StackThreshold = 256;

    /// <summary>No such neighbour: the ends of <see cref="Merge"/>'s list, and every symbol merged away.</summary>
    /// <remarks>
    /// One sentinel serves both: a merged-away symbol never needs a successor again, so a
    /// dead tail and a live one both correctly read <see cref="End"/> — <see cref="Applies"/>
    /// asks only whether there is a right-hand symbol to merge with, which is no either way.
    /// </remarks>
    private const int End = -1;

    /// <summary>UTF-8 for decoding only: a byte sequence that is not well-formed becomes U+FFFD.</summary>
    /// <remarks>
    /// Not <see cref="JsonArtifact.Utf8NoBom"/>, which throws and is shared with the
    /// persistence layer and with <see cref="Encode"/>'s own byte conversion, where
    /// refusing is right. The asymmetry is deliberate and matches the reference:
    /// strict on the way in, forgiving on the way out. See decision 0023.
    /// </remarks>
    private static readonly UTF8Encoding Utf8Lossy = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: false);

    private readonly Dictionary<string, int> _vocab;
    /// <summary>
    /// The model's own vocabulary, without the added tokens <see cref="_vocab"/>
    /// folds in — the map that answers "does the model cover this symbol".
    /// </summary>
    /// <remarks>
    /// The two are not interchangeable: an added token absent from <c>model.vocab</c> still
    /// has an id and answers <c>token_to_id</c> via <see cref="_vocab"/>, but does not cover
    /// the character it spells — equivalence.md's <c>tokenizer.add_tokens</c> row has the
    /// measurement. Coverage therefore reads this one.
    /// </remarks>
    private readonly Dictionary<string, int> _modelVocab;
    private readonly string[] _tokens;          // id -> token, the inverse of _vocab
    private readonly Dictionary<long, int> _ranks;   // (left << 32 | right) -> rank
    private readonly int[] _merged;             // rank -> the id the pair becomes
    private readonly BpePreTokenizer _split;
    // Two scanners: AddedToken.Normalized decides which one an entry joins, and
    // the two are matched against different strings. See EncodeGap.
    private readonly AddedTokenScanner _rawScanner;
    private readonly AddedTokenScanner _normalizedScanner;
    private readonly NormalizationForm[] _forms;
    private readonly MetaspaceEscape? _metaspace;
    private readonly BpeDecoderSteps? _decoder;
    private readonly HashSet<int> _addedIds;
    private readonly string? _endOfWord;
    private readonly string? _continuingPrefix;
    private readonly int _unkId;
    private readonly bool _hasUnk;
    private readonly bool _byteLevel;
    private readonly bool _byteFallback;
    private readonly bool _ignoreMerges;
    private readonly bool _fuseUnk;

    /// <summary>Creates a tokenizer from a loaded BPE model.</summary>
    /// <param name="vocabulary">A vocabulary from <see cref="Persistence.BpeFilesLoader"/> or <see cref="Persistence.TokenizerJsonLoader"/>.</param>
    /// <exception cref="ArgumentException">
    /// The declared unknown token is not in the vocabulary; or a merge names a
    /// token the vocabulary does not declare, or produces one it does not; or a
    /// byte-level vocabulary declares a continuing subword prefix, which this
    /// tokenizer would apply to its merges and not to its symbols — see
    /// <see cref="EnsureByteLevelDeclaresNoContinuingPrefix"/>; or <see cref="BpeVocabulary.PreSplit"/>
    /// declares a <see cref="SplitBehavior"/> outside its five defined values — see
    /// <see cref="EnsureSplitBehaviorIsDefined"/>; or the vocabulary does not say how it
    /// is split, or says it two ways at once — see <see cref="EnsurePreTokenizerIsDeclared"/>.
    /// </exception>
    public BpeTokenizer(BpeVocabulary vocabulary)
    {
        Guard.NotNull(vocabulary);
        EnsureByteLevelDeclaresNoContinuingPrefix(vocabulary);
        EnsureSplitBehaviorIsDefined(vocabulary);
        EnsurePreTokenizerIsDeclared(vocabulary);
        _endOfWord = vocabulary.EndOfWordSuffix;
        _continuingPrefix = vocabulary.ContinuingSubwordPrefix;
        _byteLevel = vocabulary.ByteLevel;
        _byteFallback = vocabulary.ByteFallback;
        _ignoreMerges = vocabulary.IgnoreMerges;
        _fuseUnk = vocabulary.FuseUnk;

        _forms = [.. vocabulary.NormalizationForms];
        _metaspace = vocabulary.Metaspace;
        _decoder = vocabulary.Decoder;
        (_vocab, _modelVocab, _tokens) = BuildVocabulary(vocabulary);

        _rawScanner = new AddedTokenScanner([.. vocabulary.AddedTokens.Where(t => !t.Normalized)]);
        _normalizedScanner = new AddedTokenScanner(
            [.. vocabulary.AddedTokens.Where(t => t.Normalized).Select(t => t with { Content = Normalize(t.Content) })]);
        _addedIds = [.. vocabulary.AddedTokens.Where(a => a.Special).Select(a => a.Id)];

        if (vocabulary.UnkToken is { } unk)
        {
            // The model's vocabulary, not the folded one; refusing at construction is
            // earlier than the reference — see equivalence.md's refused row.
            if (!_modelVocab.TryGetValue(unk, out _unkId))
            {
                throw new ArgumentException(
                    $"The unknown token '{unk}' is not in the vocabulary.", nameof(vocabulary));
            }
            _hasUnk = true;
        }

        _ranks = new Dictionary<long, int>(vocabulary.Merges.Count);
        _merged = new int[vocabulary.Merges.Count];
        for (int rank = 0; rank < vocabulary.Merges.Count; rank++)
        {
            MergePair pair = vocabulary.Merges[rank];
            // Against the model's vocabulary, not the folded one, and refused
            // rather than skipped — see equivalence.md's refused row.
            if (!_modelVocab.TryGetValue(pair.Left, out int left)
                || !_modelVocab.TryGetValue(pair.Right, out int right))
            {
                throw new ArgumentException(
                    $"The merge at rank {rank} names '{pair.Left}' and '{pair.Right}', "
                    + "and the vocabulary does not contain both.", nameof(vocabulary));
            }
            // A third shape: the reference panics here rather than raising — see
            // equivalence.md's refused row and the continuing-prefix row for the rest.
            string merged = pair.Left + StripContinuingPrefix(pair.Right);
            if (!_modelVocab.TryGetValue(merged, out int result))
            {
                throw new ArgumentException(
                    $"The merge at rank {rank} produces '{merged}', "
                    + "which the vocabulary does not contain.", nameof(vocabulary));
            }
            // A pair listed twice keeps its LAST occurrence, as the reference does:
            // tests/oracles/bpe_duplicate_merge.json, model "duplicate".
            _ranks[Key(left, right)] = rank;
            _merged[rank] = result;
        }

        _split = new BpePreTokenizer(
            vocabulary.PreSplit, vocabulary.PreTokenizerPattern, vocabulary.NoPreTokenizer,
            vocabulary.AddPrefixSpace);
    }

    /// <summary>Refuses a vocabulary that does not say how its text is split, or says it two ways at once.</summary>
    /// <remarks>
    /// Declaring nothing used to mean <see cref="BpePatterns.Whitespace"/>, and that
    /// spelling is now needed for the mode that splits nothing at all. Reinterpreting
    /// it would hand an existing caller a different token stream with nothing to say
    /// so, so it is refused instead — issue #122, and the spec's "three legal shapes"
    /// table. Same reason as <see cref="EnsureByteLevelDeclaresNoContinuingPrefix"/>:
    /// <see cref="BpeVocabulary"/> is public and constructible without a loader.
    /// </remarks>
    /// <param name="vocabulary">The vocabulary the constructor was handed.</param>
    private static void EnsurePreTokenizerIsDeclared(BpeVocabulary vocabulary)
    {
        bool declaresAPattern = vocabulary.PreSplit is not null || vocabulary.PreTokenizerPattern is not null;
        if (!declaresAPattern && !vocabulary.NoPreTokenizer)
        {
            throw new ArgumentException(
                "The vocabulary declares no PreSplit, no PreTokenizerPattern and no NoPreTokenizer, "
                + "so it does not say how its text is split. That shape used to mean the classic "
                + "word-boundary split: write PreTokenizerPattern = BpePatterns.Whitespace for it, "
                + "PreSplit for a Split step, or NoPreTokenizer = true for a model whose text reaches "
                + "the merge loop unsplit.",
                nameof(vocabulary));
        }

        if (declaresAPattern && vocabulary.NoPreTokenizer)
        {
            string declared = (vocabulary.PreSplit, vocabulary.PreTokenizerPattern) switch
            {
                (null, _) => "PreTokenizerPattern",
                (_, null) => "PreSplit",
                _ => "PreSplit and PreTokenizerPattern",
            };
            throw new ArgumentException(
                $"The vocabulary declares NoPreTokenizer and {declared} together, which contradict each "
                + "other: NoPreTokenizer means nothing is split, and a pattern is a split. Keep the "
                + "pattern and drop NoPreTokenizer, or drop the pattern and keep NoPreTokenizer.",
                nameof(vocabulary));
        }
    }

    /// <summary>Refuses a byte-level vocabulary that also declares a continuing subword prefix, the one pairing whose two halves this class would answer differently.</summary>
    /// <remarks>
    /// Why the disagreement would be silent is equivalence.md's <c>continuing_subword_prefix</c>
    /// row. A method rather than an inline <c>if</c>: <see cref="Persistence.TokenizerJsonLoader"/>
    /// already refuses the same pairing in a file, and this one exists only because
    /// <see cref="BpeVocabulary"/> is public and constructible without going through a loader.
    /// </remarks>
    /// <param name="vocabulary">The vocabulary the constructor was handed.</param>
    private static void EnsureByteLevelDeclaresNoContinuingPrefix(BpeVocabulary vocabulary)
    {
        if (vocabulary.ByteLevel && vocabulary.ContinuingSubwordPrefix is not null)
        {
            throw new ArgumentException(
                "The vocabulary is byte-level and declares the continuing subword prefix "
                + $"'{vocabulary.ContinuingSubwordPrefix}'. A byte-level model's symbols are never "
                + "prefixed here while a merge's right side is still stripped, so the two would disagree.",
                nameof(vocabulary));
        }
    }

    /// <summary>Refuses a <see cref="BpeVocabulary.PreSplit"/> whose <see cref="SplitBehavior"/> is not one of the five values the type defines.</summary>
    /// <remarks>
    /// <see cref="SplitBehavior"/> is public on a public record, so a hand-built
    /// <see cref="BpeVocabulary"/> can name a value no loader produces; left unchecked it
    /// surfaces deep inside <see cref="BpePreTokenizer"/>'s merge-loop switch, naming an
    /// internal parameter this constructor's own caller cannot see. Same reason as
    /// <see cref="EnsureByteLevelDeclaresNoContinuingPrefix"/>, for the other hand-built
    /// shape no loader guards.
    /// </remarks>
    /// <param name="vocabulary">The vocabulary the constructor was handed.</param>
    private static void EnsureSplitBehaviorIsDefined(BpeVocabulary vocabulary)
    {
        if (vocabulary.PreSplit is not { } preSplit)
        {
            return;
        }

        bool defined = preSplit.Behavior is SplitBehavior.Isolated or SplitBehavior.Removed
            or SplitBehavior.MergedWithPrevious or SplitBehavior.MergedWithNext or SplitBehavior.Contiguous;
        if (!defined)
        {
            throw new ArgumentException(
                $"The vocabulary's Split step declares behavior {(int)preSplit.Behavior}, "
                + "which is not one of the five SplitBehavior values.",
                nameof(vocabulary));
        }
    }

    /// <summary>Builds the folded vocabulary, the model-only one, and the id-to-token table.</summary>
    /// <remarks>
    /// <see cref="_tokens"/> is not simply <see cref="_vocab"/> inverted: for a normalized
    /// added token the two disagree on purpose. Measured against <c>tokenizers</c> 0.23.1,
    /// <c>token_to_id</c> answers the raw spelling and <c>id_to_token</c> the normalized
    /// one for such an entry -- an asymmetry in the reference itself, not a choice made here.
    /// </remarks>
    private (Dictionary<string, int> Vocab, Dictionary<string, int> ModelVocab, string[] Tokens) BuildVocabulary(BpeVocabulary vocabulary)
    {
        var vocab = new Dictionary<string, int>(vocabulary.Vocab.Count, StringComparer.Ordinal);
        int maxId = -1;
        foreach (KeyValuePair<string, int> entry in vocabulary.Vocab)
        {
            vocab[entry.Key] = entry.Value;
            maxId = Math.Max(maxId, entry.Value);
        }
        var modelVocab = new Dictionary<string, int>(vocab, StringComparer.Ordinal);
        foreach (AddedToken added in vocabulary.AddedTokens)
        {
            vocab[added.Content] = added.Id;
            maxId = Math.Max(maxId, added.Id);
        }

        var tokens = new string[maxId + 1];
        foreach (KeyValuePair<string, int> entry in vocabulary.Vocab)
        {
            tokens[entry.Value] = entry.Key;
        }
        foreach (AddedToken added in vocabulary.AddedTokens)
        {
            tokens[added.Id] = added.Normalized ? Normalize(added.Content) : added.Content;
        }

        return (vocab, modelVocab, tokens);
    }

    /// <summary>Tokenizes <paramref name="text"/> into sub-word tokens and their ids.</summary>
    /// <remarks>Matches <c>tokenizers.Tokenizer.encode(text)</c>, without the post-processor.</remarks>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// A byte-level model re-encodes <paramref name="text"/> to UTF-8, and byte_fallback
    /// re-encodes each uncovered symbol; either way an unpaired UTF-16 surrogate throws
    /// rather than substitutes, since both are lossless only over well-formed UTF-16.
    /// Neither declared, the classic path never encodes to UTF-8, so it cannot throw this.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Either a byte-level vocabulary missing one of the 256 alphabet characters
    /// (see <see cref="ByteLevelSymbols"/>), or, once a normalizer is declared, an
    /// unpaired surrogate in a gap -- <see cref="string.Normalize(NormalizationForm)"/>
    /// throws on that before the re-encoding above gets a chance to.
    /// </exception>
    public TokenizationResult Encode(string text)
    {
        Guard.NotNull(text);
        var tokens = new List<string>();
        var ids = new List<int>();
        var pieces = new List<string>();

        int pos = 0;
        bool first = true;
        while (pos < text.Length)
        {
            if (!_rawScanner.TryNext(text, pos, out int start, out int end, out var added))
            {
                EncodeGap(text, pos, text.Length, tokens, ids, pieces, first);
                break;
            }
            if (start > pos)
            {
                EncodeGap(text, pos, start, tokens, ids, pieces, first);
            }
            tokens.Add(text.Substring(start, end - start));
            ids.Add(added.Id);
            // An added token is a piece of its own, so it spends the prepend "first"
            // owes the opening one -- and every gap after this one follows a token.
            first = false;
            pos = end;
        }
        return new TokenizationResult(tokens, ids);
    }

    /// <summary>Looks up a literal vocabulary entry, added tokens included.</summary>
    /// <remarks>Matches <c>tokenizers.Tokenizer.token_to_id(token)</c>.</remarks>
    /// <param name="token">The token string.</param>
    /// <param name="id">Receives the id when the token is present.</param>
    public bool TryGetId(string token, out int id)
    {
        Guard.NotNull(token);
        return _vocab.TryGetValue(token, out id);
    }

    private static long Key(int left, int right) => ((long)left << 32) | (uint)right;

    /// <summary>Normalizes <c>text[from..to]</c>, which holds no raw added token, then splits it at the normalized ones.</summary>
    /// <remarks>
    /// Each gap is normalized on its own rather than once for the whole input:
    /// <see cref="WordPieceTokenizer"/> can reuse raw-text positions only because
    /// <c>ToLowerInvariant</c> preserves length, and every form here changes it.
    /// Order (normalize before <c>add_prefix_space</c>) and the no-op fast path
    /// below are D2 of
    /// docs/superpowers/specs/2026-08-13_0121_give-readbpe-the-normalizer-treatment.md.
    /// </remarks>
    private void EncodeGap(
        string text, int from, int to, List<string> tokens, List<int> ids, List<string> pieces, bool isFirstSplit)
    {
        if (_forms.Length == 0 && _metaspace is null && _normalizedScanner.IsEmpty)
        {
            EncodeSegment(text, from, to, tokens, ids, pieces);
            return;
        }

        string gap = Preprocess(text.Substring(from, to - from), isFirstSplit);
        int pos = 0;
        while (pos < gap.Length)
        {
            if (!_normalizedScanner.TryNext(gap, pos, out int start, out int end, out var added))
            {
                EncodeSegment(gap, pos, gap.Length, tokens, ids, pieces);
                break;
            }
            if (start > pos)
            {
                EncodeSegment(gap, pos, start, tokens, ids, pieces);
            }
            tokens.Add(gap.Substring(start, end - start));
            ids.Add(added.Id);
            pos = end;
        }
    }

    /// <summary>Normalizes, then escapes whitespace when the model declared an escape.</summary>
    /// <remarks>
    /// The order <see cref="SentencePieceTokenizer"/> already runs: the escape reads the
    /// normalized text, since both spellings decision 0050 §2 accepts sit at or after the
    /// normalizer. Added-token content goes through <see cref="Normalize"/> alone —
    /// escaping it would spell the entry with a symbol the file did not put there.
    /// </remarks>
    private string Preprocess(string text, bool isFirstSplit)
    {
        string normalized = Normalize(text);
        return _metaspace is null ? normalized : _metaspace.Apply(normalized, isFirstSplit);
    }

    /// <summary>Applies the declared forms in their declared order.</summary>
    private string Normalize(string text)
    {
        string normalized = text;
        foreach (NormalizationForm form in _forms)
        {
            normalized = normalized.Normalize(form);
        }
        return normalized;
    }

    /// <summary>Splits and merges the plain-text slice <c>text[start..end]</c>, which contains no added token.</summary>
    /// <remarks>
    /// <see cref="BpeVocabulary.AddPrefixSpace"/> is applied inside
    /// <see cref="BpePreTokenizer"/> now, per piece rather than here per segment: only
    /// that type knows whether a <c>Split</c> step ran first — equivalence.md's
    /// <c>Sequence([Split(pattern), ByteLevel(…)])</c> row. The length check is a plain early return now;
    /// what keeps an empty segment between two adjacent added tokens from becoming a
    /// <c>'Ġ'</c> Python does not emit is that type's own length check.
    /// </remarks>
    private void EncodeSegment(string text, int start, int end, List<string> tokens, List<int> ids, List<string> pieces)
    {
        int length = end - start;
        if (length == 0)
        {
            return;
        }

        pieces.Clear();
        _split.Split(text.Substring(start, length), pieces);
        foreach (string piece in pieces)
        {
            EncodePiece(piece, tokens, ids);
        }
    }

    private void EncodePiece(string piece, List<string> tokens, List<int> ids)
    {
        if (piece.Length == 0)
        {
            return;
        }

        // ignore_merges: a piece that is itself a vocabulary entry is emitted whole,
        // as Llama-3 declares it — see equivalence.md's `BPE(...)` row.
        if (_ignoreMerges)
        {
            string mapped = _byteLevel ? MapBytes(piece) : piece;
            if (_modelVocab.TryGetValue(mapped, out int whole))
            {
                ids.Add(whole);
                tokens.Add(_tokens[whole]);
                return;
            }
        }

        // Byte-level sizes by UTF-8 byte count: one character can become four bytes.
        // byte_fallback has the same shape, plus whatever Decorate adds per symbol.
        int capacity = _byteLevel || _byteFallback
            ? JsonArtifact.Utf8NoBom.GetByteCount(piece) + DecorationBytes(piece)
            : piece.Length;
        bool small = capacity <= StackThreshold;
        int[]? rented = small ? null : ArrayPool<int>.Shared.Rent(capacity);
        Span<int> symbols = small ? stackalloc int[capacity] : rented!.AsSpan(0, capacity);
        try
        {
            int count = _byteLevel ? ByteLevelSymbols(piece, symbols) : InitialSymbols(piece, symbols);
            count = Merge(symbols, count);
            for (int i = 0; i < count; i++)
            {
                ids.Add(symbols[i]);
                tokens.Add(_tokens[symbols[i]]);
            }
        }
        finally
        {
            if (rented is not null)
            {
                ArrayPool<int>.Shared.Return(rented);
            }
        }
    }

    /// <summary>An upper bound on the bytes <see cref="Decorate"/> can add to one piece's symbols.</summary>
    /// <remarks>
    /// Only a byte_fallback expansion pays for these: the prefix goes on every symbol but the
    /// first and the suffix on the last, and an expanded symbol carries them as bytes of its
    /// own. Counting one prefix per character is loose and never short.
    /// </remarks>
    private int DecorationBytes(string piece)
    {
        if (!_byteFallback)
        {
            return 0;
        }
        int prefix = _continuingPrefix is null ? 0 : JsonArtifact.Utf8NoBom.GetByteCount(_continuingPrefix);
        int suffix = _endOfWord is null ? 0 : JsonArtifact.Utf8NoBom.GetByteCount(_endOfWord);
        return (prefix * piece.Length) + suffix;
    }

    /// <summary>
    /// Fills <paramref name="symbols"/> with one id per Unicode code point, substituting the
    /// unknown-token id for an uncovered one, or dropping it if none is declared. Returns
    /// the count written, which can be less than <c>piece.Length</c>: characters can be
    /// dropped, and a surrogate pair is one code point, not two.
    /// </summary>
    /// <remarks>
    /// Splitting by <see cref="string"/> index would count a surrogate pair as two symbols
    /// where HuggingFace, iterating a Python <c>str</c>'s code points, counts one.
    /// </remarks>
    private int InitialSymbols(string piece, Span<int> symbols)
    {
        int count = 0;
        int i = 0;
        // SUBSTITUTED, not "id == _unkId": a covered character equal to unk_token
        // is not fused across — BpeFuseUnkTests's "qZ" vs "ZZ" cases pin it.
        bool previousWasSubstituted = false;
        while (i < piece.Length)
        {
            int width = char.IsHighSurrogate(piece[i])
                && i + 1 < piece.Length
                && char.IsLowSurrogate(piece[i + 1])
                ? 2
                : 1;
            bool last = i + width == piece.Length;
            string symbol = Decorate(piece, i, width, i == 0, last);
            if (_modelVocab.TryGetValue(symbol, out int id))
            {
                symbols[count++] = id;
                previousWasSubstituted = false;
            }
            else if (_byteFallback)
            {
                // byte_fallback: the decorated symbol is what is expanded, and it expands
                // before Merge, so byte pieces merge like any other symbol.
                count += ExpandToBytes(symbol, symbols.Slice(count));
                previousWasSubstituted = false;
            }
            else if (_hasUnk)
            {
                // fuse_unk: a run of uncovered points fuses to one unknown token,
                // before Merge runs — see equivalence.md's fuse_unk row.
                if (!_fuseUnk || !previousWasSubstituted)
                {
                    symbols[count++] = _unkId;
                }
                previousWasSubstituted = true;
            }
            i += width;
        }
        return count;
    }

    /// <summary>Writes one id per UTF-8 byte of <paramref name="symbol"/>, and returns how many.</summary>
    /// <remarks>
    /// Total by construction: <c>LoadBpe</c> refuses a byte_fallback vocabulary missing any of
    /// the 256 pieces, so every byte has one. A directly built vocabulary that does not is a
    /// caller's error, and the indexer says so rather than a stream saying nothing.
    /// </remarks>
    private int ExpandToBytes(string symbol, Span<int> symbols)
    {
        byte[] bytes = JsonArtifact.Utf8NoBom.GetBytes(symbol);
        for (int i = 0; i < bytes.Length; i++)
        {
            symbols[i] = _modelVocab[BytePieces.Name(bytes[i])];
        }
        return bytes.Length;
    }

    /// <summary>The vocabulary key for one code point of a piece: the characters themselves, plus whatever decoration its position calls for.</summary>
    /// <param name="piece">The pre-tokenized piece being walked.</param>
    /// <param name="at">The index of the code point's first <see cref="char"/>.</param>
    /// <param name="width">Its width in <see cref="char"/>s — two for a surrogate pair.</param>
    /// <param name="first">Whether it opens the piece.</param>
    /// <param name="last">Whether it ends the piece.</param>
    /// <remarks>
    /// Prefix then characters then suffix, composing on a symbol that is both — see
    /// <c>BpeContinuingPrefixTests.The_prefix_and_the_suffix_compose</c>, which pins
    /// <c>"ab"</c> to <c>['a', '##b&lt;/w&gt;']</c>.
    /// </remarks>
    private string Decorate(string piece, int at, int width, bool first, bool last)
    {
        string characters = piece.Substring(at, width);
        string prefix = !first && _continuingPrefix is not null ? _continuingPrefix : string.Empty;
        string suffix = last && _endOfWord is not null ? _endOfWord : string.Empty;

        return prefix.Length == 0 && suffix.Length == 0
            ? characters
            : prefix + characters + suffix;
    }

    /// <summary>The symbol without a leading continuing prefix, if it has one.</summary>
    /// <param name="symbol">A merge pair's right-hand side, as the file spells it.</param>
    private string StripContinuingPrefix(string symbol) =>
        _continuingPrefix is { Length: > 0 } prefix
            && symbol.StartsWith(prefix, StringComparison.Ordinal)
            ? symbol.Substring(prefix.Length)
            : symbol;

    /// <summary>Fills <paramref name="symbols"/> with one id per UTF-8 byte of <paramref name="piece"/>.</summary>
    /// <remarks>
    /// Unlike <see cref="InitialSymbols"/>, <c>_endOfWord</c> is never appended here. A
    /// byte-level model may still declare <see cref="BpeVocabulary.EndOfWordSuffix"/>; it is
    /// silently ignored rather than refused — equivalence.md's <c>continuing_subword_prefix</c>
    /// row records this beside the prefix's own, opposite choice.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// A byte-level vocabulary is missing one of the 256 base alphabet tokens —
    /// a broken model, not ordinary uncovered input.
    /// </exception>
    /// <exception cref="System.Text.EncoderFallbackException">
    /// <paramref name="piece"/> contains an unpaired surrogate; see <see cref="Encode"/>.
    /// </exception>
    private int ByteLevelSymbols(string piece, Span<int> symbols)
    {
        byte[] bytes = JsonArtifact.Utf8NoBom.GetBytes(piece);
        for (int i = 0; i < bytes.Length; i++)
        {
            string symbol = ByteLevelAlphabet.ToChar(bytes[i]).ToString();
            if (!_modelVocab.TryGetValue(symbol, out int id))
            {
                throw new ArgumentException(
                    $"The vocabulary has no entry for byte 0x{bytes[i]:X2} ('{symbol}'); it is not a byte-level model.");
            }
            symbols[i] = id;
        }
        return bytes.Length;
    }

    /// <summary>The piece as the byte alphabet renders it, which is how the vocabulary spells it.</summary>
    private static string MapBytes(string piece)
    {
        byte[] bytes = JsonArtifact.Utf8NoBom.GetBytes(piece);
        var mapped = new StringBuilder(bytes.Length);
        for (int i = 0; i < bytes.Length; i++)
        {
            mapped.Append(ByteLevelAlphabet.ToChar(bytes[i]));
        }
        return mapped.ToString();
    }

    /// <summary>Applies the lowest-ranked applicable merge until none applies. Returns the new symbol count.</summary>
    /// <remarks>
    /// Symbols are threaded on a doubly-linked list and candidate merges kept in a hand-rolled
    /// binary heap, validated when they come off the queue and dropped in silence when stale
    /// rather than hunted down at merge time — see decision 0017's "Merge loop" section for
    /// the scaling measurements that justified the rewrite over a rescan-and-shift loop, and
    /// for the leftmost-wins tie-break, which this reproduces from HuggingFace's own heap
    /// ordering rather than inventing.
    /// </remarks>
    private int Merge(Span<int> symbols, int count)
    {
        if (count < 2)
        {
            return count;
        }

        // Always rented, never stackalloc'd below a threshold, unlike EncodePiece: simpler
        // control flow, and one array for both spans is one fewer rental to give back.
        int capacity = QueueCapacity(count);
        int[] links = ArrayPool<int>.Shared.Rent(2 * count);
        try
        {
            long[] queue = ArrayPool<long>.Shared.Rent(capacity);
            try
            {
                return MergeQueued(symbols, count, links.AsSpan(0, 2 * count), queue.AsSpan(0, capacity));
            }
            finally
            {
                ArrayPool<long>.Shared.Return(queue);
            }
        }
        finally
        {
            ArrayPool<int>.Shared.Return(links);
        }
    }

    /// <summary>
    /// How many entries the queue can ever hold for <paramref name="count"/>
    /// symbols: the <c>count - 1</c> adjacent pairs it starts with, plus two per
    /// merge applied, of which there can be at most <c>count - 1</c> since each
    /// one removes a symbol and one always remains.
    /// </summary>
    private static int QueueCapacity(int count) => 3 * (count - 1);

    /// <summary>Runs the merge loop over a linked list of symbols and a queue of candidates.</summary>
    /// <param name="symbols">The symbols, rewritten in place; only the first <c>count</c> entries are read.</param>
    /// <param name="count">How many symbols there are, at least two.</param>
    /// <param name="links">Scratch space for the list: the first half is <c>prev</c>, the second <c>next</c>.</param>
    /// <param name="queue">Scratch space for the binary heap of candidate merges.</param>
    private int MergeQueued(Span<int> symbols, int count, Span<int> links, Span<long> queue)
    {
        Span<int> previous = links.Slice(0, count);
        Span<int> next = links.Slice(count, count);
        for (int i = 0; i < count; i++)
        {
            previous[i] = i - 1;
            next[i] = i + 1;
        }
        next[count - 1] = End;

        int size = 0;
        for (int i = 0; i + 1 < count; i++)
        {
            Offer(symbols, queue, ref size, i, i + 1);
        }

        while (size > 0)
        {
            long candidate = Take(queue, ref size);
            int at = (int)candidate;
            int rank = (int)(candidate >> 32);
            if (!Applies(symbols, next, at, rank))
            {
                continue;
            }

            symbols[at] = _merged[rank];
            Unlink(previous, next, at, next[at]);
            if (previous[at] != End)
            {
                Offer(symbols, queue, ref size, previous[at], at);
            }
            if (next[at] != End)
            {
                Offer(symbols, queue, ref size, at, next[at]);
            }
        }

        return Compact(symbols, next);
    }

    /// <summary>Queues the pair at <paramref name="left"/> and <paramref name="right"/>, if the merge table knows it.</summary>
    private void Offer(ReadOnlySpan<int> symbols, Span<long> queue, ref int size, int left, int right)
    {
        if (!_ranks.TryGetValue(Key(symbols[left], symbols[right]), out int rank))
        {
            return;
        }
        queue[size] = ((long)rank << 32) | (uint)left;
        size++;
        SiftUp(queue, size - 1);
    }

    /// <summary>Whether a candidate taken off the queue still describes an adjacent pair of exactly its rank.</summary>
    /// <remarks>
    /// Rank identifies the pair: the constructor gives each pair the index of its
    /// line in the merge table, and never two lines to one pair. So a candidate
    /// whose neighbours still resolve to its own rank is one whose two symbols
    /// are the two it was queued for, and any other outcome — no successor, no
    /// known pair, a different rank — is a candidate some earlier merge has
    /// overtaken.
    /// </remarks>
    private bool Applies(ReadOnlySpan<int> symbols, ReadOnlySpan<int> next, int at, int rank)
    {
        int right = next[at];
        return right != End
            && _ranks.TryGetValue(Key(symbols[at], symbols[right]), out int current)
            && current == rank;
    }

    /// <summary>Drops <paramref name="right"/> out of the list, joining its neighbour to <paramref name="left"/>.</summary>
    private static void Unlink(Span<int> previous, Span<int> next, int left, int right)
    {
        int after = next[right];
        next[left] = after;
        if (after != End)
        {
            previous[after] = left;
        }
        next[right] = End;
    }

    /// <summary>Rewrites the surviving symbols into <c>symbols[0..n]</c> in list order and returns <c>n</c>.</summary>
    /// <remarks>
    /// Position 0 always survives: a merge only ever unlinks the right-hand
    /// symbol of a pair, and nothing is to the left of the first one. Writing
    /// forward is safe for the same reason the list stays in order — the
    /// destination has never got past the position being read.
    /// </remarks>
    private static int Compact(Span<int> symbols, ReadOnlySpan<int> next)
    {
        int n = 1;
        for (int at = next[0]; at != End; at = next[at])
        {
            symbols[n] = symbols[at];
            n++;
        }
        return n;
    }

    /// <summary>Removes and returns the smallest entry of the heap.</summary>
    private static long Take(Span<long> queue, ref int size)
    {
        long best = queue[0];
        size--;
        if (size > 0)
        {
            queue[0] = queue[size];
            SiftDown(queue, size);
        }
        return best;
    }

    /// <summary>Moves the entry at <paramref name="at"/> up to where the heap order puts it.</summary>
    private static void SiftUp(Span<long> queue, int at)
    {
        long entry = queue[at];
        while (at > 0)
        {
            int parent = (at - 1) / 2;
            if (queue[parent] <= entry)
            {
                break;
            }
            queue[at] = queue[parent];
            at = parent;
        }
        queue[at] = entry;
    }

    /// <summary>Moves the root down to where the heap order puts it.</summary>
    private static void SiftDown(Span<long> queue, int size)
    {
        long entry = queue[0];
        int at = 0;
        int child = 1;
        while (child < size)
        {
            if (child + 1 < size && queue[child + 1] < queue[child])
            {
                child++;
            }
            if (queue[child] >= entry)
            {
                break;
            }
            queue[at] = queue[child];
            at = child;
            child = (2 * at) + 1;
        }
        queue[at] = entry;
    }

    /// <summary>Reassembles the text <paramref name="ids"/> encode.</summary>
    /// <remarks>
    /// Matches <c>tokenizers.Tokenizer.decode(ids, skip_special_tokens=…)</c>. Byte-exact only
    /// for a complete sequence <see cref="Encode"/> produced whole — decoded one id at a time,
    /// a byte sequence that is not well-formed UTF-8 becomes U+FFFD rather than throwing,
    /// matching the reference (decision 0023). <c>skipSpecialTokens</c> defaults to
    /// <see langword="false"/>, so <c>Decode(Encode(x)) == x</c> holds without passing it —
    /// except under the metaspace escape, which nothing here undoes (decision 0062).
    /// </remarks>
    /// <param name="ids">Token ids, e.g. from <see cref="Encode"/>.</param>
    /// <param name="skipSpecialTokens">Drop tokens whose <c>added_tokens</c> entry is <c>special</c> (<see cref="AddedToken.Special"/>), matching Python's <c>skip_special_tokens</c>.</param>
    /// <exception cref="ArgumentOutOfRangeException">An id is outside the vocabulary.</exception>
    public string Decode(IReadOnlyList<int> ids, bool skipSpecialTokens = false)
    {
        Guard.NotNull(ids);
        var buffer = new StringBuilder();
        var pending = new List<byte>();
        for (int i = 0; i < ids.Count; i++)
        {
            Append(buffer, pending, ids[i], skipSpecialTokens);
        }
        FlushBytes(buffer, pending);
        return Finish(buffer);
    }

    /// <summary>Reassembles the text <paramref name="ids"/> encode.</summary>
    /// <param name="ids">Token ids, e.g. from <see cref="Encode"/>.</param>
    /// <param name="skipSpecialTokens">
    /// Drop added tokens marked <c>special</c> instead of rendering them; see
    /// <see cref="Decode(IReadOnlyList{int}, bool)"/>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">An id is outside the vocabulary.</exception>
    public string Decode(ReadOnlySpan<int> ids, bool skipSpecialTokens = false)
    {
        var buffer = new StringBuilder();
        var pending = new List<byte>();
        for (int i = 0; i < ids.Length; i++)
        {
            Append(buffer, pending, ids[i], skipSpecialTokens);
        }
        FlushBytes(buffer, pending);
        return Finish(buffer);
    }

    /// <summary>Appends one token, or the byte it names when the file's decoder undoes byte pieces.</summary>
    /// <remarks>
    /// The pieces have to be recognised as tokens: <c>&lt;0xC3&gt;</c> is six characters once
    /// concatenated, and nothing downstream could tell them from text. Any other token flushes
    /// the pending bytes first, so a run decodes as one UTF-8 sequence.
    /// </remarks>
    private void Append(StringBuilder buffer, List<byte> pending, int id, bool skipSpecialTokens)
    {
        if (id < 0 || id >= _tokens.Length || _tokens[id] is not { } token)
        {
            throw new ArgumentOutOfRangeException(
                nameof(id), id, $"The id is outside the vocabulary [0, {_tokens.Length}).");
        }
        if (skipSpecialTokens && _addedIds.Contains(id))
        {
            return;
        }
        if (_decoder is { ByteFallback: true } && BytePieces.TryValue(token, out byte value))
        {
            pending.Add(value);
            return;
        }
        FlushBytes(buffer, pending);
        buffer.Append(_decoder?.MetaspaceReplacement is char meta ? token.Replace(meta, ' ') : token);
    }

    /// <summary>Turns the pending byte run into text, substituting U+FFFD for what is not well-formed UTF-8.</summary>
    private static void FlushBytes(StringBuilder buffer, List<byte> pending)
    {
        if (pending.Count == 0)
        {
            return;
        }
        buffer.Append(Utf8Lossy.GetString([.. pending]));
        pending.Clear();
    }

    /// <summary>Turns the concatenated tokens back into text.</summary>
    private string Finish(StringBuilder buffer)
    {
        if (_decoder is { StripLeadingSpace: true } && buffer.Length > 0 && buffer[0] == ' ')
        {
            buffer.Remove(0, 1);
        }

        if (!_byteLevel)
        {
            // The classic lineage marks a word's end rather than its leading space,
            // so the marker is what a space was.
            return _endOfWord is null
                ? buffer.ToString()
                : buffer.Replace(_endOfWord, " ").ToString().TrimEnd();
        }

        // Every character stands for one byte; anything else never came from Encode.
        byte[] bytes = new byte[buffer.Length];
        int n = 0;
        for (int i = 0; i < buffer.Length; i++)
        {
            if (ByteLevelAlphabet.TryToByte(buffer[i], out byte value))
            {
                bytes[n] = value;
                n++;
            }
        }
        return Utf8Lossy.GetString(bytes, 0, n);
    }
}
