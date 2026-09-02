# BkTree.OverDamerauLevenshtein

Builds an empty tree over `DamerauLevenshtein.Distance`.

<!-- docs-declaration -->

```csharp
public static BkTree OverDamerauLevenshtein(TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `element` says what counts as one character: `TextElement.Utf16Unit` by default,
or `TextElement.CodePoint` outside the Basic Multilingual Plane.

**Returns** — an empty `BkTree`.

**Example** — a transposition costs one edit here, so the pair is a neighbour.

```csharp
using Lodestar.Text.Indexing;

BkTree tree = BkTree.OverDamerauLevenshtein();
tree.AddRange(["form", "from"]);
int found = tree.WithinDistance("form", 1).Count;   // => 2
```

**Remarks** — the **unrestricted** variant, which is a true metric. Its restricted cousin `Osa` is
not, and must not be used with this tree: `d("ab","bca") = 3` exceeds
`d("ab","ba") + d("ba","bca") = 2`, so the pruning would drop real hits without saying so.

Reach for this over [`OverLevenshtein`](bktree-overlevenshtein.md) when transposed keystrokes are
the error you are correcting.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BkTree`](bktree.md),
[`DamerauLevenshtein.Distance`](../distances/dameraulevenshtein-distance.md).
