# Silhouette.ScoreFromDistances

The mean over every sample, from a distance matrix you already have.

<!-- docs-declaration -->

```csharp
public static double ScoreFromDistances(ReadOnlySpan<int> labels, ReadOnlySpan<double> distances)
```

**Parameters** — `labels` gives each sample its cluster. `distances` is the `n × n` matrix
row-major, so the distance from sample `i` to sample `j` is `distances[(i * n) + j]`.

**Returns** — `double` in `[-1, 1]`, the same number `Silhouette.Score` gives for the euclidean matrix of
the same samples.

**Exceptions** — `ArgumentException` when the inputs disagree in size, and when the number of
distinct labels falls outside `[2, n - 1]` — scikit-learn's own bound, carried with its own
sentence: `Number of labels is 1. Valid values are 2 to n_samples - 1 (inclusive)`.

**Example** — the matrix of the two clusters above, passed in directly.

```csharp
using Lodestar.Metrics;

double[] distances =
[
    0.0000, 0.1414, 7.0711, 7.2242, 7.0007,
    0.1414, 0.0000, 6.9296, 7.0824, 6.8593,
    7.0711, 6.9296, 0.0000, 0.2236, 0.1000,
    7.2242, 7.0824, 0.2236, 0.0000, 0.3162,
    7.0007, 6.8593, 0.1000, 0.3162, 0.0000,
];
int[] labels = [0, 0, 1, 1, 1];

double score = Silhouette.ScoreFromDistances(labels, distances);   // => 0.9737…
```

**Remarks** — the example's matrix is rounded to four decimals, which is why it scores
`0.9737…` where `Silhouette.Score` on the same samples scores `0.9738…`. The difference is the
rounding, not the method.

A name of its own rather than an overload of `Silhouette.Score`, because a distance
matrix and a block of features are both a span of `double` and the two signatures would collide.
That is [decision 0021](../../../decisions/0021-multioutput-is-a-method-not-an-enum.md)'s ruling
applied to an input rather than to a return type.

Nothing checks that the matrix is a metric — symmetric, zero on the diagonal, positive elsewhere.
scikit-learn does not either, and a caller who passes a similarity by mistake gets a number rather
than an exception.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Silhouette.Score`, `Silhouette.PerSampleFromDistances`, the [Python equivalence table](../../../equivalence.md).
