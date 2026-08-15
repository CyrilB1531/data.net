# Recall.PerClass

Recall for every class, in label order.

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

**Example** — the triage found every sample of class 1 and two thirds of class 2.

```csharp
using DataNet.Metrics;

int[] yTrue = [0, 0, 1, 1, 2, 2, 2];
int[] yPred = [0, 1, 1, 1, 2, 2, 0];

double[] perClass = Recall.PerClass(yTrue, yPred);
double normal = perClass[1];   // => 1
double spam = perClass[2];     // => 0.6666…
```

**Remarks** — this is the same set of numbers as the diagonal of a `Normalization.True` confusion
matrix, and their unweighted mean is `BalancedAccuracy.Score`. Which of the three shapes you reach
for is a matter of what you are about to do with it: an array to assert on, a matrix to draw, or
one
number to report.

The trap is the denominator on a restricted matrix. This divides by scikit-learn's `true_sum`,
counted over **every observed label** including ones an explicit `labels` subset excluded from the
view, where `BalancedAccuracy.Score`'s `ConfusionMatrix` overload divides by the row sum inside
the
view. The two agree whenever nothing was dropped, and give different numbers when something was.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Recall.Score`, `Precision.PerClass`, `BalancedAccuracy.Score`,
`ConfusionMatrix.ToArray`, the [Python equivalence table](../../../equivalence.md).
