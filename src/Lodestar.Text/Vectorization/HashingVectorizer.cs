using System.Text;

namespace Lodestar.Text.Vectorization;

// SonarLint S3776: cognitive complexity: a faithful implementation of a published rule-engine; decomposing it would break the 1:1 mapping with the reference that makes divergences auditable.
#pragma warning disable S3776

/// <summary>Options for <see cref="HashingVectorizer"/>.</summary>
/// <remarks>Defaults mirror <c>sklearn.feature_extraction.text.HashingVectorizer</c>.</remarks>
public sealed record HashingVectorizerOptions
{
    /// <summary>Tokenization / analysis options (vocabulary-related fields are ignored — hashing is stateless).</summary>
    public CountVectorizerOptions Count { get; init; } = new();

    /// <summary>Number of hash buckets (feature dimensions). Default 2^20.</summary>
    public int NumFeatures { get; init; } = 1 << 20;

    /// <summary>Add a sign from the hash so collisions can cancel (reduces bias). Default <c>true</c>.</summary>
    public bool AlternateSign { get; init; } = true;

    /// <summary>Row normalization, or <c>null</c> for none. Default <see cref="SparseNorm.L2"/>.</summary>
    public SparseNorm? Norm { get; init; } = SparseNorm.L2;
}

/// <summary>
/// Vectorizes documents with the hashing trick — no vocabulary is stored — reproducing
/// <c>sklearn.feature_extraction.text.HashingVectorizer</c>.
/// </summary>
/// <remarks>Stateless and thread-safe: <see cref="Transform"/> depends only on the input.</remarks>
public sealed partial class HashingVectorizer
{
    private readonly HashingVectorizerOptions _options;
    private readonly TextAnalyzer _analyzer;

    /// <summary>Creates a vectorizer with the given options (defaults if omitted).</summary>
    public HashingVectorizer(HashingVectorizerOptions? options = null)
    {
        _options = options ?? new HashingVectorizerOptions();
        if (_options.NumFeatures < 1)
        {
            throw new ArgumentException("NumFeatures must be >= 1.", nameof(options));
        }

        CountVectorizerOptions c = _options.Count;
        _analyzer = new TextAnalyzer(c.Lowercase, c.StripAccents, c.Analyzer, c.NgramRange, c.TokenPattern, c.StopWords);
    }

    /// <summary>Number of hash buckets (columns of the output matrix).</summary>
    public int NumFeatures => _options.NumFeatures;

    /// <exception cref="ArgumentNullException"><paramref name="documents"/> is null.</exception>
    /// <summary>Hashes <paramref name="documents"/> into a sparse matrix. No fitting required.</summary>
    public CsrMatrix Transform(IEnumerable<string> documents)
    {
        var docs = documents as IReadOnlyList<string> ?? documents.ToList();
        int nf = _options.NumFeatures;

        var rowPointers = new int[docs.Count + 1];
        var values = new List<double>();
        var columns = new List<int>();

        for (int row = 0; row < docs.Count; row++)
        {
            var accumulator = new Dictionary<int, double>();
            foreach (string term in _analyzer.Analyze(docs[row]))
            {
                int h = MurmurHash3.Hash32(Encoding.UTF8.GetBytes(term));
                int index = (int)(Math.Abs((long)h) % nf);
                double sign = _options.AlternateSign && h < 0 ? -1.0 : 1.0;
                accumulator[index] = accumulator.TryGetValue(index, out double v) ? v + sign : sign;
            }

            foreach (int col in accumulator.Keys.OrderBy(c => c))
            {
                double value = accumulator[col];

                // SonarLint S1244: this decides what the sparse matrix stores, and
                // "stored" means "not exactly zero". Every accumulated value is a sum
                // of ±1, so exact cancellation is the ordinary outcome when
                // AlternateSign sends two terms to the same column — and it is
                // representable. A tolerance would drop real entries.
#pragma warning disable S1244
                if (value != 0.0)
#pragma warning restore S1244
                {
                    columns.Add(col);
                    values.Add(value);
                }
            }
            rowPointers[row + 1] = values.Count;
        }

        CsrMatrix matrix = CsrMatrix.CreateUnchecked(docs.Count, nf, values.ToArray(), columns.ToArray(), rowPointers);
        if (_options.Norm is { } norm)
        {
            matrix.NormalizeRows(norm);
        }
        return matrix;
    }

    /// <exception cref="ArgumentNullException"><paramref name="documents"/> is null.</exception>
    /// <summary>Alias for <see cref="Transform"/> — the vectorizer is stateless.</summary>
    public CsrMatrix FitTransform(IEnumerable<string> documents) => Transform(documents);
}
