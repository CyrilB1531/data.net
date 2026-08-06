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
/// Reproduces the encoding of a trained SentencePiece unigram model. The stock
/// ALBERT, T5, camemBERT and XLM-R models load and tokenize identically to
/// <c>sentencepiece</c>: their <c>nmt_nfkc</c> character map is applied by
/// <see cref="PrecompiledNormalizer"/>, and the corpus behind that claim replays
/// XLM-R's own 250 002-piece vocabulary. What is still refused, and named when it
/// is, is a model trained with <c>byte_fallback</c> or with an algorithm other
/// than unigram.
/// </para>
/// <para>
/// Preprocessing then follows the model's flags: the character map first, then
/// <c>remove_extra_whitespaces</c> collapsing runs of U+0020 — that character and
/// no other — then <c>add_dummy_prefix</c> and <c>escape_whitespaces</c>, which
/// turn the remaining spaces into the meta symbol <c>▁</c> (U+2581) and prepend
/// one. Getting this exactly right matters: a mismatch makes the downstream
/// model's embeddings wrong while everything still looks like it works.
/// </para>
/// <para>Thread-safe after construction.</para>
/// </remarks>
public sealed class SentencePieceTokenizer
{
    private const char Meta = '▁'; // ▁

    // escape_whitespaces maps this character, and only this one, to the meta
    // symbol. Anything else a reader would call whitespace is either rewritten to
    // it by the model's normalizer or left alone as ordinary text.
    private static readonly char[] Spaces = [' '];

    private readonly Dictionary<string, SentencePiece> _pieces;
    private readonly PrecompiledNormalizer? _normalizer;
    private readonly int _maxPieceLength;
    private readonly int _unkId;
    private readonly double _unkScore;

    /// <summary>
    /// Creates a tokenizer from a loaded vocabulary, using each piece's declared
    /// type to decide what may match text.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Matches <c>sentencepiece.SentencePieceProcessor(model_file=…)</c> followed
    /// by <c>encode</c>. Control and unknown pieces are excluded from matching
    /// because <see cref="SentencePieceVocabulary.Types"/> says they are control
    /// and unknown pieces — not because of where they happen to sit in the id
    /// space. A model that places <c>&lt;s&gt;</c> anywhere other than id 1
    /// tokenizes correctly here.
    /// </para>
    /// </remarks>
    /// <param name="vocabulary">A vocabulary from <see cref="Persistence.SentencePieceModelLoader"/> or <see cref="Persistence.TokenizerJsonLoader"/>.</param>
    /// <exception cref="ArgumentException">The vocabulary's pieces and types disagree in length, or its unknown id is out of range.</exception>
    public SentencePieceTokenizer(SentencePieceVocabulary vocabulary)
    {
        Guard.NotNull(vocabulary);
        if (vocabulary.Pieces.Count != vocabulary.Types.Count)
        {
            throw new ArgumentException(
                $"The vocabulary has {vocabulary.Pieces.Count} pieces but {vocabulary.Types.Count} types.",
                nameof(vocabulary));
        }
        if (vocabulary.UnkId < 0 || vocabulary.UnkId >= vocabulary.Pieces.Count)
        {
            throw new ArgumentException(
                $"The unknown id {vocabulary.UnkId} is outside the vocabulary range [0, {vocabulary.Pieces.Count}).",
                nameof(vocabulary));
        }

        _pieces = new Dictionary<string, SentencePiece>(vocabulary.Count, StringComparer.Ordinal);
        double minScore = 0;
        for (int id = 0; id < vocabulary.Count; id++)
        {
            if (!vocabulary.IsMatchable(id))
            {
                continue;
            }
            SentencePiece p = vocabulary.Pieces[id];
            _pieces[p.Piece] = p;
            _maxPieceLength = Math.Max(_maxPieceLength, p.Piece.Length);
            minScore = Math.Min(minScore, p.Score);
        }
        _normalizer = vocabulary.Normalizer;
        _unkId = vocabulary.UnkId;
        _unkScore = minScore - 10.0; // heavy penalty; only used for uncovered characters
    }

    /// <summary>Creates a tokenizer from a unigram vocabulary (index = id).</summary>
    /// <param name="vocab">Pieces with scores; ids 0..n-1 in order. Control pieces (unk/bos/eos) may be included.</param>
    /// <param name="unkId">The unknown-piece id (default 0).</param>
    [Obsolete("Use SentencePieceTokenizer(SentencePieceVocabulary) instead — id-based control filtering will be removed in v2.0.0")]
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

        // Backtrack. Walking right to left is also what makes the run of unknown
        // characters below easy to fuse: the piece already emitted is the one to
        // the right of the one being emitted now.
        var ids = new List<int>();
        var tokens = new List<string>();
        int runEnd = -1;
        for (int j = n; j > 0;)
        {
            int i = startAt[j];
            if (idAt[j] == _unkId && runEnd >= 0)
            {
                // sentencepiece emits one unknown piece per *run* of uncovered
                // characters, not one per character: "ＬＥ" comes back as a single
                // token. Rewriting the run from its start rather than prepending to
                // it keeps this to one substring per step. The unknown piece is
                // never matchable, so an id equal to _unkId can only have come from
                // the branch above.
                tokens[tokens.Count - 1] = s.Substring(i, runEnd - i);
            }
            else
            {
                ids.Add(idAt[j]);
                tokens.Add(s.Substring(i, j - i));
                runEnd = idAt[j] == _unkId ? j : -1;
            }
            j = i;
        }
        ids.Reverse();
        tokens.Reverse();
        return new TokenizationResult(tokens, ids);
    }

    private string Preprocess(string text)
    {
        // The model's own normalization first: it is what turns a tab, a
        // non-breaking space or an ideographic space into an ordinary space, among
        // everything else it rewrites.
        string normalized = _normalizer is null ? text : _normalizer.Normalize(text);

        // remove_extra_whitespaces: collapse runs of U+0020, trim. U+0020 and
        // nothing else — sentencepiece splits on the space character, not on
        // "whitespace", so a tab that no normalizer rewrote stays an ordinary
        // character the vocabulary either covers or does not.
        string[] parts = normalized.Split(Spaces, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return string.Empty;
        }
        // add_dummy_prefix + escape whitespace to the meta symbol.
        return Meta + string.Join(Meta.ToString(), parts);
    }
}
