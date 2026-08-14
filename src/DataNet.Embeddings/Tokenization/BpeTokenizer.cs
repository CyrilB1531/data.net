using System.Buffers;
using System.Text;
using DataNet.Internal.Persistence;

namespace DataNet.Embeddings.Tokenization;

/// <summary>
/// Byte-pair-encoding tokenizer, in both the character-level and byte-level
/// variants, reproducing HuggingFace <c>tokenizers</c>' <c>models.BPE</c>.
/// </summary>
/// <remarks>
/// <para>
/// A pre-tokenized piece starts as one symbol per Unicode code point — a
/// surrogate pair counts once, not twice — or, byte-level, one symbol per
/// UTF-8 byte mapped through the byte-level alphabet. The lowest-ranked
/// applicable merge is then applied repeatedly until none applies. Rank is
/// the model: it is the order the pairs were learned in, and it is what a
/// merge table is for.
/// </para>
/// <para>
/// Before any of that, <see cref="Encode"/> looks for a literal occurrence of an
/// added token — HuggingFace's <c>AddedVocabulary</c> stage — and emits it as a
/// single resolved id when found. Only tokens the vocabulary actually declares
/// qualify: text that merely looks like a special token, e.g.
/// <c>&lt;|endoftext|&gt;</c> typed by a user of a vocabulary that never
/// registered it, is tokenized the ordinary way instead.
/// </para>
/// <para>
/// Merge pairs are resolved to pairs of ids once, at construction, so the merge
/// loop compares integers in a rented buffer and allocates nothing. Looking
/// candidates up by string in that loop is the cost this avoids.
/// </para>
/// <para>Thread-safe after construction: nothing here is mutable, and no result is cached.</para>
/// </remarks>
public sealed class BpeTokenizer : ISubwordTokenizer
{
    private const int StackThreshold = 256;

    /// <summary>No such neighbour: the ends of <see cref="Merge"/>'s list, and every symbol merged away.</summary>
    /// <remarks>
    /// One sentinel serves both because a symbol that has been merged away never
    /// needs a successor again. The two are therefore not distinguishable — a
    /// live symbol at the tail of the list reads <see cref="End"/> just as a dead
    /// one does — and they do not need to be: <see cref="Applies"/> asks only
    /// whether a candidate still has a right-hand symbol to merge with, and the
    /// answer is no in both cases. A pair needs a successor, so dropping the
    /// candidate is the correct outcome for the live tail on its own terms, not
    /// merely a tolerable one.
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
    /// The two are not interchangeable and the difference is observable. An added
    /// token that <c>model.vocab</c> does not declare has an id, decodes, and
    /// answers <c>token_to_id</c> — so identity reads <see cref="_vocab"/> — but it
    /// does not make the character it spells covered: HuggingFace substitutes that
    /// character with the unknown token, and measured, <c>aQa</c> is
    /// <c>['a', '[UNK]', 'a']</c> there. Coverage therefore reads this one.
    /// </remarks>
    private readonly Dictionary<string, int> _modelVocab;
    private readonly string[] _tokens;          // id -> token, the inverse of _vocab
    private readonly Dictionary<long, int> _ranks;   // (left << 32 | right) -> rank
    private readonly int[] _merged;             // rank -> the id the pair becomes
    private readonly BpePreTokenizer _split;
    private readonly bool _addPrefixSpace;
    // Two scanners: AddedToken.Normalized decides which one an entry joins, and
    // the two are matched against different strings. See EncodeGap.
    private readonly AddedTokenScanner _rawScanner;
    private readonly AddedTokenScanner _normalizedScanner;
    private readonly NormalizationForm[] _forms;
    private readonly HashSet<int> _addedIds;
    private readonly string? _endOfWord;
    private readonly string? _continuingPrefix;
    private readonly int _unkId;
    private readonly bool _hasUnk;
    private readonly bool _byteLevel;
    private readonly bool _ignoreMerges;
    private readonly bool _fuseUnk;

