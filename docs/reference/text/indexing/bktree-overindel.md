# BkTree.OverIndel

Builds an empty tree over `Indel.Distance`.

<!-- docs-declaration -->

```csharp
public static BkTree OverIndel(TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `element` says what counts as one character: `TextElement.Utf16Unit` by default,
or `TextElement.CodePoint` outside the Basic Multilingual Plane.

**Returns** — an empty `BkTree`.

**Example** — a substitution costs two here, so a radius of 1 excludes it.

```csharp
using Lodestar.Text.Indexing;

BkTree tree = BkTree.OverIndel();
tree.AddRange(["book", "cook", "boo"]);
int found = tree.WithinDistance("book", 1).Count;   // => 2
```

**Remarks** — `Indel` is `len(a) + len(b) - 2 x Lcs.SubsequenceLength(a, b)`, the LCS edit distance,
and it is a metric. It is also the measure behind rapidfuzz's `fuzz.ratio`, which makes this the
factory to pair with [`Process.ExtractIndexed`](../../fuzzy/matching/process-extractindexed.md)
when the scorer is `Fuzz.Ratio`: the radius and the score then move together, which is what that
method's contract asks of a caller.

`Lcs` itself is not a candidate for a tree — `Lcs.SubsequenceLength` returns a length, not a
distance.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BkTree`](bktree.md), [`Indel.Distance`](../distances/indel-distance.md).
