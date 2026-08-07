using System.Buffers;

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

    private readonly Dictionary<string, int> _vocab;
    private readonly string[] _tokens;          // id -> token, the inverse of _vocab
    private readonly Dictionary<long, int> _ranks;   // (left << 32 | right) -> rank
    private readonly int[] _merged;             // rank -> the id the pair becomes
    private readonly BpePreTokenizer _split;
    private readonly BpeVocabulary _vocabulary;
    private readonly (string Token, int Id)[] _addedTokens; // any order -- NextAddedToken resolves leftmost, then longest, itself
    private readonly string? _endOfWord;
    private readonly int _unkId;
    private readonly bool _hasUnk;

    /// <summary>Creates a tokenizer from a loaded BPE model.</summary>
    /// <param name="vocabulary">A vocabulary from <c>BpeFilesLoader</c> or <see cref="Persistence.TokenizerJsonLoader"/>.</param>
    /// <exception cref="ArgumentException">The declared unknown token is not in the vocabulary.</exception>
    public BpeTokenizer(BpeVocabulary vocabulary)
    {
        Guard.NotNull(vocabulary);
        _vocabulary = vocabulary;
        _endOfWord = vocabulary.EndOfWordSuffix;

        _vocab = new Dictionary<string, int>(vocabulary.Vocab.Count, StringComparer.Ordinal);
        int maxId = -1;
        foreach (KeyValuePair<string, int> entry in vocabulary.Vocab)
        {
            _vocab[entry.Key] = entry.Value;
            maxId = Math.Max(maxId, entry.Value);
        }
        foreach (KeyValuePair<string, int> entry in vocabulary.AddedTokens)
        {
            _vocab[entry.Key] = entry.Value;
            maxId = Math.Max(maxId, entry.Value);
        }

        _tokens = new string[maxId + 1];
        foreach (KeyValuePair<string, int> entry in _vocab)
        {
            _tokens[entry.Value] = entry.Key;
        }

        // An empty added token would never advance Encode's scan position --
        // IndexOf("", pos) always returns pos -- hanging the loop. The loader this
        // vocabulary is meant to come from bounds a token's *upper* length but never
        // rejects an empty one (TokenizerJsonLoader.cs), so this is not a case that
        // can be assumed away; it is filtered here instead. The order of the array
        // itself does not matter: NextAddedToken picks the leftmost match, and the
        // longest on a tie, regardless of how these are enumerated.
        _addedTokens = [.. vocabulary.AddedTokens
            .Where(entry => entry.Key.Length > 0)
            .Select(entry => (entry.Key, entry.Value))];

        if (vocabulary.UnkToken is { } unk)
        {
            if (!_vocab.TryGetValue(unk, out _unkId))
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
            // A pair naming a token the vocabulary does not contain cannot apply.
            // HuggingFace tolerates it, so refusing the file would be a divergence.
            // BpeVocabulary.SkippedMerges is where the count is reported.
            if (!_vocab.TryGetValue(pair.Left, out int left)
                || !_vocab.TryGetValue(pair.Right, out int right)
                || !_vocab.TryGetValue(pair.Left + pair.Right, out int result))
            {
                _merged[rank] = -1;
                continue;
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

        _split = new BpePreTokenizer(vocabulary.PreTokenizerPattern);
    }

    /// <summary>Tokenizes <paramref name="text"/> into sub-word tokens and their ids.</summary>
    /// <remarks>Matches <c>tokenizers.Tokenizer.encode(text)</c>, without the post-processor.</remarks>
    public TokenizationResult Encode(string text)
    {
        Guard.NotNull(text);
        var tokens = new List<string>();
        var ids = new List<int>();
        var pieces = new List<string>();
        string effective = _vocabulary.AddPrefixSpace ? " " + text : text;

        int pos = 0;
        while (pos < effective.Length)
        {
            (int at, string token, int id) = NextAddedToken(effective, pos);
            if (at < 0)
            {
                EncodeSegment(effective, pos, effective.Length, tokens, ids, pieces);
                break;
            }
            if (at > pos)
            {
                EncodeSegment(effective, pos, at, tokens, ids, pieces);
            }
            tokens.Add(token);
            ids.Add(id);
            pos = at + token.Length;
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

    /// <summary>The earliest added token at or after <paramref name="from"/>; the longest one, on a tie. <c>At</c> is -1 when none remains.</summary>
    private (int At, string Token, int Id) NextAddedToken(string text, int from)
    {
        int bestAt = -1;
        string bestToken = string.Empty;
        int bestId = 0;
        foreach ((string token, int id) in _addedTokens)
        {
            // Once a candidate is found, only a match starting at or before it can
            // still win (leftmost beats longer), so later tokens only need a window
            // reaching bestAt plus their own length -- just enough to still find a
            // match that starts exactly at bestAt. Llama-3 alone declares 256 added
            // tokens; without this bound, every one of them would rescan to the end
            // of whatever text remains, on every match found.
            int windowEnd = bestAt < 0 ? text.Length : Math.Min(text.Length, bestAt + token.Length);
            int at = text.IndexOf(token, from, windowEnd - from, StringComparison.Ordinal);
            if (at < 0)
            {
                continue;
            }
            if (bestAt < 0 || at < bestAt || (at == bestAt && token.Length > bestToken.Length))
            {
                bestAt = at;
                bestToken = token;
                bestId = id;
            }
        }
        return (bestAt, bestToken, bestId);
    }

    /// <summary>Splits and merges the plain-text slice <c>text[start..end]</c>, which contains no added token.</summary>
    private void EncodeSegment(string text, int start, int end, List<string> tokens, List<int> ids, List<string> pieces)
    {
        pieces.Clear();
        _split.Split(text.Substring(start, end - start), pieces);
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

        // One symbol per character is the upper bound for the classic path; the
        // byte-level path rents against its byte count in Task 8. The pool is
        // rented before the span is built, rather than inside the conditional
        // expression, so the rent is a statement of its own rather than an
        // assignment buried in an expression.
        bool small = piece.Length <= StackThreshold;
        int[]? rented = small ? null : ArrayPool<int>.Shared.Rent(piece.Length);
        Span<int> symbols = small ? stackalloc int[piece.Length] : rented!.AsSpan(0, piece.Length);
        try
        {
            int count = InitialSymbols(piece, symbols);
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
        while (i < piece.Length)
        {
            int width = char.IsHighSurrogate(piece[i])
                && i + 1 < piece.Length
                && char.IsLowSurrogate(piece[i + 1])
                ? 2
                : 1;
            bool last = i + width == piece.Length;
            string symbol = last && _endOfWord is not null
                ? piece.Substring(i, width) + _endOfWord
                : piece.Substring(i, width);
            if (_vocab.TryGetValue(symbol, out int id))
            {
                symbols[count++] = id;
            }
            else if (_hasUnk)
            {
                symbols[count++] = _unkId;
            }
            i += width;
        }
        return count;
    }

    /// <summary>Applies the lowest-ranked applicable merge until none applies. Returns the new symbol count.</summary>
    private int Merge(Span<int> symbols, int count)
    {
        while (count > 1)
        {
            int bestRank = int.MaxValue;
            int bestAt = -1;
            for (int i = 0; i + 1 < count; i++)
            {
                if (_ranks.TryGetValue(Key(symbols[i], symbols[i + 1]), out int rank) && rank < bestRank)
                {
                    bestRank = rank;
                    bestAt = i;
                }
            }
            if (bestAt < 0)
            {
                break;
            }
            symbols[bestAt] = _merged[bestRank];
            for (int i = bestAt + 1; i + 1 < count; i++)
            {
                symbols[i] = symbols[i + 1];
            }
            count--;
        }
        return count;
    }
}
