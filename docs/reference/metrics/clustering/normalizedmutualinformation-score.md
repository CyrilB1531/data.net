# NormalizedMutualInformation.Score

How much knowing one labelling tells you about the other, scaled into `[0, 1]`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
```

**Parameters** — `labelsTrue` is the reference partition and `labelsPred` the one being
scored, one label per sample and the same length. The label *values* carry no meaning: only which
samples share one does.

**Returns** — `double` in `[0, 1]`. `1` when each labelling determines the other, `0` when neither says
anything about the other.

**Exceptions** — `ArgumentException` when the two labellings disagree in length. An empty
input is not an error: it scores `1`.

**Example** — the same two clusterings, scored without the correction for chance.

```csharp
using DataNet.Metrics;

int[] truth = [0, 0, 1, 1];
int[] alone = [0, 1, 2, 3];

double score = NormalizedMutualInformation.Score(truth, alone);   // => 0.6666…
```

**Remarks** — the example is the reason this is not the default choice, and the reason is *not*
that the split clustering says nothing. It says a great deal: every cluster holds one sample, so
knowing the cluster tells you the class exactly, and the mutual information is genuinely high. What
it does not do is beat chance — a partition that fine agrees with any truth about that well by
accident, which is what `AdjustedRand.Score` subtracts and this does not. Read the two together and
the gap between them *is* the correction for chance.

The normalizer is the arithmetic mean of the two entropies, scikit-learn's default
`average_method`; `min`, `geometric` and `max` are absent rather than refused, because the frozen
corpus holds no row for them and an unproven normalizer is not parity. That same choice makes this
the identical number to `VMeasure.Score` on every input — the cancellation is written out in
[that entry](vmeasure-score.md), and it is worth reading before reporting both.

**Applies to** — net10.0, netstandard2.0.

**See also** — `AdjustedRand.Score`, `VMeasure.Score`, the [Python equivalence table](../../../equivalence.md).
