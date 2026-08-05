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
/// <para>Thread-safe after construction.</para>
/// </remarks>
public sealed class WordPieceTokenizer
{
    // Bounded so a pathological input fails instead of hanging the caller.
    private static readonly Regex PreTokenPattern =
        new(@"\w+|[^\w\s]+", RegexOptions.Compiled | RegexOptions.CultureInvariant, RegexDefaults.MatchTimeout);

    private readonly IReadOnlyDictionary<string, int> _vocab;
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
    }

    /// <summary>Creates a tokenizer from a loaded vocabulary.</summary>
    /// <remarks>
    /// Matches <c>tokenizers.Tokenizer.from_file("tokenizer.json")</c> (or a
    /// <c>WordPiece</c> model built from a <c>vocab.txt</c>) followed by
    /// <c>encode</c>. The unknown token, the continuation prefix and the
    /// lowercasing flag come from the file rather than from the caller's memory of
    /// how the model was trained.
    /// </remarks>
    /// <param name="vocabulary">A vocabulary from <see cref="Persistence.VocabTxtLoader"/> or <see cref="Persistence.TokenizerJsonLoader"/>.</param>
    /// <param name="maxCharsPerWord">Words longer than this become a single unknown token.</param>
    public WordPieceTokenizer(WordPieceVocabulary vocabulary, int maxCharsPerWord = 100)
        : this(
            Checked(vocabulary).Vocab,
            vocabulary.UnkToken,
            vocabulary.ContinuationPrefix,
            maxCharsPerWord,
            vocabulary.Lowercase)
    {
    }

    /// <summary>Tokenizes <paramref name="text"/> into sub-word tokens and their ids.</summary>
    public TokenizationResult Encode(string text)
    {
        Guard.NotNull(text);
        if (_lowercase)
        {
            text = text.ToLowerInvariant();
        }

        var tokens = new List<string>();
        var ids = new List<int>();
        foreach (Match m in PreTokenPattern.Matches(text))
        {
            TokenizeWord(m.Value, tokens, ids);
        }
        return new TokenizationResult(tokens, ids);
    }

    /// <summary>Tokenizes <paramref name="text"/> and returns only the token ids.</summary>
    public IReadOnlyList<int> EncodeToIds(string text) => Encode(text).Ids;

    /// <summary>Null-checks a vocabulary before its members are read in a constructor initializer.</summary>
    private static WordPieceVocabulary Checked(WordPieceVocabulary vocabulary)
    {
        Guard.NotNull(vocabulary);
        return vocabulary;
    }

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
