# BkTree(metric)

Builds an empty tree over a metric of the caller's own, for the one case the four factories do
not reach.

<!-- docs-declaration -->

```csharp
public BkTree(Func<string, string, int> metric)
```

**Parameters** — `metric` is the distance the tree indexes on. It must satisfy the triangle
inequality, be symmetric, and return `0` only for equal inputs — the same three properties
[`OverLevenshtein`](bktree-overlevenshtein.md), [`OverDamerauLevenshtein`](bktree-overdameraulevenshtein.md),
[`OverIndel`](bktree-overindel.md) and [`OverHamming`](bktree-overhamming.md) already carry proof
for. Nothing here checks it — it cannot be, from a delegate — so the caller owns the precondition,
and a `metric` that violates it returns an incomplete result set rather than throwing.

**Returns** — an empty `BkTree`.

**Example** — a metric none of the four factories offer.

```csharp
using Lodestar.Text.Distances;
using Lodestar.Text.Indexing;

BkTree tree = new((a, b) => Levenshtein.Distance(a, b));
tree.AddRange(["book", "boo", "cook"]);
int found = tree.WithinDistance("bok", 1).Count;   // => 2
```

**Remarks** — reach for this constructor when the four factories do not name the distance you
need — a domain-specific edit cost, a phonetic distance, anything satisfying the same inequality —
and bring the proof with it: `AdmissibleMetricTests` in the test suite shows the shape that proof
takes, an exhaustive sweep over every triple of words up to a bounded length and alphabet, not a
sample.

Runs on both target frameworks: net10.0, netstandard2.0.

**See also** — [`BkTree`](bktree.md), [`BkTree.OverLevenshtein`](bktree-overlevenshtein.md).
