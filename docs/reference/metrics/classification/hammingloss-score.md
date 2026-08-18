# HammingLoss.Score

The fraction of labels predicted wrongly — `sklearn.metrics.hamming_loss`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, ReadOnlySpan<double> sampleWeight = default)
public static double Score(ReadOnlySpan<bool> yTrue, ReadOnlySpan<bool> yPred, int labelCount, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — the first overload takes `yTrue` and `yPred` as one label per sample. The second
takes them as a label matrix: one boolean per label per sample, row-major, `labelCount` values per
row. `sampleWeight` is one weight per **sample** — per row, not per label — or empty, the default.

**Returns** — `double` in `[0, 1]`. `0` when everything is right.

**Exceptions** — `ArgumentException` when the inputs disagree in length, are empty, when the matrix
is not a whole number of rows of `labelCount`, or when the weights do not match the sample count.

**Example** — four samples over three classes, one wrong.

```csharp
using Lodestar.Metrics;

int[] truth = [0, 1, 2, 1];
int[] predicted = [0, 2, 2, 1];

double loss = HammingLoss.Score(truth, predicted);  // => 0.25
```

Over a matrix it counts labels rather than samples, which is where it and
[`ZeroOneLoss.Score`](zerooneloss-score.md) differ:

```csharp
using Lodestar.Metrics;

bool[] truth = [true, false, true, false, true, true];
bool[] predicted = [true, false, false, true, true, true];

double loss = HammingLoss.Score(truth, predicted, labelCount: 3);  // => 0.3333…
```

Two of the six labels are wrong. `ZeroOneLoss.Score` reads `1` on the same input, because both rows
carry a mistake.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`ZeroOneLoss.Score`](zerooneloss-score.md), [`Accuracy.Score`](accuracy-score.md),
the [Python equivalence table](../../../equivalence.md).
