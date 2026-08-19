# MultilabelConfusionMatrix.Compute

One 2×2 matrix per label or per sample — `sklearn.metrics.multilabel_confusion_matrix`.

<!-- docs-declaration -->

```csharp
public static ConfusionMatrix[] Compute(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, ReadOnlySpan<int> labels = default, ReadOnlySpan<double> sampleWeight = default)
public static ConfusionMatrix[] Compute(ReadOnlySpan<bool> yTrue, ReadOnlySpan<bool> yPred, int labelCount, bool samplewise = false, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — the first overload takes `yTrue` and `yPred` as one label per sample and reports one
matrix per class, with `labels` fixing which classes and in what order; omit it for the sorted union
of both inputs. The second takes them as a row-major label matrix of `labelCount` values per row, and
`samplewise` decides whether to count one matrix per label or one per row. `sampleWeight` is one
weight per **sample** — per row, not per label.

**Returns** — a fresh `ConfusionMatrix[]`: one per class in label order, one per label in column
order, or one per sample in row order.

**Exceptions** — `ArgumentException` when the inputs disagree in length, are empty, when the matrix
is not a whole number of rows of `labelCount`, or when the weights do not match the sample count.

**Example** — three labels over two samples.

```csharp
using Lodestar.Metrics;

bool[] truth = [true, false, true, false, true, true];
bool[] predicted = [true, false, false, true, true, true];

ConfusionMatrix[] perLabel = MultilabelConfusionMatrix.Compute(truth, predicted, labelCount: 3);
int matrices = perLabel.Length;  // => 3
```

Counting the same input by sample instead returns one matrix per row:

```csharp
using Lodestar.Metrics;

bool[] truth = [true, false, true, false, true, true];
bool[] predicted = [true, false, false, true, true, true];

ConfusionMatrix[] perSample = MultilabelConfusionMatrix.Compute(truth, predicted, 3, samplewise: true);
int matrices = perSample.Length;  // => 2
```

**Remarks** — each entry is an ordinary [`ConfusionMatrix`](confusionmatrix.md) over labels `0` and
`1`, so [`Recall.Score`](recall-score.md), [`Precision.Score`](precision-score.md) and the rest read
it directly. Under `samplewise` a row's weight applies to each of that row's labels, because the
matrix counts labels there rather than samples.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`ConfusionMatrix.Compute`](confusionmatrix-compute.md),
[`Precision.PerClass`](precision-perclass.md), the [Python equivalence table](../../../equivalence.md).
