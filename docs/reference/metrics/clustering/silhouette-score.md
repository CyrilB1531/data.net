# Silhouette.Score

The mean over every sample, from the samples themselves.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<int> labels, ReadOnlySpan<double> features, int featureCount)
```

**Parameters** — `labels` gives each sample its cluster. `features` holds the samples
row-major: sample `i` occupies `featureCount` values starting at `i * featureCount`.
`featureCount` is how many values each sample holds.

**Returns** — `double` in `[-1, 1]`. Near `1` the clusters are well separated, near `0` they touch, and
below `0` the samples are mostly closer to another cluster than to their own.

**Exceptions** — `ArgumentException` when the inputs disagree in size, and when the number of
distinct labels falls outside `[2, n - 1]` — scikit-learn's own bound, carried with its own
sentence: `Number of labels is 1. Valid values are 2 to n_samples - 1 (inclusive)`.

**Example** — two clusters that are genuinely apart.

```csharp
using DataNet.Metrics;

double[] features = [0.0, 0.0, 0.1, 0.1, 5.0, 5.0, 5.1, 5.2, 5.0, 4.9];
int[] labels = [0, 0, 1, 1, 1];

double score = Silhouette.Score(labels, features, 2);   // => 0.9738…
```

**Remarks** — euclidean only. scikit-learn accepts some twenty `metric=` names; each one admitted
here would be a parity claim to prove and keep, so a caller who wants another computes the matrix
and passes it to `Silhouette.ScoreFromDistances`.

This is `O(n²)` in both time and memory: it builds the whole distance matrix and then reads it, so
it allocates `n²` doubles of its own. `ScoreFromDistances` costs the same `n²`, held by the caller
instead — which is what to use when you already have the matrix, not a way to avoid paying for it.
Neither runs at 100 000 samples, where the matrix is 80 GB; past about 46 000 this refuses outright,
because the buffer no longer fits an array.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Silhouette.PerSample`, `Silhouette.ScoreFromDistances`, the [Python equivalence table](../../../equivalence.md).
