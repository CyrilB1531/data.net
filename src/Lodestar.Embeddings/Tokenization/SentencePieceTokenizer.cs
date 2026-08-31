using System.Buffers;

namespace Lodestar.Embeddings.Tokenization;

// SonarLint S3776: cognitive complexity: a faithful implementation of a published rule-engine; decomposing it would break the 1:1 mapping with the reference that makes divergences auditable.
#pragma warning disable S3776

/// <summary>A vocabulary piece: its string, log-probability score and id.</summary>
public readonly record struct SentencePiece(string Piece, double Score, int Id);

/// <summary>
/// SentencePiece <em>unigram</em> tokenizer: segments text to maximize the sum of
/// piece log-probabilities, via Viterbi over the vocabulary.
/// </summary>
/// <remarks>
/// Reproduces <c>sentencepiece.SentencePieceProcessor(model_file=…).encode</c>; character
/// map (<see cref="PrecompiledNormalizer"/>) and whitespace flags run first. See
/// <c>docs/equivalence.md</c>'s Unigram rows and <c>docs/guides/embeddings.md</c>'s
/// "Models that are refused" list. Thread-safe after construction.
/// </remarks>
public sealed class SentencePieceTokenizer : ISubwordTokenizer
{
    /// <summary>add_dummy_prefix and escape_whitespaces, the pair this path always applies.</summary>
    /// <remarks>
    /// Shared with the BPE path rather than spelled out twice (decision 0050 §2), which is
    /// what makes the two answer alike. <c>remove_extra_whitespaces</c> is the flag that
    /// separates them: set here, and off for the SentencePiece-BPE lineage, which declares
    /// no normalizer to collapse runs with.
    /// </remarks>
    private static readonly MetaspaceEscape Escape =
        new('▁', MetaspacePrependScheme.Always, removeExtraWhitespaces: true, skipPrependWhenAlreadyPrefixed: false);

    private readonly CharSpanMap<SentencePiece> _pieces;
    private readonly PrecompiledNormalizer? _normalizer;

    // Control/unknown pieces stay out of _pieces so they never match text; a
    // special-token template still needs their ids, so only those few are duplicated.
    private readonly Dictionary<string, int> _nonMatchableIds;
    private readonly int _maxPieceLength;
    private readonly int _unkId;
    private readonly double _unkScore;

    /// <summary>
    /// Creates a tokenizer from a loaded vocabulary, using each piece's declared
    /// type to decide what may match text.
    /// </summary>
    /// <remarks>
    /// Matches <c>sentencepiece.SentencePieceProcessor(model_file=…)</c> followed by
    /// <c>encode</c>. Control and unknown pieces are excluded because
    /// <see cref="SentencePieceVocabulary.Types"/> says so, not because of id position --
    /// see <c>docs/equivalence.md</c>'s <c>sp.IsControl(i)</c> row.
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

        var matchable = new List<KeyValuePair<string, SentencePiece>>(vocabulary.Count);
        _nonMatchableIds = new Dictionary<string, int>(StringComparer.Ordinal);
        double minScore = 0;
        for (int id = 0; id < vocabulary.Count; id++)
        {
            if (!vocabulary.IsMatchable(id))
            {
                _nonMatchableIds[vocabulary.Pieces[id].Piece] = id;
                continue;
            }
            SentencePiece p = vocabulary.Pieces[id];
            matchable.Add(new KeyValuePair<string, SentencePiece>(p.Piece, p));
            _maxPieceLength = Math.Max(_maxPieceLength, p.Piece.Length);
            minScore = Math.Min(minScore, p.Score);
        }
        _pieces = new CharSpanMap<SentencePiece>(matchable);
        _normalizer = vocabulary.Normalizer;
        _unkId = vocabulary.UnkId;
        _unkScore = minScore - 10.0; // heavy penalty; only used for uncovered characters
    }

    /// <summary>Creates a tokenizer from a unigram vocabulary (index = id).</summary>
    /// <param name="vocab">Pieces with scores; ids 0..n-1 in order. Control pieces (unk/bos/eos) may be included.</param>
    /// <param name="unkId">The unknown-piece id (default 0).</param>
    // SonarLint S1133: the removal is already scheduled, and the message below names
    // the release. Dropping a public constructor is a breaking change and so waits
    // for a major; until then the overload has to stay for the callers it was
    // deprecated for, and this rule fires on every [Obsolete] there is.
