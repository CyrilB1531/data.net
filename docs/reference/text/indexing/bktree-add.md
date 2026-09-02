# BkTree.Add

Adds one item, and says whether it was new.

<!-- docs-declaration -->

```csharp
public bool Add(string item)
```

**Parameters** — `item` is the string to index.

**Returns** — `bool`: `true` when the item was added, `false` when an equal item was already
indexed. Matches `HashSet<T>.Add`.

**Example** — the second add is refused.

```csharp
using Lodestar.Text.Indexing;

BkTree tree = BkTree.OverLevenshtein();
bool firstAdd = tree.Add("book");     // => True
bool secondAdd = tree.Add("book");    // => False
int count = tree.Count;               // => 1
```

**Remarks** — insertion walks from the root, descending into the child keyed by the distance to the
current node and attaching a new node where none exists, so it costs one distance computation per
level. A duplicate is detected as a distance of `0`, which can only ever be the node itself.

Insertion order decides the tree's *shape* and therefore how well it prunes; it never decides an
answer, which is what the tie-break by insertion rank and the shape-independence test exist for.
Adding in sorted order builds a degenerate tree — shuffle a sorted dictionary before indexing it.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BkTree.AddRange`](bktree-addrange.md), `BkTree.Count`.
