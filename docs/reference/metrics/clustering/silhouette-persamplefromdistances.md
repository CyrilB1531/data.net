# Silhouette.PerSampleFromDistances

The score of each sample, from a distance matrix.

<!-- docs-declaration -->

```csharp
public static double[] PerSampleFromDistances(ReadOnlySpan<int> labels, ReadOnlySpan<double> distances)
```

**Parameters** — as `Silhouette.ScoreFromDistances`: `labels`, and the `n × n` matrix
`distances` row-major.

**Returns** — `double[]`, one value per sample in the order the samples were given.

**Exceptions** — `ArgumentException` when the inputs disagree in size, and when the number of
distinct labels falls outside `[2, n - 1]` — scikit-learn's own bound, carried with its own
sentence: `Number of labels is 1. Valid values are 2 to n_samples - 1 (inclusive)`.

**Example** — the same per-sample diagnosis, on a matrix.

```csharp
using Lodestar.Metrics;

double[] distances =
[
    0.0, 1.0, 9.0,
    1.0, 0.0, 9.0,
    9.0, 9.0, 0.0,
];
int[] labels = [0, 0, 1];

double[] scores = Silhouette.PerSampleFromDistances(labels, distances);
double alone = scores[2];   // => 0
```

**Remarks** — the third sample is a cluster of one, so it scores `0`, and the other two score
high. This is the computation the three other members are written in terms of: `Silhouette.Score`
and `Silhouette.PerSample` build the euclidean matrix and call it.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Silhouette.ScoreFromDistances`, `Silhouette.PerSample`, the [Python equivalence table](../../../equivalence.md).
