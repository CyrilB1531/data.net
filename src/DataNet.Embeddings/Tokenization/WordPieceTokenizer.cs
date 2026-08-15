using System.Text.RegularExpressions;

namespace DataNet.Embeddings.Tokenization;

/// <summary>The result of tokenizing a piece of text: the sub-word tokens and their vocabulary ids.</summary>
public sealed record TokenizationResult(IReadOnlyList<string> Tokens, IReadOnlyList<int> Ids)
{
    /// <summary>Compares the tokens and ids element by element.</summary>
    /// <remarks>
    /// The generated equality would compare <see cref="Tokens"/> and <see cref="Ids"/>
    /// by reference, so two results holding the same tokens would be unequal — in
    /// the one place a caller has every reason to compare: asserting an encoding
    /// against the result written out by hand.
    /// </remarks>
    public bool Equals(TokenizationResult? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        if (other is null || Tokens.Count != other.Tokens.Count || Ids.Count != other.Ids.Count)
        {
            return false;
        }
        for (int i = 0; i < Tokens.Count; i++)
        {
            if (!string.Equals(Tokens[i], other.Tokens[i], StringComparison.Ordinal))
            {
                return false;
            }
        }
        for (int i = 0; i < Ids.Count; i++)
        {
            if (Ids[i] != other.Ids[i])
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>Hashes the lengths only, which is O(1) and still consistent with equality.</summary>
    /// <remarks>
    /// Equal results necessarily agree on both counts; unequal ones are allowed to
    /// share a hash. Hashing every token would make the cheap operation the
    /// expensive one on a long encoding.
    /// </remarks>
    public override int GetHashCode()
    {
        unchecked
        {
            return (17 * 31 + Tokens.Count) * 31 + Ids.Count;
        }
    }
}

// CA1308: this is the `lowercase` option, HuggingFace's do_lower_case. ToUpperInvariant
// would match no vocabulary entry, giving wrong ids rather than differently-cased tokens.
#pragma warning disable CA1308

/// <summary>WordPiece tokenizer (BERT family), reproducing HuggingFace <c>tokenizers</c>' greedy longest-match algorithm.</summary>
/// <remarks>
/// Pre-tokenization splits on whitespace and isolates punctuation (HuggingFace
/// <c>Whitespace</c> pre-tokenizer, regex <c>\w+|[^\w\s]+</c>); each resulting word
/// is then greedily matched against the vocabulary, with <c>##</c>-prefixed
/// continuation pieces -- <c>docs/equivalence.md</c>'s <c>WordPiece(vocab)</c> row.
/// The <c>added_tokens</c> scan runs ahead of all that; see <see cref="Encode"/>.
/// Thread-safe after construction.
/// </remarks>
public sealed class WordPieceTokenizer : ISubwordTokenizer
{
    // Bounded so a pathological input fails instead of hanging the caller.
    private static readonly Regex PreTokenPattern =
        new(@"\w+|[^\w\s]+", RegexOptions.Compiled | RegexOptions.CultureInvariant, RegexDefaults.MatchTimeout);

    private readonly IReadOnlyDictionary<string, int> _vocab;
    private readonly AddedToken[] _addedTokens;
    private readonly AddedTokenScanner _rawScanner;
    private readonly AddedTokenScanner _normalizedScanner;
    private readonly string _unkToken;
    private readonly int _unkId;
    private readonly string _continuationPrefix;
    private readonly int _maxCharsPerWord;
    private readonly bool _lowercase;

    /// <summary>Creates a tokenizer from an in-memory vocabulary.</summary>
    /// <param name="vocab">Map from token string to id.</param>
    /// <param name="unkToken">The unknown-token string (must be present in <paramref name="vocab"/>).</param>
    /// <param name="continuationPrefix">Prefix marking non-initial word pieces (default <c>##</c>).</param>
    /// <param name="maxCharsPerWord">Words longer than this become a single unknown token.</param>
    /// <param name="lowercase">Lowercase the text before tokenizing.</param>
    public WordPieceTokenizer(
        IReadOnlyDictionary<string, int> vocab,
        string unkToken = "[UNK]",
        string continuationPrefix = "##",
        int maxCharsPerWord = 100,
        bool lowercase = false)
        : this(vocab, unkToken, continuationPrefix, maxCharsPerWord, lowercase, [])
    {
    }

    /// <summary>Creates a tokenizer from a loaded vocabulary.</summary>
    /// <remarks>
    /// Matches <c>tokenizers.Tokenizer.from_file("tokenizer.json")</c> (or a
    /// <c>WordPiece</c> model built from a <c>vocab.txt</c>) followed by
    /// <c>encode</c>. The unknown token, the continuation prefix, the lowercasing
    /// flag and the <c>added_tokens</c> table come from the file rather than from
    /// the caller's memory of how the model was trained.
    /// </remarks>
    /// <param name="vocabulary">A vocabulary from <see cref="Persistence.VocabTxtLoader"/> or <see cref="Persistence.TokenizerJsonLoader"/>.</param>
    /// <param name="maxCharsPerWord">Words longer than this become a single unknown token.</param>
    public WordPieceTokenizer(WordPieceVocabulary vocabulary, int maxCharsPerWord = 100)
        : this(
            Checked(vocabulary).Vocab,
            vocabulary.UnkToken,
            vocabulary.ContinuationPrefix,
            maxCharsPerWord,
            vocabulary.Lowercase,
            vocabulary.AddedTokens)
    {
    }

    /// <summary>The one constructor; the two public ones differ only in where the added tokens come from.</summary>
    private WordPieceTokenizer(
        IReadOnlyDictionary<string, int> vocab,
        string unkToken,
        string continuationPrefix,
        int maxCharsPerWord,
        bool lowercase,
        IReadOnlyList<AddedToken> addedTokens)
    {
        Guard.NotNull(vocab);
        if (!vocab.TryGetValue(unkToken, out int unkId))
        {
            throw new ArgumentException($"The unknown token '{unkToken}' is not in the vocabulary.", nameof(unkToken));
        }

        _vocab = vocab;
        _unkToken = unkToken;
        _unkId = unkId;
        _continuationPrefix = continuationPrefix;
        _maxCharsPerWord = maxCharsPerWord;
        _lowercase = lowercase;

        // Two scanners: the two halves of added_tokens are matched against different
        // strings, split by AddedToken.Normalized (Special plays no part). See Encode.
        _addedTokens = [.. addedTokens];
        _rawScanner = new AddedTokenScanner([.. addedTokens.Where(t => !t.Normalized)]);
        _normalizedScanner = new AddedTokenScanner(
            [.. addedTokens.Where(t => t.Normalized).Select(t => lowercase ? t with { Content = t.Content.ToLowerInvariant() } : t)]);
    }

    /// <summary>Tokenizes <paramref name="text"/> into sub-word tokens and their ids.</summary>
    /// <remarks>
    /// The <c>added_tokens</c> table is matched first, by
    /// <see cref="AddedToken.Normalized"/> rather than <see cref="AddedToken.Special"/> --
    /// <c>docs/equivalence.md</c>'s <c>WordPiece(vocab)</c> row covers which pass an
    /// entry runs in and which string it is matched against. "Added tokens are matched
    /// before normalization" is the natural summary and the wrong one -- measured
    /// (<c>wordpiece_added_tokens.json</c>), raw stays case-sensitive, normalized does not.
    /// </remarks>
    /// <param name="text">The text to tokenize.</param>
    public TokenizationResult Encode(string text)
    {
        Guard.NotNull(text);
        var tokens = new List<string>();
        var ids = new List<int>();

        // Indexed with raw-text positions, sound only because ToLowerInvariant preserves
        // length -- ToLower and BpeTokenizer's four forms do not, hence its per-gap pass.
        string normalized = _lowercase ? text.ToLowerInvariant() : text;

        int pos = 0;
        while (pos < text.Length)
        {
            if (!_rawScanner.TryNext(text, pos, out int start, out int end, out var raw))
            {
                EncodeGap(normalized, pos, normalized.Length, tokens, ids);
                break;
            }
            if (start > pos)
            {
                EncodeGap(normalized, pos, start, tokens, ids);
            }
            // The raw slice, not the entry's content: a match that stripped
            // whitespace consumed that whitespace into the token it emits.
            tokens.Add(text.Substring(start, end - start));
            ids.Add(raw.Id);
            pos = end;
        }
        return new TokenizationResult(tokens, ids);
    }

    /// <summary>Tokenizes <paramref name="text"/> and returns only the token ids.</summary>
    public IReadOnlyList<int> EncodeToIds(string text) => Encode(text).Ids;

    /// <summary>Looks up a literal vocabulary entry, added tokens included.</summary>
    /// <remarks>
    /// Matches <c>tokenizers.Tokenizer.token_to_id(token)</c>. The lookup is exact
    /// and case-sensitive — <c>[CLS]</c> is a vocabulary entry, not text, so the
    /// lowercasing flag deliberately does not apply, and an added token is looked up
    /// under the content the file wrote even when what <see cref="Encode"/> matches
    /// is its normalized form. That is what <c>tokenizers</c> own <c>get_vocab()</c>
    /// reports.
    /// </remarks>
    /// <param name="token">The token string.</param>
    /// <param name="id">Receives the id when the token is present.</param>
    public bool TryGetId(string token, out int id)
    {
        Guard.NotNull(token);
        if (_vocab.TryGetValue(token, out id))
        {
            return true;
        }
        // A scan, not a second dictionary: added_tokens tables are tiny (Llama-3's
        // 256 is the largest in sight), so copying the vocabulary to hold them would not be.
        AddedToken? added = Array.Find(_addedTokens, t => string.Equals(t.Content, token, StringComparison.Ordinal));
        if (added is null)
        {
            id = 0;
            return false;
        }
        id = added.Id;
        return true;
    }

    /// <summary>Null-checks a vocabulary before its members are read in a constructor initializer.</summary>
    private static WordPieceVocabulary Checked(WordPieceVocabulary vocabulary)
    {
        Guard.NotNull(vocabulary);
        return vocabulary;
    }

    /// <summary>Encodes <c>normalized[from..to]</c> -- text no raw-matched added token claimed -- scanning it for normalized added tokens and handing what is left to the model.</summary>
    /// <remarks>
    /// The gap is scanned as its own string, matching HuggingFace's
    /// <c>AddedVocabulary</c> (read from its structure, not measured).
    /// <c>wordpiece_added_tokens.json</c>'s
    /// <c>the_raw_pass_wins_over_a_normalized_match_further_left</c> cuts a gap at a
    /// word character; no committed case puts a <see cref="AddedToken.SingleWord"/>
    /// or stripping entry at a gap edge.
    /// </remarks>
    private void EncodeGap(string normalized, int from, int to, List<string> tokens, List<int> ids)
    {
        if (_normalizedScanner.IsEmpty)
        {
            EncodeSegment(normalized, from, to, tokens, ids);
            return;
        }

        string gap = Slice(normalized, from, to);
        int pos = 0;
        while (pos < gap.Length)
        {
            if (!_normalizedScanner.TryNext(gap, pos, out int start, out int end, out var added))
            {
                EncodeSegment(gap, pos, gap.Length, tokens, ids);
                break;
            }
            if (start > pos)
            {
                EncodeSegment(gap, pos, start, tokens, ids);
            }
            tokens.Add(gap.Substring(start, end - start));
            ids.Add(added.Id);
            pos = end;
        }
    }

    /// <summary>Pre-tokenizes and models <c>normalized[start..end]</c>, which holds no added token.</summary>
    /// <remarks>
    /// The slice is already normalized: <see cref="Encode"/> lowercases the whole
    /// input once, before the scan, because the normalized half of the table is
    /// matched against that same string.
    /// </remarks>
    private void EncodeSegment(string normalized, int start, int end, List<string> tokens, List<int> ids)
    {
        if (end <= start)
        {
            return;
        }

        foreach (Match m in PreTokenPattern.Matches(Slice(normalized, start, end)))
        {
            TokenizeWord(m.Value, tokens, ids);
        }
    }

    /// <summary>The slice, or the string itself when the slice is the whole of it.</summary>
    /// <remarks>
    /// The identity case is the common one — no added token to cut anything out —
    /// and copying the whole input there would be a per-call allocation this
    /// tokenizer did not make before it gained a scan.
    /// </remarks>
    private static string Slice(string text, int start, int end) =>
        start == 0 && end == text.Length ? text : text.Substring(start, end - start);

    private void TokenizeWord(string word, List<string> tokens, List<int> ids)
    {
        if (word.Length > _maxCharsPerWord)
        {
            tokens.Add(_unkToken);
            ids.Add(_unkId);
            return;
        }

        var pieces = new List<string>();
        var pieceIds = new List<int>();
        int start = 0;
        bool bad = false;

        while (start < word.Length)
        {
            int end = word.Length;
            string? found = null;
            int foundId = 0;
            while (start < end)
            {
                string sub = word[start..end];
                if (start > 0)
                {
                    sub = _continuationPrefix + sub;
                }
                if (_vocab.TryGetValue(sub, out int id))
                {
                    found = sub;
                    foundId = id;
                    break;
                }
                end--;
            }

            if (found is null)
            {
                bad = true;
                break;
            }

            pieces.Add(found);
            pieceIds.Add(foundId);
            start = end;
        }

        if (bad)
        {
            tokens.Add(_unkToken);
            ids.Add(_unkId);
        }
        else
        {
            tokens.AddRange(pieces);
            ids.AddRange(pieceIds);
        }
    }
}
