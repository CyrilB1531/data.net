namespace DataNet.Fuzzy;

/// <summary>
/// Groups near-duplicate records using blocking to avoid a quadratic number of
/// comparisons.
/// </summary>
/// <remarks>
/// Records are first partitioned by a blocking key (e.g. first letter, Soundex
/// code, postal code); pairwise similarity is then computed only within each
/// block. Records whose similarity meets a threshold are linked, and the
/// transitive closure forms clusters. Blocking trades a little recall (true
/// duplicates in different blocks are missed) for a large speed-up.
/// </remarks>
public static class Deduplicator
{
    /// <summary>
    /// Returns clusters of record indices that are mutually (transitively) similar
    /// within their block. Every record appears in exactly one cluster; singletons
    /// are included.
    /// </summary>
    /// <typeparam name="T">The record type.</typeparam>
    /// <param name="records">The records to deduplicate.</param>
    /// <param name="blockingKey">Partitions records; only records sharing a key are compared.</param>
    /// <param name="similarity">Similarity in [0, 100] between two records.</param>
    /// <param name="threshold">Minimum similarity (inclusive) to link two records.</param>
    public static IReadOnlyList<IReadOnlyList<int>> FindClusters<T>(
        IReadOnlyList<T> records,
        Func<T, string> blockingKey,
        Func<T, T, double> similarity,
        double threshold)
    {
        ArgumentNullException.ThrowIfNull(records);
        ArgumentNullException.ThrowIfNull(blockingKey);
        ArgumentNullException.ThrowIfNull(similarity);

        var uf = new UnionFind(records.Count);

        // Bucket record indices by blocking key.
        var blocks = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        for (int i = 0; i < records.Count; i++)
        {
            string key = blockingKey(records[i]);
            if (!blocks.TryGetValue(key, out List<int>? bucket))
            {
                bucket = [];
                blocks[key] = bucket;
            }
            bucket.Add(i);
        }

        // Compare only within blocks.
        foreach (List<int> bucket in blocks.Values)
        {
            for (int x = 0; x < bucket.Count; x++)
            {
                for (int y = x + 1; y < bucket.Count; y++)
                {
                    if (similarity(records[bucket[x]], records[bucket[y]]) >= threshold)
                    {
                        uf.Union(bucket[x], bucket[y]);
                    }
                }
            }
        }

        // Gather clusters by representative, preserving first-appearance order.
        var clusters = new Dictionary<int, List<int>>();
        var order = new List<int>();
        for (int i = 0; i < records.Count; i++)
        {
            int root = uf.Find(i);
            if (!clusters.TryGetValue(root, out List<int>? members))
            {
                members = [];
                clusters[root] = members;
                order.Add(root);
            }
            members.Add(i);
        }

        return order.Select(root => (IReadOnlyList<int>)clusters[root]).ToList();
    }

    private sealed class UnionFind
    {
        private readonly int[] _parent;
        private readonly int[] _rank;

        public UnionFind(int n)
        {
            _parent = new int[n];
            _rank = new int[n];
            for (int i = 0; i < n; i++)
            {
                _parent[i] = i;
            }
        }

        public int Find(int x)
        {
            while (_parent[x] != x)
            {
                _parent[x] = _parent[_parent[x]];
                x = _parent[x];
            }
            return x;
        }

        public void Union(int a, int b)
        {
            int ra = Find(a);
            int rb = Find(b);
            if (ra == rb)
            {
                return;
            }
            if (_rank[ra] < _rank[rb])
            {
                (ra, rb) = (rb, ra);
            }
            _parent[rb] = ra;
            if (_rank[ra] == _rank[rb])
            {
                _rank[ra]++;
            }
        }
    }
}
