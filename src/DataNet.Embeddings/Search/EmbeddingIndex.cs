namespace DataNet.Embeddings.Search;

/// <summary>A single search hit: the item's index and its similarity score.</summary>
public readonly record struct SearchResult(int Index, float Score);

/// <summary>
/// An exhaustive (brute-force) cosine-similarity index for semantic search.
/// </summary>
/// <remarks>
/// <para>
/// Vectors are stored contiguously and, by default, L2-normalized on insertion so
/// that cosine similarity reduces to a SIMD dot product. Exhaustive search is the
/// right default up to hundreds of thousands of vectors; an approximate index
/// (HNSW) is only worth adding once a real need is demonstrated (brief, Lot 3).
/// </para>
/// <para>Adding is not thread-safe; concurrent <see cref="Search"/> calls are.</para>
/// </remarks>
public sealed class EmbeddingIndex
{
    private readonly int _dim;
    private readonly bool _normalize;
    private float[] _data = Array.Empty<float>();
    private int _length;
    private int _count;

    /// <summary>Creates an index for vectors of the given dimension.</summary>
    /// <param name="dimension">The embedding dimension.</param>
    /// <param name="normalize">L2-normalize vectors on insertion and queries on search (default true).</param>
    public EmbeddingIndex(int dimension, bool normalize = true)
    {
        Guard.NotLessThan(dimension, 1);
        _dim = dimension;
        _normalize = normalize;
    }

    /// <summary>Number of indexed vectors.</summary>
    public int Count => _count;

    /// <summary>The embedding dimension.</summary>
    public int Dimension => _dim;

    /// <summary>Adds a vector to the index (a normalized copy is stored when normalization is on).</summary>
    public void Add(ReadOnlySpan<float> vector)
    {
        if (vector.Length != _dim)
        {
            throw new ArgumentException($"vector length {vector.Length} != dimension {_dim}.", nameof(vector));
        }

        if (_data.Length < _length + _dim)
        {
            int newCapacity = _data.Length == 0 ? Math.Max(_dim * 4, _dim) : _data.Length * 2;
            if (newCapacity < _length + _dim)
            {
                newCapacity = _length + _dim;
            }
            Array.Resize(ref _data, newCapacity);
        }

        int start = _length;
        vector.CopyTo(_data.AsSpan(start, _dim));
        _length += _dim;
        if (_normalize)
        {
            NormalizeStored(start);
        }
        _count++;
    }

    /// <summary>Returns the <paramref name="k"/> most similar items to <paramref name="query"/>, best first.</summary>
    public IReadOnlyList<SearchResult> Search(ReadOnlySpan<float> query, int k)
    {
        if (query.Length != _dim)
        {
            throw new ArgumentException($"query length {query.Length} != dimension {_dim}.", nameof(query));
        }
        Guard.NotLessThan(k, 1);

        float[]? owned = null;
        ReadOnlySpan<float> q = query;
        if (_normalize)
        {
            owned = query.ToArray();
            float norm = VectorMath.L2Norm(owned);
            if (norm > 0)
            {
                for (int i = 0; i < owned.Length; i++)
                {
                    owned[i] /= norm;
                }
            }
            q = owned;
        }

        ReadOnlySpan<float> data = _data.AsSpan(0, _length);
        var scored = new SearchResult[_count];
        for (int item = 0; item < _count; item++)
        {
            ReadOnlySpan<float> row = data.Slice(item * _dim, _dim);
            scored[item] = new SearchResult(item, VectorMath.Dot(q, row));
        }

        // Stable top-k: sort by score desc, then index asc (matches numpy argsort tie-break intent).
        Array.Sort(scored, static (x, y) =>
        {
            int c = y.Score.CompareTo(x.Score);
            return c != 0 ? c : x.Index.CompareTo(y.Index);
        });

        int take = Math.Min(k, _count);
        var result = new SearchResult[take];
        Array.Copy(scored, result, take);
        return result;
    }

    private void NormalizeStored(int start)
    {
        double sum = 0;
        for (int i = 0; i < _dim; i++)
        {
            float v = _data[start + i];
            sum += (double)v * v;
        }
        double norm = Math.Sqrt(sum);
        if (norm == 0)
        {
            return;
        }
        for (int i = 0; i < _dim; i++)
        {
            _data[start + i] = (float)(_data[start + i] / norm);
        }
    }
}
