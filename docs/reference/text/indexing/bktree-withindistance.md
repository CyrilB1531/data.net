# BkTree.WithinDistance

Every indexed item within a radius of the query, nearest first.

<!-- docs-declaration -->

```csharp
public IReadOnlyList<BkTreeMatch> WithinDistance(string query, int maxDistance, int? limit = null)
```

**Parameters** — `query` is the string to search around. `maxDistance` is the inclusive radius in
the tree's own metric; `0` finds only an exactly equal item. `limit` caps the returned list at the
**nearest** hits, never the first the traversal happened to reach — and it does not bound the
search, because a nearer hit can be found at any point in it.

**Returns** — `IReadOnlyList<BkTreeMatch>`, distance ascending, ties by insertion order. Empty when
nothing is within the radius, and when the tree is empty.

**Example** — a small dictionary and the single-edit neighbourhood of a misspelling.

```csharp
using Lodestar.Text.Indexing;

BkTree tree = BkTree.OverLevenshtein();
tree.AddRange(["book", "books", "boo", "cook", "cake"]);

IReadOnlyList<BkTreeMatch> hits = tree.WithinDistance("bok", 1);
int found = hits.Count;        // => 2
string nearest = hits[0].Item; // => book
```

**Remarks** — the traversal descends into a child only when its key lies in
`[d - maxDistance, d + maxDistance]`, which is the triangle inequality and is why the metric must
satisfy it. At `int.MaxValue` the bound is skipped rather than computed, since it would overflow
and admits everything anyway.

The pruning is worth roughly three times fewer distance computations at a radius of 1 and nothing
at a radius of 3 or more — see [`BkTree`](bktree.md) for the measured table. A large radius over a
large corpus is a linear scan wearing a tree.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BkTree.Nearest`](bktree-nearest.md), [`BkTreeMatch`](bktreematch.md),
[`Process.ExtractIndexed`](../../fuzzy/matching/process-extractindexed.md).
