# DaviesBouldin.Score

The Davies-Bouldin index — `sklearn.metrics.davies_bouldin_score`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<int> labels, ReadOnlySpan<double> features, int featureCount)
```

**Parameters** — `labels` is one cluster label per sample, any integers and not necessarily
contiguous. `features` is the samples row-major: sample `i` occupies `featureCount` values from
`i * featureCount`. `featureCount` is how many values each sample holds.

**Returns** — `double`, `0` or above. **Lower is better**, unlike every other clustering score in
this package. `0` when no cluster has any spread, or when the centroids coincide.

**Exceptions** — `ArgumentException` when `features` is not `labels.Length × featureCount`, or when
the number of distinct labels is outside `[2, n - 1]`, with scikit-learn's own sentence — the same
range and the same message as [`CalinskiHarabasz.Score`](calinskiharabasz-score.md) and
[`Silhouette.Score`](silhouette-score.md). `ArgumentOutOfRangeException` when `featureCount` is not
positive.

**Example** — the same six samples the variance ratio scores, in the same two clusters.

```csharp
using Lodestar.Metrics;

double[] samples = [1.0, 2.0, 1.5, 1.8, 5.0, 8.0, 8.0, 8.0, 1.0, 0.6, 9.0, 11.0];
int[] clusters = [0, 0, 1, 1, 0, 1];

double index = DaviesBouldin.Score(clusters, samples, featureCount: 2);  // => 0.2826…
```

Scattering the samples across three clusters makes this number **rise** where
[`CalinskiHarabasz.Score`](calinskiharabasz-score.md) makes it fall:

```csharp
using Lodestar.Metrics;

double[] samples = [1.0, 2.0, 1.5, 1.8, 5.0, 8.0, 8.0, 8.0, 1.0, 0.6, 9.0, 11.0];
int[] scattered = [0, 1, 2, 0, 1, 2];

double worse = DaviesBouldin.Score(scattered, samples, featureCount: 2);  // => 1.2713…
```

**Remarks** — a pair of clusters whose centroids coincide contributes `0` rather than an infinity:
the reference substitutes infinity for the zero distance before dividing, which drops the pair out
of the maximum. That is why a perfect clustering and a degenerate one can both read `0`.

Euclidean only, and no precomputed-distance form, for the reason
[`CalinskiHarabasz`](calinskiharabasz.md) gives.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CalinskiHarabasz.Score`](calinskiharabasz-score.md),
[`Silhouette.Score`](silhouette-score.md), the [Python equivalence table](../../../equivalence.md).
