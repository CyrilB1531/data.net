namespace Lodestar.Text.Vectorization;

/// <summary>Options for <see cref="TfidfVectorizer"/>: tokenization plus TF-IDF weighting.</summary>
public sealed record TfidfVectorizerOptions
{
    /// <summary>Tokenization / vocabulary options (shared with <see cref="CountVectorizer"/>).</summary>
    public CountVectorizerOptions Count { get; init; } = new();

    /// <summary>TF-IDF weighting options.</summary>
    public TfidfOptions Tfidf { get; init; } = new();
}

/// <summary>
/// Converts documents directly into a TF-IDF matrix, reproducing
/// <c>sklearn.feature_extraction.text.TfidfVectorizer</c> (a <see cref="CountVectorizer"/>
/// followed by a <see cref="TfidfTransformer"/>).
/// </summary>
public sealed partial class TfidfVectorizer
{
    private readonly CountVectorizer _counts;
    private readonly TfidfTransformer _tfidf;

    /// <summary>Creates a vectorizer with the given options (defaults if omitted).</summary>
    public TfidfVectorizer(TfidfVectorizerOptions? options = null)
    {
        options ??= new TfidfVectorizerOptions();
        _counts = new CountVectorizer(options.Count);
        _tfidf = new TfidfTransformer(options.Tfidf);
    }

    /// <exception cref="InvalidOperationException">nothing has been fitted yet.</exception>
    /// <summary>The learned vocabulary, sorted; valid after fitting.</summary>
    public IReadOnlyList<string> GetFeatureNames() => _counts.GetFeatureNames();

    /// <summary>The learned inverse-document-frequency vector (one per feature).</summary>
    public IReadOnlyList<double> Idf => _tfidf.Idf;

    /// <exception cref="ArgumentNullException"><paramref name="documents"/> is null.</exception>
    /// <summary>Learns the vocabulary and idf weights.</summary>
    public TfidfVectorizer Fit(IEnumerable<string> documents)
    {
        FitTransform(documents);
        return this;
    }

    /// <exception cref="ArgumentNullException"><paramref name="documents"/> is null.</exception>
    /// <summary>Learns and returns the TF-IDF matrix in one pass.</summary>
    public CsrMatrix FitTransform(IEnumerable<string> documents)
    {
        CsrMatrix counts = _counts.FitTransform(documents);
        return _tfidf.FitTransform(counts);
    }

    /// <exception cref="ArgumentNullException"><paramref name="documents"/> is null.</exception>
    /// <exception cref="InvalidOperationException">nothing has been fitted yet.</exception>
    /// <summary>Transforms documents using the already-learned vocabulary and idf weights.</summary>
    public CsrMatrix Transform(IEnumerable<string> documents)
    {
        CsrMatrix counts = _counts.Transform(documents);
        return _tfidf.Transform(counts);
    }
}
