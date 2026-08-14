namespace DataNet.Embeddings.Search;

/// <summary>A single search hit: the item's index and its similarity score.</summary>
public readonly record struct SearchResult(int Index, float Score);

/// <summary>
/// An exhaustive (brute-force) cosine-similarity index for semantic search.
/// </summary>
/// <remarks>
/// Stored contiguously and, by default, L2-normalized on insertion so cosine
/// similarity reduces to a SIMD dot product — see the guide's "Index a corpus"
/// section for why exhaustive search is the default rather than HNSW.
/// Adding is not thread-safe; concurrent <see cref="Search"/> calls are.
/// </remarks>
public sealed partial class EmbeddingIndex
{
    private readonly int _dim;
    private readonly bool _normalize;
    private float[] _data = Array.Empty<float>();
    private int _length;
    private int _count;
    private string?[]? _ids;

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

    /// <summary>Adds a vector together with an opaque id the caller can recall after a reload.</summary>
    /// <param name="vector">The embedding, of length <see cref="Dimension"/>.</param>
    /// <param name="id">
    /// Anything identifying the document — a primary key, a URL, a path. Kept
    /// verbatim and never interpreted. <c>null</c> is exactly equivalent to
    /// <see cref="Add(ReadOnlySpan{float})"/>.
    /// </param>
    /// <remarks>
    /// A separate overload rather than an optional parameter on
    /// <see cref="Add(ReadOnlySpan{float})"/>: adding one would change that method's
    /// signature and break every already-compiled caller.
    /// </remarks>
    public void Add(ReadOnlySpan<float> vector, string? id)
    {
        Add(vector);
        if (id is null)
        {
            return;
        }

        // Allocated on the first id and no earlier: an index whose items are
        // anonymous pays nothing for a feature it does not use.
        _ids ??= new string?[_count];
        if (_ids.Length < _count)
        {
            Array.Resize(ref _ids, Math.Max(_count, _ids.Length * 2));
        }
        _ids[_count - 1] = id;
    }

    /// <summary>Whether any vector in this index carries an id.</summary>
    public bool HasIds => _ids is not null;

    /// <summary>The id of the item at <paramref name="index"/>, or <c>null</c> if it has none.</summary>
    /// <param name="index">A position in <c>[0, Count)</c> — a <see cref="SearchResult.Index"/>.</param>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the index.</exception>
    /// <remarks>
    /// The id is looked up here rather than carried on <see cref="SearchResult"/>.
    /// <see cref="Search"/> scores into a <c>SearchResult[Count]</c> and sorts it;
    /// the struct is 8 bytes the collector never has to look inside, and putting a
    /// reference in it would turn that hot array into one the GC must scan and the
    /// sort must move references through.
    /// </remarks>
    public string? GetId(int index)
    {
        if ((uint)index >= (uint)_count)
        {
            throw new ArgumentOutOfRangeException(
                nameof(index),
                index,
                $"index must be in [0, {_count}).");
        }
        return IdAt(index);
    }

    /// <summary>The id at <paramref name="index"/>, unchecked, tolerating a short id buffer.</summary>
    /// <remarks>
    /// The buffer stops at the last item that was given an id, so positions past it
    /// are absent rather than null-filled.
    /// </remarks>
    private string? IdAt(int index) =>
        _ids is not null && index < _ids.Length ? _ids[index] : null;

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

        // SonarLint S1244: exact zero is the only norm that makes the division below
        // undefined; a tolerance compare would leave short vectors unnormalized instead.
#pragma warning disable S1244
        if (norm == 0)
#pragma warning restore S1244
        {
            return;
        }
        for (int i = 0; i < _dim; i++)
        {
            _data[start + i] = (float)(_data[start + i] / norm);
        }
    }
}
