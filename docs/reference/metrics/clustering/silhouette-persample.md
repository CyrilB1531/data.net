# Silhouette.PerSample

The score of each sample, from the samples themselves.

<!-- docs-declaration -->

```csharp
public static double[] PerSample(ReadOnlySpan<int> labels, ReadOnlySpan<double> features, int featureCount)
```

**Parameters** — as `Silhouette.Score`: `labels`, the samples `features` row-major, and
`featureCount`.

**Returns** — `double[]`, one value per sample in the order the samples were given.

**Exceptions** — `ArgumentException` when the inputs disagree in size, and when the number of
distinct labels falls outside `[2, n - 1]` — scikit-learn's own bound, carried with its own
sentence: `Number of labels is 1. Valid values are 2 to n_samples - 1 (inclusive)`.

**Example** — the mean hides which sample is misplaced; this does not.

```csharp
using DataNet.Metrics;

double[] features = [0.0, 0.0, 0.2, 0.1, 4.0, 4.0, 4.2, 3.9, 0.1, 0.3];
int[] labels = [0, 0, 1, 1, 1];

double[] scores = Silhouette.PerSample(labels, features, 2);
double stranger = scores[4];   // => -0.9501…
```

**Remarks** — the negative value is the point. Sample 4 was labelled into the far cluster while
sitting among the near one, and no mean would have told you which sample to look at.

A cluster holding one sample scores that sample `0.0` rather than dividing by zero: there is no
other member to be close to, so it is neither well nor badly placed. That is scikit-learn's answer,
measured.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Silhouette.Score`, `Silhouette.PerSampleFromDistances`, the [Python equivalence table](../../../equivalence.md).
