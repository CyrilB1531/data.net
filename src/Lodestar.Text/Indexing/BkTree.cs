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
    /// unequal lengths; that variant was checked exhaustively and satisfies the inequality.</remarks>
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
