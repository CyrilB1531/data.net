namespace Lodestar.Text.Vectorization;

/// <summary>Options for <see cref="TfidfTransformer"/> / <see cref="TfidfVectorizer"/>.</summary>
/// <remarks>Defaults mirror <c>sklearn.feature_extraction.text.TfidfTransformer</c>.</remarks>
public sealed record TfidfOptions
{
    /// <summary>Multiply term frequencies by the inverse document frequency. Default <c>true</c>.</summary>
    public bool UseIdf { get; init; } = true;

    /// <summary>
    /// Smooth idf weights by adding one to document frequencies, as if an extra document
    /// contained every term once: <c>idf = ln((1 + n) / (1 + df)) + 1</c>. Default <c>true</c>.
    /// </summary>
    public bool SmoothIdf { get; init; } = true;

    /// <summary>Replace term frequency <c>tf</c> with <c>1 + ln(tf)</c>. Default <c>false</c>.</summary>
    public bool SublinearTf { get; init; }

    /// <summary>Row normalization, or <c>null</c> for none. Default <see cref="SparseNorm.L2"/>.</summary>
    public SparseNorm? Norm { get; init; } = SparseNorm.L2;
}

/// <summary>
/// Transforms a count matrix into a TF-IDF matrix, reproducing
/// <c>sklearn.feature_extraction.text.TfidfTransformer</c>.
/// </summary>
public sealed class TfidfTransformer
{
    private readonly TfidfOptions _options;
    private double[]? _idf;

    /// <summary>Creates a transformer with the given options (defaults if omitted).</summary>
    public TfidfTransformer(TfidfOptions? options = null)
    {
        _options = options ?? new TfidfOptions();
    }

    /// <summary>The learned inverse-document-frequency vector (one per feature), or empty if not using idf.</summary>
    public IReadOnlyList<double> Idf => _idf ?? throw new InvalidOperationException("Not fitted.");

    /// <exception cref="ArgumentNullException"><paramref name="counts"/> is null.</exception>
    /// <summary>Learns the idf vector from a count matrix.</summary>
    public TfidfTransformer Fit(CsrMatrix counts)
    {
        Guard.NotNull(counts);
        int n = counts.RowCount;
        var df = new int[counts.ColumnCount];
        for (int k = 0; k < counts.NonZeroCount; k++)
        {
            df[counts.ColumnIndices[k]]++;
        }

        var idf = new double[counts.ColumnCount];
        int smooth = _options.SmoothIdf ? 1 : 0;
        double numerator = n + smooth;
        for (int c = 0; c < idf.Length; c++)
        {
            // idf = ln((n + smooth) / (df + smooth)) + 1
            idf[c] = Math.Log(numerator / (df[c] + smooth)) + 1.0;
        }
        _idf = idf;
        return this;
    }

    /// <exception cref="ArgumentException"><paramref name="counts"/> has a different column count from the fit.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="counts"/> is null.</exception>
    /// <exception cref="InvalidOperationException">nothing has been fitted yet.</exception>
    /// <summary>Applies the TF-IDF weighting (and optional normalization) to a count matrix.</summary>
    public CsrMatrix Transform(CsrMatrix counts)
    {
        Guard.NotNull(counts);
        if (_options.UseIdf && _idf is null)
        {
            throw new InvalidOperationException("The transformer has not been fitted. Call Fit or FitTransform first.");
        }
        if (_idf is not null && counts.ColumnCount != _idf.Length)
        {
            // CsrMatrix bounds its own columns, but nothing ties that count to the
            // idf vector — unchecked, a mismatch throws deep in the loop below.
            throw new ArgumentException(
                $"The matrix has {counts.ColumnCount} columns but this transformer was fitted on {_idf.Length} features.",
                nameof(counts));
        }

        var values = new double[counts.NonZeroCount];
        for (int k = 0; k < counts.NonZeroCount; k++)
        {
            double tf = counts.Values[k];
            if (_options.SublinearTf && tf > 0)
            {
                tf = 1.0 + Math.Log(tf);
            }
            if (_options.UseIdf)
            {
                tf *= _idf![counts.ColumnIndices[k]];
            }
            values[k] = tf;
        }

        // Copy structure; values are the freshly weighted ones. The structure came
        // from an already-built matrix, so it needs no second validation pass.
        CsrMatrix result = CsrMatrix.CreateUnchecked(
            counts.RowCount,
            counts.ColumnCount,
            values,
            (int[])counts.ColumnIndices.Clone(),
            (int[])counts.RowPointers.Clone());

        if (_options.Norm is { } norm)
        {
            result.NormalizeRows(norm);
        }
        return result;
    }

    /// <exception cref="ArgumentNullException"><paramref name="counts"/> is null.</exception>
    /// <summary>Fits and transforms in one call.</summary>
    public CsrMatrix FitTransform(CsrMatrix counts) => Fit(counts).Transform(counts);

    /// <summary>The weighting options, for <see cref="TfidfVectorizer"/>'s artifact.</summary>
    internal TfidfOptions Options => _options;

    /// <summary>The learned idf vector, or <c>null</c> if never fitted — unlike <see cref="Idf"/>, this does not throw.</summary>
    internal double[]? FittedIdf => _idf;

    /// <summary>Restores the idf vector from an artifact whose length has already been checked.</summary>
    internal void RestoreIdf(double[] idf) => _idf = idf;
}
