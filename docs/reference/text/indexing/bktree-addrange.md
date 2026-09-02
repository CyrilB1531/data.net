# BkTree.AddRange

Adds every item in a sequence, skipping duplicates.

<!-- docs-declaration -->

```csharp
public void AddRange(IEnumerable<string> items)
```

**Parameters** — `items` is the sequence to index. It is enumerated once.

**Returns** — nothing. Read `Count` to see how many were distinct.

**Example** — one duplicate among five.

```csharp
using Lodestar.Text.Indexing;

BkTree tree = BkTree.OverLevenshtein();
tree.AddRange(["book", "books", "boo", "cook", "book"]);
int count = tree.Count;   // => 4
```

**Remarks** — exactly [`Add`](bktree-add.md) in a loop, which is what makes the tree incremental:
there is no build step, so a dictionary can take a new word without a rebuild. That is the
difference from a VP-tree, which takes its whole array up front.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BkTree.Add`](bktree-add.md), `BkTree.Count`.
