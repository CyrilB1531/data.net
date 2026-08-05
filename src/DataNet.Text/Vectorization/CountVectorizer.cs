namespace DataNet.Text.Vectorization;

// SonarLint S3776: cognitive complexity: a faithful implementation of a published rule-engine; decomposing it would break the 1:1 mapping with the reference that makes divergences auditable.
#pragma warning disable S3776

/// <summary>Configuration for <see cref="CountVectorizer"/> (and, via composition, TF-IDF).</summary>
/// <remarks>Defaults mirror <c>sklearn.feature_extraction.text.CountVectorizer</c>.</remarks>
public sealed record CountVectorizerOptions
{
    /// <summary>Lowercase documents before tokenizing. Default <c>true</c>.</summary>
    public bool Lowercase { get; init; } = true;

    /// <summary>Strip accents via NFKD decomposition (like sklearn <c>strip_accents="unicode"</c>). Default <c>false</c>.</summary>
    public bool StripAccents { get; init; }

    /// <summary>Which analyzer to use. Default <see cref="AnalyzerKind.Word"/>.</summary>
    public AnalyzerKind Analyzer { get; init; } = AnalyzerKind.Word;

    /// <summary>Inclusive (min, max) n-gram sizes. Default <c>(1, 1)</c>.</summary>
    public (int Min, int Max) NgramRange { get; init; } = (1, 1);

    /// <summary>
    /// Minimum document frequency. A value in <c>(0, 1)</c> is a proportion of documents;
    /// a value <c>&gt;= 1</c> is an absolute count. Default <c>1</c>.
    /// </summary>
    public double MinDf { get; init; } = 1.0;

    /// <summary>
    /// Maximum document frequency. A value in <c>[0, 1]</c> is a proportion; a value
    /// <c>&gt; 1</c> is an absolute count. Default <c>1.0</c> (all documents).
    /// </summary>
    public double MaxDf { get; init; } = 1.0;

    /// <summary>If true, all non-zero counts are set to 1. Default <c>false</c>.</summary>
    public bool Binary { get; init; }

    /// <summary>Stop words to remove after tokenizing (word analyzer only). Default none.</summary>
    public IReadOnlyCollection<string>? StopWords { get; init; }

    /// <summary>Regex selecting word tokens. Default matches runs of two or more word characters.</summary>
    public string TokenPattern { get; init; } = @"\b\w\w+\b";

    /// <summary>Compares every option, treating <see cref="StopWords"/> as a set.</summary>
    /// <param name="other">The options to compare against.</param>
    /// <remarks>
    /// <para>
    /// The generated equality compares <see cref="StopWords"/> by reference, so two
    /// configurations built from the same list literal would be unequal — and
    /// comparing two configurations is a far more natural thing to do than
    /// comparing two fitted models. <see cref="TfidfVectorizerOptions"/> embeds this
    /// record, so it inherits the fix.
    /// </para>
    /// <para>
    /// Set semantics, not sequence: stop words are looked up, never enumerated in
    /// order, so two lists holding the same words configure the same vectorizer
    /// whatever their order or repetition.
    /// </para>
    /// </remarks>
    public bool Equals(CountVectorizerOptions? other)
    {
        if (ReferenceEquals(this, other))
        {
            return true;
        }
        if (other is null
            || Lowercase != other.Lowercase
            || StripAccents != other.StripAccents
            || Analyzer != other.Analyzer
            || NgramRange != other.NgramRange
            || !MinDf.Equals(other.MinDf)
            || !MaxDf.Equals(other.MaxDf)
            || Binary != other.Binary
            || !string.Equals(TokenPattern, other.TokenPattern, StringComparison.Ordinal))
        {
            return false;
        }
        if (StopWords is null || other.StopWords is null)
        {
            return StopWords is null && other.StopWords is null;
        }
        var mine = new HashSet<string>(StopWords, StringComparer.Ordinal);
        return mine.SetEquals(other.StopWords);
    }

    /// <summary>Hashes the scalars, which is O(1).</summary>
    /// <remarks>
    /// <see cref="StopWords"/> contributes only whether it is present. Its
    /// <em>count</em> cannot be used: <see cref="Equals(CountVectorizerOptions)"/>
    /// compares as a set, so <c>["the", "the"]</c> equals <c>["the"]</c> while the
    /// counts differ — and equal objects are required to hash alike. Hashing the
    /// words themselves would mean hashing all of them, order-independently, which
    /// is the O(n) this exists to avoid. Unequal options are allowed to collide.
    /// </remarks>
    public override int GetHashCode()
    {
        unchecked
        {
            int hash = (17 * 31) + (Lowercase ? 1 : 0);
            hash = (hash * 31) + (StripAccents ? 1 : 0);
            hash = (hash * 31) + (int)Analyzer;
            hash = (hash * 31) + NgramRange.GetHashCode();
            hash = (hash * 31) + MinDf.GetHashCode();
            hash = (hash * 31) + MaxDf.GetHashCode();
            hash = (hash * 31) + (Binary ? 1 : 0);
            hash = (hash * 31) + StringComparer.Ordinal.GetHashCode(TokenPattern);
            return (hash * 31) + (StopWords is null ? -1 : 0);
        }
    }
}

/// <summary>
/// Converts a collection of documents into a sparse matrix of token counts,
/// reproducing <c>sklearn.feature_extraction.text.CountVectorizer</c>.
/// </summary>
/// <remarks>Not thread-safe during <see cref="Fit"/>; read-only afterwards.</remarks>
public sealed partial class CountVectorizer
{
    private readonly CountVectorizerOptions _options;
    private readonly TextAnalyzer _analyzer;
    private Dictionary<string, int>? _vocabulary;
    private string[] _featureNames = [];

