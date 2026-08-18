# PairConfusionMatrix.ToArray

The same four counts as a 2×2 array, in scikit-learn's own order.

<!-- docs-declaration -->

```csharp
public long[,] ToArray()
```

**Parameters** — none.

**Returns** — `long[,]`, always `[[DifferentInBoth, SameInPredictedOnly], [SameInTrueOnly,
SameInBoth]]` — the same shape `pair_confusion_matrix` returns in numpy.

**Example** — reading a computed matrix both ways.

```csharp
using Lodestar.Metrics;

PairConfusionMatrix pairs = PairConfusionMatrix.Compute([0, 0, 1, 1], [1, 1, 0, 0]);
long[,] grid = pairs.ToArray();
long topLeft = grid[0, 0];   // => 8
long bottomRight = grid[1, 1];   // => 4
```

**Remarks** — for reading ported Python side by side, where the code indexes `C[0, 1]` or
`C[1, 0]` and you want the same indices to mean the same thing. Writing new code, prefer the named
properties on [`PairConfusionMatrix`](pairconfusionmatrix.md) instead: `[0,1]` and `[1,0]` are easy
to transpose by accident, and `SameInPredictedOnly` cannot be mistaken for `SameInTrueOnly` the way
two array indices can.

The matrix is **not symmetric** — scikit-learn's own documentation says so — and this array does
not fix that: `grid[0, 1]` and `grid[1, 0]` answer different questions, exactly as
`SameInPredictedOnly` and `SameInTrueOnly` do.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`PairConfusionMatrix`](pairconfusionmatrix.md),
[`PairConfusionMatrix.Compute`](pairconfusionmatrix-compute.md).
