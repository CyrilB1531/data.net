# BkTree.OverHamming

Builds an empty tree over `Hamming.Distance`.

<!-- docs-declaration -->

```csharp
public static BkTree OverHamming(TextElement element = TextElement.Utf16Unit)
```

**Parameters** — `element` says what counts as one character: `TextElement.Utf16Unit` by default,
or `TextElement.CodePoint` outside the Basic Multilingual Plane.

**Returns** — an empty `BkTree`.

**Example** — one differing position.

```csharp
using Lodestar.Text.Indexing;

BkTree tree = BkTree.OverHamming();
tree.AddRange(["1010", "1011", "0000"]);
int found = tree.WithinDistance("1010", 1).Count;   // => 2
```

**Remarks** — Lodestar's `Hamming` is not textbook Hamming: it adds the absolute length difference
rather than refusing unequal lengths. That variant had to be checked rather than assumed, and it
satisfies the triangle inequality — zero violations over every triple of words up to length 4 on a
three-letter alphabet and up to length 6 on a two-letter one.

Positional by nature, so it suits fixed-width codes — identifiers, hashes, fingerprints — far
better than words, where a single insertion shifts every later character and inflates the distance.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BkTree`](bktree.md), [`Hamming.Distance`](../distances/hamming-distance.md).