    /// <summary>Creates a tokenizer from a loaded BPE model.</summary>
    /// <param name="vocabulary">A vocabulary from <see cref="Persistence.BpeFilesLoader"/> or <see cref="Persistence.TokenizerJsonLoader"/>.</param>
    /// <exception cref="ArgumentException">
    /// The declared unknown token is not in the vocabulary; or a merge names a
    /// token the vocabulary does not declare; or a merge's result is not itself
    /// a vocabulary entry; or a byte-level vocabulary declares a continuing
    /// subword prefix, which this tokenizer would apply to its merges and not to
    /// its symbols — see <see cref="EnsureByteLevelDeclaresNoContinuingPrefix"/>;
    /// or <see cref="BpeVocabulary.PreSplit"/> declares a <see cref="SplitBehavior"/>
    /// outside its five defined values — see <see cref="EnsureSplitBehaviorIsDefined"/>.
    /// </exception>
    public BpeTokenizer(BpeVocabulary vocabulary)
    {
        Guard.NotNull(vocabulary);
        EnsureByteLevelDeclaresNoContinuingPrefix(vocabulary);
        EnsureSplitBehaviorIsDefined(vocabulary);
        _addPrefixSpace = vocabulary.AddPrefixSpace;
        _endOfWord = vocabulary.EndOfWordSuffix;
        _continuingPrefix = vocabulary.ContinuingSubwordPrefix;
        _byteLevel = vocabulary.ByteLevel;
        _ignoreMerges = vocabulary.IgnoreMerges;
        _fuseUnk = vocabulary.FuseUnk;

        _forms = [.. vocabulary.NormalizationForms];
        (_vocab, _modelVocab, _tokens) = BuildVocabulary(vocabulary);

        _rawScanner = new AddedTokenScanner([.. vocabulary.AddedTokens.Where(t => !t.Normalized)]);
        _normalizedScanner = new AddedTokenScanner(
            [.. vocabulary.AddedTokens.Where(t => t.Normalized).Select(t => t with { Content = Normalize(t.Content) })]);
        _addedIds = [.. vocabulary.AddedTokens.Where(a => a.Special).Select(a => a.Id)];

        if (vocabulary.UnkToken is { } unk)
        {
            // The model's vocabulary, not the folded one. The reference does
            // not refuse such a file: it loads, answers token_to_id, and
            // encodes text the model covers. It raises "Unk token `<unk>`
            // not found in the vocabulary" from encode, and only on text that
            // needs a substitution. Refusing at construction is earlier than
            // that, deliberately — a failure that depends on which text a
            // caller happens to pass is what loading exists to catch.
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
            // rather than skipped. An earlier comment here said HuggingFace
            // tolerates a merge naming an absent token; measured on both the
            // constructor and the tokenizer.json load path, it raises
            // "Token `x` out of vocabulary". It does not tolerate it.
            if (!_modelVocab.TryGetValue(pair.Left, out int left)
                || !_modelVocab.TryGetValue(pair.Right, out int right))
            {
                throw new ArgumentException(
                    $"The merge at rank {rank} names '{pair.Left}' and '{pair.Right}', "
                    + "and the vocabulary does not contain both.", nameof(vocabulary));
            }
            // The result is a third shape, and the reference does not raise on it
            // — it panics, walking off the end of a slice. A panic is a bug, not
            // behaviour to reproduce, so this message is DataNet's own.
            //
            // The result is the left side plus the right side WITHOUT its
            // continuing prefix. Frozen in bpe_continuing_prefix.json rather
            // than asserted here: ("a", "##b") produces "ab" and ("##b", "##c")
            // produces "##bc" — the left keeps its own prefix and only the
            // right loses one — under models merge_stripped_result and
            // merge_both_prefixed. An end-of-word suffix is part of the string
            // and rides along, ("a", "##b</w>") producing "ab</w>", under
            // merge_suffixed_right.
            //
            // The reference does not merely prefer the stripped form. With the
            // concatenated one in the vocabulary instead, it refuses to build:
            // "Token `##bc` out of vocabulary".
            string merged = pair.Left + StripContinuingPrefix(pair.Right);
            if (!_modelVocab.TryGetValue(merged, out int result))
            {
                throw new ArgumentException(
                    $"The merge at rank {rank} produces '{merged}', "
                    + "which the vocabulary does not contain.", nameof(vocabulary));
            }
            // If a pair is listed twice, the first (lowest) rank is kept rather than
            // the last write winning. Neither tokenizer.json nor merges.txt defines
            // what a duplicate pair should mean, so this is DataNet's own choice,
            // made because rank is supposed to be the order a pair was learned in,
            // and a pair cannot have been learned twice at two different ranks. It
            // is a choice, not a verified fact about HuggingFace's own trainer or
            // loader: tiny_bpe.json's 116 merges are 116 distinct pairs, so no
            // corpus in this branch can tell this apart from "last write wins".
            long key = Key(left, right);
            if (!_ranks.ContainsKey(key))
            {
                _ranks[key] = rank;
            }
            _merged[rank] = result;
        }

        _split = new BpePreTokenizer(vocabulary.PreSplit, vocabulary.PreTokenizerPattern);
    }

