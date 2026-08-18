# RandIndex.Score

How many pairs of samples the two partitions treat the same way, over all pairs.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
```

**Parameters** — `labelsTrue` is the reference partition and `labelsPred` the one being
scored, one label per sample and the same length. The label *values* carry no meaning: only which
samples share one does.

**Returns** — `double` in `[0, 1]`. `1` when the two labellings agree on every pair, whichever
way; `0` only when they agree on none.

**Exceptions** — `ArgumentException` when the two labellings disagree in length. An empty
input is not an error: it scores `1`.

**Example** — this is `AdjustedRand.Score` before the correction, and the gap between the two is
the correction made legible.

```csharp
using Lodestar.Metrics;

int[] truth = [0, 0, 0, 1, 1, 1];
int[] split = [0, 0, 1, 2, 2, 2];

double rand = RandIndex.Score(truth, split);            // => 0.8666666666666667
double adjusted = AdjustedRand.Score(truth, split);     // => 0.7058823529411765
```

**Remarks** — `0.867` against `0.706` is the pair of numbers that makes the correction concrete:
most pairs already agree by construction on a small sample, so an *uncorrected* score stays high
even for a clustering that is only partly right. `AdjustedRand.Score` subtracts what agreement by
chance alone would already give, which is why it reads lower on the same input.

**Because it is uncorrected, two independent labellings score well above zero here.**
`[0,0,1,1]` against `[0,1,0,1]` scores `0.333` under this metric and `-0.5` under
`AdjustedRand.Score` — the same disagreement, read two different ways. That makes this the wrong
choice for comparing clusterings that use different numbers of clusters, where chance agreement
itself varies with cluster count.

An empty input and a single sample both score `1`, as they do for every other metric in this
namespace: agreeing about nothing is agreeing.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`AdjustedRand.Score`](adjustedrand-score.md),
[`PairConfusionMatrix.Compute`](pairconfusionmatrix-compute.md),
[the Python equivalence table](../../../equivalence.md).
