# BkTree.Nearest

The *n* nearest indexed items, however far away they are.

<!-- docs-declaration -->

```csharp
public IReadOnlyList<BkTreeMatch> Nearest(string query, int count)
```

**Parameters** — `query` is the string to search around; `count` is how many hits to return.

**Returns** — `IReadOnlyList<BkTreeMatch>`, distance ascending, ties by insertion order. Shorter
than `count` when the tree holds fewer items, and empty when it holds none or `count` is `0`.

**Example** — the three closest entries, with no radius to choose.

```csharp
using Lodestar.Text.Indexing;

BkTree tree = BkTree.OverLevenshtein();
tree.AddRange(["book", "books", "boo", "cook", "cake"]);

IReadOnlyList<BkTreeMatch> nearest = tree.Nearest("bok", 3);
int closest = nearest[0].Distance;   // => 1
int found = nearest.Count;           // => 3
```

**Remarks** — this is the query that genuinely tightens as it runs: the radius starts unbounded and
becomes the worst distance held once `count` hits are found, so every improvement narrows the
remaining search. The answer is nevertheless exactly the first `count` of
[`WithinDistance`](bktree-withindistance.md) at an unbounded radius — the shrinking radius is an
optimization, and a test asserts it changes nothing.

Use it when you do not know a radius. When you do,
[`WithinDistance`](bktree-withindistance.md) prunes from the first node rather than after the
`count`-th hit.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BkTree.WithinDistance`](bktree-withindistance.md), [`BkTreeMatch`](bktreematch.md).
