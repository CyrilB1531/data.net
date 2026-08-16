# AdjustedRand.Score

How many pairs of samples the two partitions agree about, minus what chance would give.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
```

**Parameters** — `labelsTrue` is the reference partition and `labelsPred` the one being
scored, one label per sample and the same length. The label *values* carry no meaning: only which
samples share one does.

**Returns** — `double` in `[-0.5, 1]`. `1` is the same partition however it is named, `0` is what a random
labelling scores, and a negative number is agreement *worse* than chance.

**Exceptions** — `ArgumentException` when the two labellings disagree in length. An empty
input is not an error: it scores `1`.

**Example** — a clustering that got one sample wrong, against one that split everything.

```csharp
using DataNet.Metrics;

int[] truth = [0, 0, 1, 1, 2, 2];
int[] almost = [0, 0, 1, 2, 2, 2];
int[] alone = [0, 1, 2, 3, 4, 5];

double good = AdjustedRand.Score(truth, almost);   // => 0.4444…
double useless = AdjustedRand.Score(truth, alone);   // => 0
```

**Remarks** — the correction for chance is a subtraction, so this can and does go negative:
`[0,0,1,1]` against `[0,1,0,1]` scores `-0.5`, measured. Read that as *systematically* disagreeing
rather than as an error.

An empty input scores `1`, as does a single sample, and so does one cluster covering everything
when the truth is also one cluster — all three are the same partition, and scikit-learn answers
them before computing anything. Against a two-cluster truth, one cluster scores `0` instead.

**Applies to** — net10.0, netstandard2.0.

**See also** — `VMeasure.Score`, `NormalizedMutualInformation.Score`, the [Python equivalence table](../../../equivalence.md).
