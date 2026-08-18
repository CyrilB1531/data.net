# PairConfusionMatrix.Compute

Counts the pairs two labellings agree and disagree about.

<!-- docs-declaration -->

```csharp
public static PairConfusionMatrix Compute(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
```

**Parameters** — `labelsTrue` is the reference labelling and `labelsPred` the one being scored,
one label per sample and the same length. The label *values* carry no meaning: only which samples
share one does.

**Returns** — [`PairConfusionMatrix`](pairconfusionmatrix.md), four `long` counts that sum to the
square of the sample count.

**Exceptions** — `ArgumentException` when the two labellings disagree in length. An empty input
is not an error: every count is `0`.

**Example** — verified against the two worked examples in scikit-learn's own docstring.

```csharp
using Lodestar.Metrics;

PairConfusionMatrix perfect = PairConfusionMatrix.Compute([0, 0, 1, 1], [1, 1, 0, 0]);
long same = perfect.SameInBoth;   // => 4

PairConfusionMatrix penalised = PairConfusionMatrix.Compute([0, 0, 1, 2], [0, 0, 1, 1]);
long overMerged = penalised.SameInPredictedOnly;   // => 2
```

**Remarks** — the first example is a renaming: `[1,1,0,0]` is `[0,0,1,1]` with the two labels
swapped, so every pair that is together in one labelling is together in the other, and every pair
apart in one is apart in the other — `DifferentInBoth` is `8`, `SameInBoth` is `4`, and the other
two quadrants are `0`. Renaming a cluster changes nothing this type counts, which is exactly what
[`RandIndex.Score`](randindex-score.md) needs to stay `1` on it.

The second is the penalised case scikit-learn's own documentation uses: the prediction merges two
of the truth's clusters into one. `SameInPredictedOnly` is `2` — two pairs the prediction now
calls together that the truth kept apart — where a clean split would have left it `0`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`PairConfusionMatrix`](pairconfusionmatrix.md),
[`PairConfusionMatrix.ToArray`](pairconfusionmatrix-toarray.md),
[`RandIndex.Score`](randindex-score.md).
