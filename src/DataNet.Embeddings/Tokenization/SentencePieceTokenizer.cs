namespace DataNet.Embeddings.Tokenization;

// SonarLint S3776: cognitive complexity: a faithful implementation of a published rule-engine; decomposing it would break the 1:1 mapping with the reference that makes divergences auditable.
#pragma warning disable S3776

/// <summary>A vocabulary piece: its string, log-probability score and id.</summary>
public readonly record struct SentencePiece(string Piece, double Score, int Id);

/// <summary>
/// SentencePiece <em>unigram</em> tokenizer: segments text to maximize the sum of
/// piece log-probabilities, via Viterbi over the vocabulary.
/// </summary>
/// <remarks>
/// <para>
/// Reproduces the encoding of a trained SentencePiece unigram model (as used by
/// ALBERT, T5, camemBERT, XLM-R…). Preprocessing follows the model's
/// <c>identity</c> normalizer with <c>add_dummy_prefix</c>: whitespace is
/// collapsed, spaces become the meta symbol <c>▁</c> (U+2581), and a leading
/// <c>▁</c> is prepended. Getting the tokenization exactly right matters — a
/// mismatch makes the downstream model's embeddings wrong.
/// </para>
/// <para>Thread-safe after construction.</para>
/// </remarks>
public sealed class SentencePieceTokenizer
{
    private const char Meta = '▁'; // ▁

    private readonly Dictionary<string, SentencePiece> _pieces;
    private readonly int _maxPieceLength;
    private readonly int _unkId;
    private readonly double _unkScore;

    /// <summary>Creates a tokenizer from a unigram vocabulary (index = id).</summary>
    /// <param name="vocab">Pieces with scores; ids 0..n-1 in order. Control pieces (unk/bos/eos) may be included.</param>
    /// <param name="unkId">The unknown-piece id (default 0).</param>
    public SentencePieceTokenizer(IReadOnlyList<SentencePiece> vocab, int unkId = 0)
    {
        Guard.NotNull(vocab);
        _pieces = new Dictionary<string, SentencePiece>(vocab.Count, StringComparer.Ordinal);
        double minScore = 0;
        foreach (SentencePiece p in vocab)
        {
            // Skip the control pieces so they never match real text.
            if (p.Id is 0 or 1 or 2 && p.Piece.StartsWith('<'))
            {
                continue;
            }
            _pieces[p.Piece] = p;
            _maxPieceLength = Math.Max(_maxPieceLength, p.Piece.Length);
            minScore = Math.Min(minScore, p.Score);
        }
        _unkId = unkId;
        _unkScore = minScore - 10.0; // heavy penalty; only used for uncovered characters
    }

    /// <summary>Tokenizes <paramref name="text"/> into unigram pieces and their ids.</summary>
    public TokenizationResult Encode(string text)
    {
        Guard.NotNull(text);
        string s = Preprocess(text);
        if (s.Length == 0)
        {
            return new TokenizationResult([], []);
        }

        int n = s.Length;
        var best = new double[n + 1];
        var startAt = new int[n + 1];
        var idAt = new int[n + 1];
        for (int f = 0; f < best.Length; f++)
        {
            best[f] = double.NegativeInfinity;
        }
        best[0] = 0;

        for (int i = 0; i < n; i++)
        {
            if (double.IsNegativeInfinity(best[i]))
            {
                continue;
            }

            bool matchedSingle = false;
            int maxL = Math.Min(_maxPieceLength, n - i);
            for (int l = 1; l <= maxL; l++)
            {
                string sub = s.Substring(i, l);
                if (_pieces.TryGetValue(sub, out SentencePiece p))
                {
                    if (l == 1)
                    {
                        matchedSingle = true;
                    }
                    double cand = best[i] + p.Score;
                    if (cand > best[i + l])
                    {
                        best[i + l] = cand;
                        startAt[i + l] = i;
                        idAt[i + l] = p.Id;
                    }
                }
            }

            // Uncovered single character -> unknown piece.
            if (!matchedSingle)
            {
                double cand = best[i] + _unkScore;
                if (cand > best[i + 1])
                {
                    best[i + 1] = cand;
                    startAt[i + 1] = i;
                    idAt[i + 1] = _unkId;
                }
            }
        }

        // Backtrack.
        var ids = new List<int>();
        var tokens = new List<string>();
        for (int j = n; j > 0;)
        {
            int i = startAt[j];
            ids.Add(idAt[j]);
            tokens.Add(s.Substring(i, j - i));
            j = i;
        }
        ids.Reverse();
        tokens.Reverse();
        return new TokenizationResult(tokens, ids);
    }

    private static string Preprocess(string text)
    {
        // remove_extra_whitespaces: collapse runs of whitespace, trim.
        string[] parts = text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return string.Empty;
        }
        // add_dummy_prefix + escape whitespace to the meta symbol.
        return Meta + string.Join(Meta.ToString(), parts);
    }
}
