# FowlkesMallows

Agreement between two partitions as the geometric mean of pair precision and pair recall.

<!-- docs-declaration -->

```csharp
public static class FowlkesMallows
```

**Example** — the same partition under different names.

```csharp
using Lodestar.Metrics;

double same = FowlkesMallows.Score([0, 0, 1, 1], [2, 2, 0, 0]);  // => 1
```

**Remarks** — counts **pairs of samples**, like [`AdjustedRand`](adjustedrand.md), and unlike it
applies no correction for chance. Two independent partitions therefore score above zero here,
which makes this the wrong measure for comparing clusterings with different numbers of clusters
and a reasonable one for comparing two at the same size.

Reference behaviour is `sklearn.metrics.fowlkes_mallows_score`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`AdjustedRand`](adjustedrand.md),
[`AdjustedMutualInformation`](adjustedmutualinformation.md), [the clustering index](../clustering.md).

## Members

| Member | What it does |
| --- | --- |
| [`FowlkesMallows.Score`](fowlkesmallows-score.md) | The geometric mean of pair precision and pair recall. |
