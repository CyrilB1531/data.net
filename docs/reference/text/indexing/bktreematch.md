# BkTreeMatch

One hit from a `BkTree` query: the item, and how far it is.

<!-- docs-declaration -->

```csharp
public readonly record struct BkTreeMatch(string Item, int Distance)
```

**Parameters** — `Item` is the indexed string. `Distance` is what the tree's own metric returned
for it against the query.

**Example** — the exact match itself, at distance zero.

```csharp
using Lodestar.Text.Indexing;

BkTree tree = BkTree.OverLevenshtein();
tree.AddRange(["book", "cook"]);

BkTreeMatch hit = tree.WithinDistance("book", 1)[0];
string item = hit.Item;       // => book
int distance = hit.Distance;  // => 0
```

**Remarks** — `Distance` is an integer edit distance, not a normalized score. Comparing it against
a similarity in `[0, 100]` — a `Fuzz` ratio, say — is comparing two different quantities, and it is
the mistake [`Process.ExtractIndexed`](../../fuzzy/matching/process-extractindexed.md)'s contract
is written to prevent.

A `record struct`, so equality is by value and two hits with the same item and distance compare
equal.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BkTree.WithinDistance`](bktree-withindistance.md),
[`BkTree.Nearest`](bktree-nearest.md).
