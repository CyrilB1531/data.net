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

// CA1308 (normalize to uppercase): this lowercasing is the `lowercase`
// constructor option, mirroring HuggingFace's do_lower_case — true for an
// uncased checkpoint, false for a cased one such as bert-base-cased.
// ToUpperInvariant would match no vocabulary entry, producing wrong ids rather
// than differently-cased tokens.
#pragma warning disable CA1308

/// <summary>
/// WordPiece tokenizer (used by BERT-family models), reproducing the greedy
/// longest-match algorithm of HuggingFace <c>tokenizers</c> WordPiece.
/// </summary>
/// <remarks>
/// <para>
/// Getting this exactly right matters: if the tokenization does not match the one
/// the model was trained with, the embeddings are wrong. Pre-tokenization splits
/// on whitespace and isolates punctuation (HuggingFace <c>Whitespace</c> pre-tokenizer,
/// regex <c>\w+|[^\w\s]+</c>); each resulting word is then greedily matched against
/// the vocabulary, with <c>##</c>-prefixed continuation pieces.
/// </para>
/// <para>
/// Ahead of all of that runs the <c>added_tokens</c> scan: the entries
/// <see cref="WordPieceVocabulary.AddedTokens"/> carries are matched as literal
/// text, and only what is left between them reaches the pre-tokenizer. See
/// <see cref="Encode"/> for the order the two normalization rules impose.
/// </para>
/// <para>Thread-safe after construction.</para>
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

        // Two scanners because the two halves of the table are matched against two
        // different strings; AddedToken.Normalized is what puts an entry in one or
        // the other, and Special has nothing to do with it. See Encode.
        _addedTokens = [.. addedTokens];
        _rawScanner = new AddedTokenScanner([.. addedTokens.Where(t => !t.Normalized)]);
        _normalizedScanner = new AddedTokenScanner(
            [.. addedTokens.Where(t => t.Normalized).Select(t => lowercase ? t with { Content = t.Content.ToLowerInvariant() } : t)]);
    }

    /// <summary>Tokenizes <paramref name="text"/> into sub-word tokens and their ids.</summary>
    /// <remarks>
    /// <para>
    /// The <c>added_tokens</c> table is matched first, and the two halves of it are
    /// not matched against the same string. <see cref="AddedToken.Normalized"/> is
    /// what splits them: an entry that is <em>not</em> normalized is matched against
    /// the <em>raw</em> text and emits the raw slice, while a normalized one has its
    /// own content normalized and is matched against the normalized text, emitting
    /// that. Measured against <c>tokenizers</c> 0.23.1 with a <c>Lowercase</c>
    /// normalizer and <c>[CLS]</c> added: not normalized, it matches
    /// <c>'a [CLS] b'</c> and emits <c>[CLS]</c> while <c>'a [cls] b'</c> does not
    /// match at all and falls through to the model; normalized, both spellings match
    /// and both emit <c>[cls]</c>. "Added tokens are matched before normalization" is
    /// the natural summary and the wrong one.
    /// </para>
    /// <para>
    /// <see cref="AddedToken.Special"/> decides none of this, though it looks as if
    /// it does on every file <c>add_special_tokens</c> wrote, which sets
    /// <c>normalized = !special</c>. A file may carry either combination, and a
    /// special-but-normalized entry runs in the normalized pass like any other.
    /// </para>
    /// <para>
    /// The raw half is matched in an outer pass, as HuggingFace's
    /// <c>AddedVocabulary</c> does — it splits on the raw trie first and runs the
    /// normalized trie over what is left — so what a raw entry consumes is never
    /// offered to the normalized scanner, even where a normalized entry would have
    /// matched further left. See
    /// <c>docs/decisions/0022-added-token-matching-flags.md</c>.
    /// </para>
    /// </remarks>
    /// <param name="text">The text to tokenize.</param>
    public TokenizationResult Encode(string text)
    {
        Guard.NotNull(text);
        var tokens = new List<string>();
        var ids = new List<int>();

        // Normalized once, and indexed with positions found in the raw text. That is
        // sound only because ToLowerInvariant maps char to char and so preserves
        // length — an assumption about the scripts in scope, not a fact of Unicode,
        // and the reason this is ToLowerInvariant rather than ToLower: a
        // culture-sensitive mapping is under no such obligation.
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
        // A scan rather than a second dictionary: added_tokens tables are tiny —
        // Llama-3's 256 is the largest in sight — and a copy of a thirty-thousand
        // entry vocabulary to hold them would not be.
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

    /// <summary>
    /// Encodes <c>normalized[from..to]</c> — text no raw-matched added token claimed
    /// — scanning it for normalized added tokens and handing what is left to the
    /// model.
    /// </summary>
    /// <remarks>
    /// The gap is scanned as a string of its own, so a strip cannot reach across the
    /// entry that closed it and a <see cref="AddedToken.SingleWord"/> entry sees the
    /// gap's edges as word boundaries. That is what HuggingFace's
    /// <c>AddedVocabulary</c> does, which splits on the raw trie first and then runs
    /// the normalized trie over each resulting slice — read from that type's
    /// structure rather than measured, since no committed case puts a word
    /// character on a gap edge for a corpus to replay.
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
