# BkTree.OverLevenshtein

Builds an empty tree over `Levenshtein.Distance`.

<!-- docs-declaration -->

```csharp
public static BkTree OverLevenshtein(TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `element` says what counts as one character: `TextElement.Utf16Unit` by default,
or `TextElement.CodePoint` for Python's answer outside the Basic Multilingual Plane.

**Returns** — an empty `BkTree`.

**Example** — one substitution away.

```csharp
using Lodestar.Text.Indexing;

BkTree tree = BkTree.OverLevenshtein();
tree.AddRange(["book", "cook"]);
int found = tree.WithinDistance("book", 1).Count;   // => 2
```

**Remarks** — Levenshtein with unit costs is a metric, so the tree's pruning is sound. This is the
factory to reach for unless a specific reason says otherwise: it is the distance a spelling
corrector means.

Pick the same `element` the rest of your pipeline uses. A tree built at `Utf16Unit` and queried
through code-point-based code is measuring two different things.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BkTree`](bktree.md), [`Levenshtein.Distance`](../distances/levenshtein-distance.md).
