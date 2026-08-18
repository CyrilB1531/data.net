# CalinskiHarabasz.Score

The variance ratio criterion — `sklearn.metrics.calinski_harabasz_score`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<int> labels, ReadOnlySpan<double> features, int featureCount)
```

**Parameters** — `labels` is one cluster label per sample, any integers and not necessarily
contiguous. `features` is the samples row-major: sample `i` occupies `featureCount` values from
`i * featureCount`. `featureCount` is how many values each sample holds.

**Returns** — `double`, at least `0` and unbounded above; higher means the clusters are further
apart relative to their own spread. `1` when the clusters have no spread at all, which the reference
answers rather than dividing by zero.

**Exceptions** — `ArgumentException` when `features` is not `labels.Length × featureCount`, or when
the number of distinct labels is outside `[2, n - 1]` — one cluster leaves nothing to compare
against and one cluster per sample leaves nothing inside one. The message is scikit-learn's own,
"Number of labels is k. Valid values are 2 to n_samples - 1 (inclusive)", and
[`DaviesBouldin.Score`](daviesbouldin-score.md) and [`Silhouette.Score`](silhouette-score.md) refuse
the same range with the same sentence. `ArgumentOutOfRangeException` when `featureCount` is not
positive.

**Example** — six samples in two dimensions, split into two clusters.

```csharp
using Lodestar.Metrics;

double[] samples = [1.0, 2.0, 1.5, 1.8, 5.0, 8.0, 8.0, 8.0, 1.0, 0.6, 9.0, 11.0];
int[] clusters = [0, 0, 1, 1, 0, 1];

double ratio = CalinskiHarabasz.Score(clusters, samples, featureCount: 2);  // => 35.5865…
```

Splitting the same samples every which way scores far lower, which is the comparison the number is
for:

```csharp
using Lodestar.Metrics;

double[] samples = [1.0, 2.0, 1.5, 1.8, 5.0, 8.0, 8.0, 8.0, 1.0, 0.6, 9.0, 11.0];
int[] scattered = [0, 1, 2, 0, 1, 2];

double worse = CalinskiHarabasz.Score(scattered, samples, featureCount: 2);  // => 2.7478…
```

**Remarks** — euclidean only, as the reference is here: `calinski_harabasz_score` takes no `metric`
at all, so unlike [`Silhouette.Score`](silhouette-score.md) there is nothing to leave out. There is
no precomputed-distance form either, because the score reads cluster centroids and a distance matrix
does not carry them.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`DaviesBouldin.Score`](daviesbouldin-score.md), [`Silhouette.Score`](silhouette-score.md),
the [Python equivalence table](../../../equivalence.md).
