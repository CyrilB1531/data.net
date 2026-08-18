# JaccardScore.PerClass

One coefficient per class, in label order — `jaccard_score(average=None)`.

<!-- docs-declaration -->

```csharp
public static double[] PerClass(ConfusionMatrix cm, ZeroDivision zeroDivision = ZeroDivision.Zero)
public static double[] PerClass(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, ZeroDivision zeroDivision = ZeroDivision.Zero, ReadOnlySpan<int> labels = default, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `cm` is a matrix you already have, or `yTrue` and `yPred` are the labels to count
one from. `zeroDivision` is the answer for a class neither side carries. `labels` fixes the label set
and its order. `sampleWeight` is one weight per sample.

**Returns** — a fresh `double[]`, one entry per class in label order.

**Exceptions** — `ArgumentNullException` when `cm` is null. `ArgumentException` when the inputs
disagree in length or the weights do not match. `UndefinedMetricException` when a class is empty on
both sides and `zeroDivision` is `ZeroDivision.Throw`.

**Example** — the per-class view the averages hide.

```csharp
using Lodestar.Metrics;

int[] truth = [0, 1, 2, 1];
int[] predicted = [0, 2, 2, 1];

double[] perClass = JaccardScore.PerClass(truth, predicted);
double second = perClass[1];  // => 0.5
```

The three classes score `1`, `0.5` and `0.5`, whose plain mean is the `0.6666…`
[`JaccardScore.Score`](jaccardscore-score.md) reports under `Averaging.Macro`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`JaccardScore.Score`](jaccardscore-score.md),
[`Precision.PerClass`](precision-perclass.md), the [Python equivalence table](../../../equivalence.md).
