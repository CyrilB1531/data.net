# F1.PerClass

F1 for every class, in label order.

<!-- docs-declaration -->

```csharp
public static double[] PerClass(ConfusionMatrix cm, ZeroDivision zeroDivision = ZeroDivision.Zero)
public static double[] PerClass(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, ZeroDivision zeroDivision = ZeroDivision.Zero, ReadOnlySpan<int> labels = default, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `cm` is a matrix already counted, or pass `yTrue` and `yPred`. `zeroDivision`
decides what an undefined per-class score becomes. `labels` fixes the label set and its order, and
`sampleWeight` weights the samples.

**Returns** — a fresh `double[]`, one entry per label, in the same order as the matrix's `Labels`.

**Exceptions** — `ArgumentNullException` when `cm` is null; `ArgumentException` when the label
spans
disagree in length or are empty.

**Example** — both classes of the spam filter at once.

```csharp
using DataNet.Metrics;

int[] yTrue = [1, 1, 1, 1, 0, 0, 0, 0];
int[] yPred = [1, 1, 0, 0, 1, 0, 0, 0];

double[] perClass = F1.PerClass(yTrue, yPred);
double ham = perClass[0];    // => 0.6666…
double spam = perClass[1];   // => 0.5714…
```

**Remarks** — this is scikit-learn's `average=None`, and it is a separate method rather than an
`Averaging` member because it returns an array where the others return a scalar; an enum cannot
change a return type. Reach for it when you want to see which class is dragging a macro average
down, which is the first question a bad macro score raises.

The trap is index versus label. The array is positional in the matrix's label order, so on labels
`[10, 20, 30]` the score for class `20` is at index `1`, not at index `20`. Read `cm.Labels[i]`,
or
use `ClassificationReport.Compute`, whose `ClassRow` carries the label with the score.

If you want all three of precision, recall and F1 per class, `ClassificationReport.Compute`
computes
them in one pass over one matrix instead of three.

**Applies to** — net10.0, netstandard2.0.

**See also** — `F1.Score`, `Precision.PerClass`, `Recall.PerClass`,
`ClassificationReport.Compute`,
the [Python equivalence table](../../../equivalence.md).
