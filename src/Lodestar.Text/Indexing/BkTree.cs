using Lodestar.Text.Distances;

namespace Lodestar.Text.Indexing;

/// <summary>
/// A Burkhard-Keller tree: a metric index over strings that answers "everything within
/// edit distance k" without scanning the corpus.
/// </summary>
/// <remarks>
/// Correct <b>only</b> on a distance satisfying the triangle inequality: the pruning to <c>[d - k, d + k]</c>
/// relies on it, and a distance that violates it returns an incomplete set rather than throwing. The four factories
/// bind the ones that qualify — the reference page names them, with the counterexample that excludes <c>Osa</c> and
/// the measured radii where the tree stops paying. Not thread-safe for writes; concurrent queries are.
/// </remarks>
public sealed class BkTree
{
    private readonly Func<string, string, int> metric;
    private Node? root;
    private int count;

    /// <summary>Builds an empty tree over an arbitrary integer distance.</summary>
    /// <param name="metric">
    /// Must satisfy the triangle inequality, be symmetric, and return 0 only for equal
    /// inputs. Not checked — it cannot be, from a delegate.
    /// </param>
    public BkTree(Func<string, string, int> metric)
    {
        Guard.NotNull(metric);
        this.metric = metric;
    }

    /// <summary>Over <see cref="Levenshtein.Distance(ReadOnlySpan{char}, ReadOnlySpan{char}, TextElement)"/>.</summary>
    public static BkTree OverLevenshtein(TextElement element = TextElement.Utf16Unit) =>
        new((a, b) => Levenshtein.Distance(a.AsSpan(), b.AsSpan(), element));

    /// <summary>Over <see cref="DamerauLevenshtein.Distance(ReadOnlySpan{char}, ReadOnlySpan{char}, TextElement)"/>,
    /// the unrestricted variant, which is a true metric.</summary>
    public static BkTree OverDamerauLevenshtein(TextElement element = TextElement.Utf16Unit) =>
        new((a, b) => DamerauLevenshtein.Distance(a.AsSpan(), b.AsSpan(), element));

    /// <summary>Over <see cref="Indel.Distance(ReadOnlySpan{char}, ReadOnlySpan{char}, TextElement)"/>,
    /// the LCS edit distance behind <c>fuzz.ratio</c>.</summary>
    public static BkTree OverIndel(TextElement element = TextElement.Utf16Unit) =>
        new((a, b) => Indel.Distance(a.AsSpan(), b.AsSpan(), element));

    /// <summary>Over <see cref="Hamming.Distance(ReadOnlySpan{char}, ReadOnlySpan{char}, TextElement)"/>.</summary>
    /// <remarks>Lodestar's Hamming adds the absolute length difference rather than refusing
    /// unequal lengths, so it is not textbook Hamming and its triangle inequality had to be
    /// checked rather than assumed — <c>AdmissibleMetricTests</c> does that exhaustively.</remarks>
    public static BkTree OverHamming(TextElement element = TextElement.Utf16Unit) =>
        new((a, b) => Hamming.Distance(a.AsSpan(), b.AsSpan(), element));

    /// <summary>How many distinct items the tree holds.</summary>
    public int Count => this.count;

    /// <summary>Adds one item; returns <c>false</c> if an equal item is already indexed.</summary>
    public bool Add(string item)
    {
        Guard.NotNull(item);

        if (this.root is null)
        {
            this.root = new Node(item, this.count);
            this.count = 1;
            return true;
        }

        Node current = this.root;
        while (true)
        {
            int d = this.metric(item, current.Item);
            if (d == 0)
            {
                return false;
            }

            if (current.Children.TryGetValue(d, out Node? next))
            {
                current = next;
                continue;
            }

            current.Children.Add(d, new Node(item, this.count));
            this.count++;
            return true;
        }
    }

    /// <summary>Adds every item, skipping duplicates.</summary>
    public void AddRange(IEnumerable<string> items)
    {
        Guard.NotNull(items);
        foreach (string item in items)
        {
            this.Add(item);
        }
    }

    /// <summary>Every indexed item within <paramref name="maxDistance"/> of the query.</summary>
    /// <param name="query">The string to search around.</param>
    /// <param name="maxDistance">The inclusive radius; 0 finds only an exactly equal item.</param>
    /// <param name="limit">
    /// Caps the returned list at the <i>nearest</i> hits, never the first the traversal reached.
    /// It is not a bound on the search: a nearer hit can be found at any point in it.
    /// </param>
    /// <returns>Distance ascending, ties by insertion order.</returns>
    public IReadOnlyList<BkTreeMatch> WithinDistance(string query, int maxDistance, int? limit = null)
    {
        Guard.NotNull(query);
        Guard.NotLessThan(maxDistance, 0);
        if (limit is { } cap)
        {
            Guard.NotLessThan(cap, 0);
        }

        List<Hit> hits = this.Collect(query, maxDistance);
        return Sorted(hits, limit);
    }