#pragma warning disable S1133
    [Obsolete("Use SentencePieceTokenizer(SentencePieceVocabulary) instead — id-based control filtering will be removed in v2.0.0")]
#pragma warning restore S1133
    public SentencePieceTokenizer(IReadOnlyList<SentencePiece> vocab, int unkId = 0)
    {
        Guard.NotNull(vocab);
        var matchable = new List<KeyValuePair<string, SentencePiece>>(vocab.Count);
        _nonMatchableIds = new Dictionary<string, int>(StringComparer.Ordinal);
        double minScore = 0;
        foreach (SentencePiece p in vocab)
        {
            // Skip the control pieces so they never match real text.
            if (p.Id is 0 or 1 or 2 && p.Piece.StartsWith('<'))
            {
                _nonMatchableIds[p.Piece] = p.Id;
                continue;
            }
            matchable.Add(new KeyValuePair<string, SentencePiece>(p.Piece, p));
            _maxPieceLength = Math.Max(_maxPieceLength, p.Piece.Length);
            minScore = Math.Min(minScore, p.Score);
        }
        _pieces = new CharSpanMap<SentencePiece>(matchable);
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

        // Rented: three per-call arrays were the rest of #498's bytes. A rental may be
        // longer than asked, so every loop below is bounded by n rather than by Length.
        double[] best = ArrayPool<double>.Shared.Rent(n + 1);
        int[] startAt = ArrayPool<int>.Shared.Rent(n + 1);
        int[] idAt = ArrayPool<int>.Shared.Rent(n + 1);
        try
        {
            for (int f = 0; f <= n; f++)
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
                    if (_pieces.TryGetValue(s.AsSpan(i, l), out SentencePiece p))
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

            // Backtrack right to left: the run of unknown characters below fuses easily
            // because the piece already emitted sits to the right of the one now emitted.
            var ids = new List<int>();
            var tokens = new List<string>();
            int runEnd = -1;
            for (int j = n; j > 0;)
            {
                int i = startAt[j];
                if (idAt[j] == _unkId && runEnd >= 0)
                {
                    // One unknown piece per run of uncovered characters -- docs/equivalence.md's
                    // Unigram row. Rewriting from the run's start keeps this to one substring per step.
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
        finally
        {
            ArrayPool<double>.Shared.Return(best);
            ArrayPool<int>.Shared.Return(startAt);
            ArrayPool<int>.Shared.Return(idAt);
        }
    }

    /// <summary>Looks up a literal vocabulary piece, control markers included.</summary>
    /// <remarks>
    /// Matches <c>sentencepiece.SentencePieceProcessor.piece_to_id(piece)</c>. The
    /// control pieces a template names — <c>&lt;s&gt;</c>, <c>&lt;/s&gt;</c>,
    /// <c>&lt;pad&gt;</c> — resolve here even though they can never match text.
    /// </remarks>
    /// <param name="token">The piece string.</param>
    /// <param name="id">Receives the id when the piece is present.</param>
    public bool TryGetId(string token, out int id)
    {
        Guard.NotNull(token);
        if (_pieces.TryGetValue(token, out SentencePiece piece))
        {
            id = piece.Id;
            return true;
        }
        return _nonMatchableIds.TryGetValue(token, out id);
    }

    private string Preprocess(string text)
    {
        // The model's own normalization first -- it turns a tab, a non-breaking space
        // or an ideographic space into an ordinary space, among what else it rewrites.
        string normalized = _normalizer is null ? text : _normalizer.Normalize(text);

        // One text, one piece here: the unigram path splits at nothing, and its scheme
        // is Always, which prepends to every piece anyway.
        return Escape.Apply(normalized, isFirstSplit: true);
    }
}
