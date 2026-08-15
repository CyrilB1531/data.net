# Precision.PerClass

Precision for every class, in label order.

<!-- docs-declaration -->

```csharp
public static double[] PerClass(ConfusionMatrix cm, ZeroDivision zeroDivision = ZeroDivision.Zero)
public static double[] PerClass(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, ZeroDivision zeroDivision = ZeroDivision.Zero, ReadOnlySpan<int> labels = default, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `cm` is a matrix already counted, or pass `yTrue` and `yPred`. `zeroDivision`
decides an undefined per-class score, `labels` fixes the label set and its order, and
`sampleWeight`
weights the samples.

**Returns** — a fresh `double[]`, one entry per label, in the matrix's label order.

**Exceptions** — `ArgumentNullException` when `cm` is null; `ArgumentException` when the label
spans
disagree in length or are empty.

**Example** — the three-way triage: nothing predicted into class 2 was wrong.

```csharp
using DataNet.Metrics;

int[] yTrue = [0, 0, 1, 1, 2, 2, 2];
int[] yPred = [0, 1, 1, 1, 2, 2, 0];

double[] perClass = Precision.PerClass(yTrue, yPred);
double urgent = perClass[0];   // => 0.5
double spam = perClass[2];     // => 1
```

**Remarks** — scikit-learn's `average=None`, as a method because it returns an array. This is the
first thing to look at when a macro average is low: it is usually one class, and usually the
rarest.

Two traps. The array is positional in the label order, so the score for label `20` is not at index
`20`; and a class nothing was predicted into contributes a `0.0` here by default, which then drags
`Averaging.Macro` down by a full `1/k` even though the model was never asked about it. If that
class
is absent because your evaluation set is small rather than because the model is bad,
`ZeroDivision.NaN` is the honest setting.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Precision.Score`, `Recall.PerClass`, `ClassificationReport.Compute`,
`ZeroDivision`,
the [Python equivalence table](../../../equivalence.md).
