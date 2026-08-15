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
using DataNet.Metrics;

int[] truth = [0, 0, 0, 1, 1, 1];
int[] split = [0, 0, 1, 2, 2, 2];

double score = VMeasure.Score(truth, split);   // => 0.8132…
```

**Remarks** — scikit-learn's `beta`, which weighs homogeneity against completeness, is not
reproduced: its default of `1` is the harmonic mean, and no oracle row exists for another value.

This equals `NormalizedMutualInformation.Score` on every input, which is not a coincidence but the
arithmetic-mean normalizer written twice. When they disagree with `AdjustedRand.Score`, the gap is
the correction for chance.

**Applies to** — net10.0, netstandard2.0.

**See also** — `AdjustedRand.Score`, `Homogeneity.Score`, `Completeness.Score`, the [Python equivalence table](../../../equivalence.md).