    /// <summary>
    /// Refuses a byte-level vocabulary that also declares a continuing subword
    /// prefix, the one pairing whose two halves this class would answer
    /// differently.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <see cref="ByteLevelSymbols"/> decorates nothing, so the prefix is never
    /// applied there, while the constructor's merge loop strips it from every
    /// merge's right side without asking whether the model is byte-level. The
    /// byte-level alphabet contains the characters a prefix is typically spelled
    /// with — <c>#</c> is byte <c>0x23</c> — so on such a model a stripped right
    /// side can land on another entry that exists, and the merge then silently
    /// produces a different id instead of raising.
    /// </para>
    /// <para>
    /// <see cref="Persistence.TokenizerJsonLoader"/> refuses the same pairing in a
    /// file. This one is here as well because <see cref="BpeVocabulary"/> is public
    /// and constructible: a caller can pair the two without going through a loader.
    /// A method rather than an <c>if</c> in the constructor, which sits at 14 of
    /// S3776's limit of 15 — a call carries no cognitive complexity where a branch
    /// costs a point.
    /// </para>
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

    /// <summary>
    /// Refuses a <see cref="BpeVocabulary.PreSplit"/> whose <see cref="SplitBehavior"/>
    /// is not one of the five values the type defines.
    /// </summary>
    /// <remarks>
    /// <see cref="SplitBehavior"/> is a public enum on <see cref="BpeSplitStep"/>, a public
    /// record, so a hand-built <see cref="BpeVocabulary"/> can name a value outside it —
    /// no loader can produce one, since <see cref="Persistence.TokenizerJsonLoader"/> maps
    /// from a fixed set of strings, but nothing stops a caller building
    /// <see cref="BpeVocabulary"/> directly. Left unchecked, that value surfaces only once
    /// <see cref="Encode(string)"/> walks into <see cref="BpePreTokenizer"/>'s merge-loop
    /// switch and throws <see cref="ArgumentOutOfRangeException"/> naming a parameter of
    /// that internal type that no caller of this constructor can see. Refusing it here
    /// instead names the vocabulary, the same way
    /// <see cref="EnsureByteLevelDeclaresNoContinuingPrefix"/> does for the other hand-built
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
    /// A byte-level model re-encodes <paramref name="text"/> to UTF-8; an unpaired
    /// UTF-16 surrogate throws rather than substitutes, since byte-level BPE is
    /// lossless only over well-formed UTF-16. The classic path never encodes to
    /// UTF-8, so it cannot throw this.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// Either a byte-level vocabulary missing one of the 256 alphabet characters
    /// (see <see cref="ByteLevelSymbols"/>), or, once a normalizer is declared, an
    /// unpaired surrogate in a gap -- <see cref="string.Normalize(NormalizationForm)"/>
    /// throws on that before the byte-level re-encoding above gets a chance to.
    /// </exception>
    public TokenizationResult Encode(string text)
    {
        Guard.NotNull(text);
        var tokens = new List<string>();
        var ids = new List<int>();
        var pieces = new List<string>();

        int pos = 0;
        while (pos < text.Length)
        {
            if (!_rawScanner.TryNext(text, pos, out int start, out int end, out var added))
            {
                EncodeGap(text, pos, text.Length, tokens, ids, pieces);
                break;
            }
            if (start > pos)
            {
                EncodeGap(text, pos, start, tokens, ids, pieces);
            }
            tokens.Add(text.Substring(start, end - start));
            ids.Add(added.Id);
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
    private void EncodeGap(string text, int from, int to, List<string> tokens, List<int> ids, List<string> pieces)
    {
        if (_forms.Length == 0 && _normalizedScanner.IsEmpty)
        {
            EncodeSegment(text, from, to, tokens, ids, pieces);
            return;
        }

        string gap = Normalize(text.Substring(from, to - from));
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
    /// <para>
    /// This is where <see cref="BpeVocabulary.AddPrefixSpace"/> applies, and both
    /// halves of where matter. HuggingFace prepends the space inside the
    /// <c>ByteLevel</c> pre-tokenizer, which runs <em>after</em> <c>AddedVocabulary</c>
    /// has cut the input at its added tokens — so the space goes on every segment,
    /// not once on the whole input. And it prepends only when the segment does not
    /// already begin with one: <c>' hello world'</c> and <c>'hello world'</c> both
    /// give <c>['Ġhello', 'Ġworld']</c>, where an unconditional prepend would put a
    /// bare <c>'Ġ'</c> in front of the first.
    /// </para>
    /// <para>
    /// An empty segment produces nothing, which is why the prepend cannot be hoisted
    /// out of this guard: prepending to one would emit a <c>'Ġ'</c> that Python does
    /// not, e.g. for the empty string or for the gap between two adjacent added tokens.
    /// </para>
    /// </remarks>
    private void EncodeSegment(string text, int start, int end, List<string> tokens, List<int> ids, List<string> pieces)
    {
        int length = end - start;
        if (length == 0)
        {
            return;
        }

        string segment = text.Substring(start, length);
        if (_addPrefixSpace && segment[0] != ' ')
        {
            segment = " " + segment;
        }

        pieces.Clear();
        _split.Split(segment, pieces);
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

        // ignore_merges: a piece that is itself a vocabulary entry is emitted whole.
        // Llama-3 declares this, and without it that family tokenizes differently
        // while looking entirely plausible.
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

        // One symbol per character is the upper bound for the classic path; a
        // byte-level piece is sized by its UTF-8 byte count instead, because one
        // character can become up to four symbols. The pool is rented before the
        // span is built, rather than inside the conditional expression, so the
        // rent is a statement of its own rather than an assignment buried in an
        // expression.
        int capacity = _byteLevel ? JsonArtifact.Utf8NoBom.GetByteCount(piece) : piece.Length;
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

    /// <summary>
    /// Fills <paramref name="symbols"/> with one id per Unicode code point,
    /// substituting the unknown-token id for a code point the vocabulary does not
    /// cover — or, when no unknown token is declared, dropping it. Returns the
    /// number of symbols written, which can be less than <c>piece.Length</c> both
    /// because characters can be dropped and because a surrogate pair is one code
    /// point, not two.
    /// </summary>
    /// <remarks>
    /// Splitting by <see cref="string"/> index would count a two-<see cref="char"/>
    /// surrogate pair as two symbols where HuggingFace, iterating a Python
    /// <c>str</c>'s code points, counts one — an astral character such as an emoji
    /// would come back as two unknown tokens instead of one.
    /// </remarks>
    private int InitialSymbols(string piece, Span<int> symbols)
    {
        int count = 0;
        int i = 0;
        // Whether the previous code point was SUBSTITUTED, which is not the same
        // question as whether the previous symbol's id is _unkId: an unknown
        // token that is itself a covered character produces that id without
        // having been substituted, and HuggingFace does not fuse across it.
        // Measured with unk_token "q" over a vocabulary containing "q":
        // "qZ" gives two tokens, "ZZ" gives one.
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
            else if (_hasUnk)
            {
                // fuse_unk: a run of uncovered code points is one unknown token,
                // not one each. This happens before Merge runs, which is why a
                // fused symbol can itself take part in a merge — as HuggingFace's
                // does.
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

    /// <summary>
    /// The vocabulary key for one code point of a piece: the characters
    /// themselves, plus whatever decoration its position calls for.
    /// </summary>
    /// <param name="piece">The pre-tokenized piece being walked.</param>
    /// <param name="at">The index of the code point's first <see cref="char"/>.</param>
    /// <param name="width">Its width in <see cref="char"/>s — two for a surrogate pair.</param>
    /// <param name="first">Whether it opens the piece.</param>
    /// <param name="last">Whether it ends the piece.</param>
    /// <remarks>
    /// The two decorations compose, prefix then characters then suffix, and a
    /// symbol that both continues a piece and ends it carries both — measured
    /// against <c>tokenizers</c> 0.23.1, where <c>"ab"</c> under both settings
    /// gives <c>['a', '##b&lt;/w&gt;']</c>. The prefix belongs to the piece and
    /// not to the text, which costs nothing here: this runs once per code point
    /// and only reads <paramref name="first"/>, and the caller computing it —
    /// <see cref="InitialSymbols"/>, which is the once-per-piece one — already
    /// knows where the piece starts.
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
    /// <para>
    /// One byte, one symbol: a four-byte emoji enters the merge loop as four
    /// symbols. That is where the round-trip guarantee comes from — every byte of
    /// the input is represented, so decoding can put them back.
    /// </para>
    /// <para>
    /// Unlike <see cref="InitialSymbols"/>, this never appends <c>_endOfWord</c>:
    /// <see cref="BpeVocabulary.EndOfWordSuffix"/> and
    /// <see cref="BpeVocabulary.ByteLevel"/> are independent properties, so a
    /// vocabulary can declare both, and the suffix is then silently ignored on this
    /// path rather than applied. Nothing here measured what the reference does with
    /// that pairing, so it is a documented gap rather than a refusal.
    /// </para>
    /// <para>
    /// <see cref="BpeVocabulary.ContinuingSubwordPrefix"/> was the same gap and is
    /// not one any more: it is refused beside <see cref="BpeVocabulary.ByteLevel"/>,
    /// by <see cref="EnsureByteLevelDeclaresNoContinuingPrefix"/> here and by
    /// <see cref="Persistence.TokenizerJsonLoader"/> for a file. The prefix carries a
    /// cost the suffix does not: <see cref="StripContinuingPrefix(string)"/>, in the
    /// constructor's merge loop, strips it from a merge's right side without checking
    /// <c>_byteLevel</c>, so the merge results would be computed as if the prefix
    /// applied while the symbols here are built as if it did not. That disagreement is
    /// silent — the byte-level alphabet contains the characters a prefix is typically
    /// spelled with, so a stripped side can land on another entry that exists — which
    /// is why the pairing is refused rather than documented.
    /// </para>
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// A byte-level model's vocabulary is expected to contain all 256 byte-level
    /// alphabet characters as base tokens -- that is what makes it byte-level.
    /// A missing entry means the vocabulary is not what <see cref="BpeVocabulary.ByteLevel"/>
    /// claims it is, which is a broken model, not ordinary uncovered input the way
    /// an unmapped code point is on the classic path.
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
    /// <para>
    /// Rescanning every adjacent pair after every merge, and shifting the array
    /// down to close the gap, costs the square of the symbol count: 3.80x, 3.91x
    /// and 4.17x per doubling of a token with no split point in it, against the
    /// 2.02x, 2.08x and 2.00x this costs instead. Those figures are one machine's
    /// — an Intel Core i7-4770S (Haswell), Ubuntu 24.04.4, .NET SDK 10.0.110 —
    /// and <c>BpeScalingBenchmarks</c> in <c>bench/</c> is what re-measures them
    /// anywhere else. So the symbols are threaded on a doubly-linked list — a merge
    /// unlinks one node and nothing moves — and the candidate merges are kept in
    /// a priority queue, so each round takes the best one instead of looking for
    /// it. Only the two pairs a merge actually creates are new candidates.
    /// </para>
    /// <para>
    /// Positions are indices into <paramref name="symbols"/> and never change,
    /// which is what lets the queue express the whole ordering rule: an entry is
    /// the rank in the high 32 bits and the left position in the low 32, so
    /// comparing the two packed <see cref="long"/>s <em>is</em> "lowest rank
    /// first, leftmost occurrence on a tie". Both halves are non-negative, so the
    /// natural ordering of the packed value is the lexicographic ordering of the
    /// pair.
    /// </para>
    /// <para>
    /// Entries are never removed when they go stale — a merge would otherwise
    /// have to find the queue entries mentioning the two symbols it consumed.
    /// They are validated when they come off the queue instead, by
    /// <see cref="Applies"/>, and dropped in silence when they no longer describe
    /// an adjacent pair of exactly their rank. The queue can therefore hold
    /// entries no longer worth acting on, but never misses one that is: every
    /// adjacency the merge table knows about is offered when it is created, and
    /// an adjacency changes only when one of its two symbols is merged, which
    /// either destroys it or produces the merge that offers it again.
    /// </para>
    /// </remarks>
    private int Merge(Span<int> symbols, int count)
    {
        if (count < 2)
        {
            return count;
        }

        // Both buffers are always rented, never stackalloc'd below a threshold the
        // way EncodePiece sizes symbols itself. That is a deliberate difference:
        // the threshold would put two conditional rentals and two conditional
        // returns around the loop that carries the algorithm, and the corpus
        // benchmark -- 5000 documents of ordinary text, which is where short
        // pieces dominate -- measures no cost to renting unconditionally. Each is
        // still rented in a statement of its own and returned in a finally, so
        // every path out of here gives both back. The links are one array holding
        // two spans rather than two rentals, so there is one fewer to give back.
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
    /// <para>
    /// Matches <c>tokenizers.Tokenizer.decode(ids, skip_special_tokens=…)</c>.
    /// For a byte-level model this is exact: every byte of the input was mapped
    /// to a symbol, so every byte comes back.
    /// </para>
    /// <para>
    /// The default is deliberately the opposite of HuggingFace's. Python skips
    /// special tokens unless told otherwise; here the round trip is exact unless
    /// asked otherwise, because a <c>Decode</c> that silently drops tokens makes
    /// <c>Decode(Encode(x)) == x</c> false in exactly the case a caller would
    /// write to check it.
    /// </para>
    /// <para>
    /// For a byte-level model, a byte sequence that is not well-formed UTF-8
    /// becomes U+FFFD, as in the reference, which is what makes decoding one id
    /// at a time possible. A caller who needs to know can test the result for U+FFFD.
    /// </para>
    /// </remarks>
    /// <param name="ids">Token ids, e.g. from <see cref="Encode"/>.</param>
    /// <param name="skipSpecialTokens">
    /// Drop added tokens marked <c>special</c> instead of rendering them, the ones
    /// <see cref="AddedToken.Special"/> carries from the file's <c>added_tokens</c>
    /// entry. An ordinary added token -- <c>Special</c> unset -- is rendered
    /// either way, matching Python's <c>skip_special_tokens</c>.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">An id is outside the vocabulary.</exception>
    public string Decode(IReadOnlyList<int> ids, bool skipSpecialTokens = false)
    {
        Guard.NotNull(ids);
        var buffer = new StringBuilder();
        for (int i = 0; i < ids.Count; i++)
        {
            Append(buffer, ids[i], skipSpecialTokens);
        }
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
        for (int i = 0; i < ids.Length; i++)
        {
            Append(buffer, ids[i], skipSpecialTokens);
        }
        return Finish(buffer);
    }

    private void Append(StringBuilder buffer, int id, bool skipSpecialTokens)
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
        buffer.Append(token);
    }

    /// <summary>Turns the concatenated tokens back into text.</summary>
    private string Finish(StringBuilder buffer)
    {
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
