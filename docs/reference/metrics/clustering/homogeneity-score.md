# Homogeneity.Score

Whether each cluster holds samples of a single class.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
```

**Parameters** — `labelsTrue` is the reference partition and `labelsPred` the one being
scored, one label per sample and the same length. The label *values* carry no meaning: only which
samples share one does.

**Returns** — `double` in `[0, 1]`. `1` when every cluster is pure, `0` when the clustering says nothing
about the classes.

**Exceptions** — `ArgumentException` when the two labellings disagree in length. An empty
input is not an error: it scores `1`.

**Example** — splitting a class in two keeps every cluster pure, and costs nothing here.

```csharp
using Lodestar.Metrics;

int[] truth = [0, 0, 0, 1, 1, 1];
int[] split = [0, 0, 1, 2, 2, 2];

double pure = Homogeneity.Score(truth, split);   // => 1
```

**Remarks** — the example scores `1` while `Completeness.Score` on the same input scores less,
and that asymmetry is the point of having both. This number answers "is any cluster mixed?" and
nothing else; a clustering that splits every sample into its own cluster is perfectly homogeneous.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Completeness.Score`, `VMeasure.Score`, the [Python equivalence table](../../../equivalence.md).
