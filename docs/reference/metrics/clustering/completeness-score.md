# Completeness.Score

Whether every sample of one class landed in the same cluster.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
```

**Parameters** — `labelsTrue` is the reference partition and `labelsPred` the one being
scored, one label per sample and the same length. The label *values* carry no meaning: only which
samples share one does.

**Returns** — `double` in `[0, 1]`. `1` when no class is split across clusters, `0` when the clustering says
nothing about the classes.

**Exceptions** — `ArgumentException` when the two labellings disagree in length. An empty
input is not an error: it scores `1`.

**Example** — merging two classes into one cluster keeps each class together, and costs nothing
here.

```csharp
using Lodestar.Metrics;

int[] truth = [0, 0, 1, 1];
int[] merged = [0, 0, 0, 0];

double whole = Completeness.Score(truth, merged);   // => 1
```

**Remarks** — the mirror of `Homogeneity.Score`, and literally so: this is that score with the
two labellings exchanged. The example scores `1` here and `0` there, which is why neither number
means anything on its own.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Homogeneity.Score`, `VMeasure.Score`, the [Python equivalence table](../../../equivalence.md).
