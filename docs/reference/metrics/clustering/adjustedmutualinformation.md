# AdjustedMutualInformation

Shared information between two labellings, corrected for what chance alone would produce.

<!-- docs-declaration -->

```csharp
public static class AdjustedMutualInformation
```

**Example** — the same partition under different names.

```csharp
using Lodestar.Metrics;

double same = AdjustedMutualInformation.Score([0, 0, 1, 1], [2, 2, 0, 0]);  // => 1
```

**Remarks** — [`NormalizedMutualInformation`](normalizedmutualinformation.md) with the chance
correction [`AdjustedRand`](adjustedrand.md) applies to pair counts. That correction is what makes
it safe to compare clusterings with **different numbers of clusters**: splitting a labelling
further raises the raw mutual information, and raises the expected one with it.

Reference behaviour is `sklearn.metrics.adjusted_mutual_info_score`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`NormalizedMutualInformation`](normalizedmutualinformation.md),
[`FowlkesMallows`](fowlkesmallows.md), [the clustering index](../clustering.md).

## Members

| Member | What it does |
| --- | --- |
| [`AdjustedMutualInformation.Score`](adjustedmutualinformation-score.md) | Mutual information, corrected for chance. |