    /// <summary>The <paramref name="count"/> nearest indexed items, however far they are.</summary>
    /// <returns>Distance ascending, ties by insertion order; shorter than
    /// <paramref name="count"/> when the tree holds fewer items.</returns>
    /// <remarks>The radius starts unbounded and tightens to the worst hit held once
    /// <paramref name="count"/> are found, so the pruning strengthens as the search proceeds.
    /// That is an optimization: the answer is the first <paramref name="count"/> of
    /// <see cref="WithinDistance"/> at an unbounded radius, and a test asserts it.</remarks>
    public IReadOnlyList<BkTreeMatch> Nearest(string query, int count)
    {
        Guard.NotNull(query);
        Guard.NotLessThan(count, 0);

        if (this.root is null || count == 0)
        {
            return [];
        }

        // Capped at what the tree could ever hand back -- never more than count and never
        // more than this.count -- so count == int.MaxValue cannot overflow the +1 below.
        int capacity = Math.Min(count, this.count);
        var best = new List<Hit>(capacity + 1);
        int radius = int.MaxValue;
        var stack = new Stack<Node>();
        stack.Push(this.root);

        while (stack.Count > 0)
        {
            Node node = stack.Pop();
            int d = this.metric(query, node.Item);

            if (d <= radius)
            {
                Insert(best, new Hit(node.Item, d, node.Order), count);
                if (best.Count == count)
                {
                    radius = best[count - 1].Distance;
                }
            }

            foreach (KeyValuePair<int, Node> child in node.Children)
            {
                // Branchless and overflow-safe: unlike d + radius or d - radius, a subtraction
                // of two small distances cannot overflow, however close radius is to int.MaxValue.
                if (child.Key - d <= radius && d - child.Key <= radius)
                {
                    stack.Push(child.Value);
                }
            }
        }

        return [.. best.Select(static h => new BkTreeMatch(h.Item, h.Distance))];
    }

    /// <summary>Walks the tree once, keeping everything inside the radius.</summary>
    private List<Hit> Collect(string query, int maxDistance)
    {
        var hits = new List<Hit>();
        if (this.root is null)
        {
            return hits;
        }

        var stack = new Stack<Node>();
        stack.Push(this.root);
        while (stack.Count > 0)
        {
            Node node = stack.Pop();
            int d = this.metric(query, node.Item);
            if (d <= maxDistance)
            {
                hits.Add(new Hit(node.Item, d, node.Order));
            }

            foreach (KeyValuePair<int, Node> child in node.Children)
            {
                // Branchless and overflow-safe: unlike d + maxDistance or d - maxDistance, a
                // subtraction of two small distances cannot overflow near int.MaxValue.
                if (child.Key - d <= maxDistance && d - child.Key <= maxDistance)
                {
                    stack.Push(child.Value);
                }
            }
        }

        return hits;
    }

    /// <summary>Sorts by distance then insertion order, and applies the cap after that.</summary>
    private static BkTreeMatch[] Sorted(List<Hit> hits, int? limit)
    {
        hits.Sort(static (x, y) =>
        {
            int c = x.Distance.CompareTo(y.Distance);
            return c != 0 ? c : x.Order.CompareTo(y.Order);
        });

        int take = limit is { } cap && cap < hits.Count ? cap : hits.Count;
        var ordered = new BkTreeMatch[take];
        for (int i = 0; i < take; i++)
        {
            ordered[i] = new BkTreeMatch(hits[i].Item, hits[i].Distance);
        }
        return ordered;
    }

    /// <summary>Inserts into an already-sorted bounded list, dropping the worst past the cap.</summary>
    private static void Insert(List<Hit> best, Hit hit, int capacity)
    {
        int at = best.Count;
        while (at > 0 && (best[at - 1].Distance > hit.Distance
            || (best[at - 1].Distance == hit.Distance && best[at - 1].Order > hit.Order)))
        {
            at--;
        }

        best.Insert(at, hit);
        if (best.Count > capacity)
        {
            best.RemoveAt(best.Count - 1);
        }
    }

    /// <summary>A hit carrying the insertion rank the public result drops.</summary>
    private readonly record struct Hit(string Item, int Distance, int Order);

    /// <summary>One indexed item, its insertion rank, and its children keyed by exact distance.</summary>
    /// <remarks>Insertion rank is the tie-break the queries order by, so an answer does not
    /// depend on the shape insertion order gave the tree.</remarks>
    private sealed class Node(string item, int order)
    {
        public string Item { get; } = item;

        public int Order { get; } = order;

        public Dictionary<int, Node> Children { get; } = [];
    }
}
