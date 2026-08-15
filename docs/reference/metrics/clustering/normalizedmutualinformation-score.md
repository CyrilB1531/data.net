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

**Remarks** — the example is the reason this is not the default choice: a clustering that puts
every sample on its own carries no information about the truth, and still scores two thirds here
where `AdjustedRand.Score` scores `0`.

The normalizer is the arithmetic mean of the two entropies, scikit-learn's default
`average_method`; the other three it offers are not reproduced. That choice is also what makes
this equal to `VMeasure.Score` on every input.

**Applies to** — net10.0, netstandard2.0.

**See also** — `AdjustedRand.Score`, `VMeasure.Score`, the [Python equivalence table](../../../equivalence.md).