    /// <summary>Creates a vectorizer with the given options (defaults if omitted).</summary>
    public CountVectorizer(CountVectorizerOptions? options = null)
    {
        _options = options ?? new CountVectorizerOptions();
        _analyzer = new TextAnalyzer(
            _options.Lowercase,
            _options.StripAccents,
            _options.Analyzer,
            _options.NgramRange,
            _options.TokenPattern,
            _options.StopWords);
    }

    /// <summary>The learned vocabulary, sorted; valid after <see cref="Fit"/>/<see cref="FitTransform"/>.</summary>
    public IReadOnlyList<string> GetFeatureNames()
    {
        EnsureFitted();
        return _featureNames;
    }

    /// <summary>Learns the vocabulary from <paramref name="documents"/>.</summary>
    public CountVectorizer Fit(IEnumerable<string> documents)
    {
        FitTransform(documents);
        return this;
    }

    /// <summary>Learns the vocabulary and returns the count matrix in one pass.</summary>
    public CsrMatrix FitTransform(IEnumerable<string> documents)
    {
        var docs = documents as IReadOnlyList<string> ?? documents.ToList();
        int nDocs = docs.Count;

        // First pass: provisional vocabulary (first-seen order) and per-document counts.
        var provisional = new Dictionary<string, int>(StringComparer.Ordinal);
        var perDoc = new List<Dictionary<int, int>>(nDocs);
        foreach (string doc in docs)
        {
            var counts = new Dictionary<int, int>();
            foreach (string term in _analyzer.Analyze(doc))
            {
                if (!provisional.TryGetValue(term, out int col))
                {
                    col = provisional.Count;
                    provisional[term] = col;
                }
                counts[col] = counts.TryGetValue(col, out int c) ? c + 1 : 1;
            }
            perDoc.Add(counts);
        }

        // Document frequencies over the provisional columns.
        var df = new int[provisional.Count];
        foreach (Dictionary<int, int> counts in perDoc)
        {
            foreach (int col in counts.Keys)
            {
                df[col]++;
            }
        }

        // Document-frequency limits (sklearn _limit_features semantics).
        double low = _options.MinDf is > 0 and < 1 ? _options.MinDf * nDocs : _options.MinDf;
        double high = _options.MaxDf <= 1.0 ? _options.MaxDf * nDocs : _options.MaxDf;

        // Kept terms, sorted -> final column index.
        var kept = new List<string>();
        foreach (KeyValuePair<string, int> entry in provisional)
        {
            string term = entry.Key;
            int col = entry.Value;
            if (df[col] >= low && df[col] <= high)
            {
                kept.Add(term);
            }
        }
        kept.Sort(StringComparer.Ordinal);

        _featureNames = kept.ToArray();
        _vocabulary = new Dictionary<string, int>(kept.Count, StringComparer.Ordinal);
        for (int i = 0; i < kept.Count; i++)
        {
            _vocabulary[kept[i]] = i;
        }

        // Map provisional columns to final columns (or -1 if dropped).
        var remap = new int[provisional.Count];
        foreach (KeyValuePair<string, int> entry in provisional)
        {
            string term = entry.Key;
            int col = entry.Value;
            remap[col] = _vocabulary.TryGetValue(term, out int finalCol) ? finalCol : -1;
        }

        return BuildMatrix(perDoc, remap, _featureNames.Length);
    }

    /// <summary>Transforms <paramref name="documents"/> using the already-learned vocabulary.</summary>
    public CsrMatrix Transform(IEnumerable<string> documents)
    {
        EnsureFitted();
        var docs = documents as IReadOnlyList<string> ?? documents.ToList();

        var rowPointers = new int[docs.Count + 1];
        var values = new List<double>();
        var columns = new List<int>();

        for (int row = 0; row < docs.Count; row++)
        {
            var counts = new Dictionary<int, int>();
            foreach (string term in _analyzer.Analyze(docs[row]))
            {
                if (_vocabulary!.TryGetValue(term, out int col))
                {
                    counts[col] = counts.TryGetValue(col, out int c) ? c + 1 : 1;
                }
            }
            AppendRow(counts, values, columns);
            rowPointers[row + 1] = values.Count;
        }

        return CsrMatrix.CreateUnchecked(docs.Count, _featureNames.Length, values.ToArray(), columns.ToArray(), rowPointers);
    }

    private CsrMatrix BuildMatrix(List<Dictionary<int, int>> perDoc, int[] remap, int columnCount)
    {
        var rowPointers = new int[perDoc.Count + 1];
        var values = new List<double>();
        var columns = new List<int>();

        for (int row = 0; row < perDoc.Count; row++)
        {
            var mapped = new Dictionary<int, int>();
            foreach (KeyValuePair<int, int> entry in perDoc[row])
            {
                int provCol = entry.Key;
                int count = entry.Value;
                int finalCol = remap[provCol];
                if (finalCol >= 0)
                {
                    mapped[finalCol] = count;
                }
            }
            AppendRow(mapped, values, columns);
            rowPointers[row + 1] = values.Count;
        }

        return CsrMatrix.CreateUnchecked(perDoc.Count, columnCount, values.ToArray(), columns.ToArray(), rowPointers);
    }

    private void AppendRow(Dictionary<int, int> counts, List<double> values, List<int> columns)
    {
        foreach (int col in counts.Keys.OrderBy(c => c))
        {
            columns.Add(col);
            values.Add(_options.Binary ? 1.0 : counts[col]);
        }
    }

    private void EnsureFitted()
    {
        if (_vocabulary is null)
        {
            throw new InvalidOperationException("The vectorizer has not been fitted. Call Fit or FitTransform first.");
        }
    }
}
