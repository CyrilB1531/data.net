# VMeasure.Score

Homogeneity and completeness as one number, their harmonic mean.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<int> labelsTrue, ReadOnlySpan<int> labelsPred)
```

**Parameters** — `labelsTrue` is the reference partition and `labelsPred` the one being
scored, one label per sample and the same length. The label *values* carry no meaning: only which
samples share one does.

**Returns** — `double` in `[0, 1]`, `0` when homogeneity and completeness are both `0`.

**Exceptions** — `ArgumentException` when the two labellings disagree in length. An empty
input is not an error: it scores `1`.

**Example** — one number for a clustering that splits one class and merges nothing.

```csharp
using Lodestar.Metrics;

int[] truth = [0, 0, 0, 1, 1, 1];
int[] split = [0, 0, 1, 2, 2, 2];

double score = VMeasure.Score(truth, split);   // => 0.8132…
```

**Remarks** — this returns exactly what `NormalizedMutualInformation.Score` returns, on every
input. That is not a coincidence and it is worth deriving once, because a reader who reports both
numbers thinks they have two: homogeneity is `MI / H(true)` and completeness is `MI / H(pred)`, so
their harmonic mean `2hc / (h + c)` cancels down to `2·MI / (H(true) + H(pred))` — mutual
information divided by the arithmetic mean of the two entropies, which is precisely the normalizer
`normalized_mutual_info_score` applies by default. Two names, one quantity.

The number that *does* say something different is `AdjustedRand.Score`, and the gap between the two
is the correction for chance: a clustering that invents clusters scores well here and near zero
there.

scikit-learn's `beta`, which would weigh homogeneity against completeness rather than averaging them
evenly, is not a parameter here: its default of `1` is the plain harmonic mean above, and no oracle
row exists for any other value.

**Applies to** — net10.0, netstandard2.0.

**See also** — `AdjustedRand.Score`, `Homogeneity.Score`, `Completeness.Score`, the [Python equivalence table](../../../equivalence.md).
