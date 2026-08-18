# JaccardScore.Score

The Jaccard similarity coefficient — `sklearn.metrics.jaccard_score`.

<!-- docs-declaration -->

```csharp
public static double Score(ConfusionMatrix cm, Averaging average = Averaging.Binary, int posLabel = 1, ZeroDivision zeroDivision = ZeroDivision.Zero)
public static double Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, Averaging average = Averaging.Binary, int posLabel = 1, ZeroDivision zeroDivision = ZeroDivision.Zero, ReadOnlySpan<int> labels = default, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `cm` is a matrix you already have, or `yTrue` and `yPred` are the labels to count
one from. `average` decides how the per-class coefficients are reduced. `posLabel` is the class
reported under `Averaging.Binary`. `zeroDivision` is the answer for a class neither side carries.
`labels` fixes the label set and its order; omit it for the sorted union of both inputs.
`sampleWeight` is one weight per sample.

**Returns** — `double` in `[0, 1]`, never above [`Precision.Score`](precision-score.md) or
[`Recall.Score`](recall-score.md) on the same class.

**Exceptions** — `ArgumentNullException` when `cm` is null. `ArgumentException` when the inputs
disagree in length or the weights do not match — and when `average` is `Averaging.Binary` and
`posLabel` occurs in neither input, which is the refusal `Precision.Score` already makes.
`UndefinedMetricException` when a class is empty on both sides and `zeroDivision` is
`ZeroDivision.Throw`.

**Example** — four samples over three classes.

```csharp
using Lodestar.Metrics;

int[] truth = [0, 1, 2, 1];
int[] predicted = [0, 2, 2, 1];

double macro = JaccardScore.Score(truth, predicted, Averaging.Macro);  // => 0.6666…
```

`Averaging.Micro` gives `0.6` and `Averaging.Weighted` `0.625` on the same input — the three
disagree because one class is scored perfectly and two are half right.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`JaccardScore.PerClass`](jaccardscore-perclass.md),
[`Precision.Score`](precision-score.md), [`Recall.Score`](recall-score.md), the
[Python equivalence table](../../../equivalence.md).
