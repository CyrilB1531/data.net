# PairConfusionMatrix

How two labellings pair the samples up: for every ordered pair, whether each labelling put the
two together.

<!-- docs-declaration -->

```csharp
public readonly record struct PairConfusionMatrix(
    long DifferentInBoth,
    long SameInPredictedOnly,
    long SameInTrueOnly,
    long SameInBoth)
```

**Example** — one class split into two clusters.

```csharp
using Lodestar.Metrics;

PairConfusionMatrix pairs = PairConfusionMatrix.Compute([0, 0, 1, 1], [0, 1, 2, 3]);
long agreeing = pairs.SameInBoth;   // => 0
long disagreeing = pairs.SameInTrueOnly;   // => 4
```

**Remarks** — four fields, all `long`. **This is not a [`ConfusionMatrix`](../classification/confusionmatrix.md)**,
which is a different type answering a different question: `ConfusionMatrix` counts *labels*, one
cell per (true class, predicted class); this type counts **ordered pairs of samples**, one cell
per (were-they-together-in-truth, were-they-together-in-the-prediction). Reusing the name would
have been wrong and reusing the type would have been worse — a caller reading `ConfusionMatrix`
would expect label counts and get pair counts instead.

They are `long` because they reach `long` scale fast: the four values sum to `n²`, which is about
`5·10⁹` at a hundred thousand samples — past `int.MaxValue`.

The names follow a truth-table reading, and [`RandIndex.Score`](randindex-score.md) is built from
exactly these four: `DifferentInBoth` and `SameInBoth` are the pairs the two labellings agree
about, and dividing their sum by the total is the whole of the Rand index.

Being a `record struct`, it compares by value and deconstructs — `var (diff, predOnly, trueOnly,
both) = pairs;` works, in declaration order.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`ConfusionMatrix`](../classification/confusionmatrix.md), [`RandIndex.Score`](randindex-score.md),
[the clustering index](../clustering.md).

## Members

| Member | What it does |
| --- | --- |
| [`PairConfusionMatrix.Compute`](pairconfusionmatrix-compute.md) | Counts the pairs two labellings agree and disagree about. |
| [`PairConfusionMatrix.ToArray`](pairconfusionmatrix-toarray.md) | The same four counts as a 2×2 array, in scikit-learn's own order. |
