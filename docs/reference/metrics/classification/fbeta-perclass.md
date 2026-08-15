# FBeta.PerClass

F-beta for every class, in label order.

<!-- docs-declaration -->

```csharp
public static double[] PerClass(ConfusionMatrix cm, double beta, ZeroDivision zeroDivision = ZeroDivision.Zero)
public static double[] PerClass(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, double beta, ZeroDivision zeroDivision = ZeroDivision.Zero, ReadOnlySpan<int> labels = default, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `cm` is a matrix already counted, or pass `yTrue` and `yPred`. `beta` is the
weight
of recall relative to precision. `zeroDivision` decides an undefined per-class score, `labels`
fixes
the label set and its order, and `sampleWeight` weights the samples.

**Returns** — a fresh `double[]`, one entry per label, in the matrix's label order.

**Exceptions** — `ArgumentOutOfRangeException` when `beta` is negative, `NaN` or infinite;
`ArgumentNullException` when `cm` is null; `ArgumentException` when the label spans disagree in
length or are empty.

**Example** — both classes at `beta = 2`.

```csharp
using DataNet.Metrics;

int[] yTrue = [1, 1, 1, 1, 0, 0, 0, 0];
int[] yPred = [1, 1, 0, 0, 1, 0, 0, 0];

double[] perClass = FBeta.PerClass(yTrue, yPred, 2.0);
double ham = perClass[0];    // => 0.7142…
double spam = perClass[1];   // => 0.5263…
```

**Remarks** — the per-class form exists for the same reason `F1.PerClass` does: to see which class
a
macro average is hiding. It is worth a moment's thought before using it, though, because `beta`
weights recall over precision **for every class at once**, and the asymmetry that justified `beta`
was usually about one class in particular.

The trap is the arithmetic behind the scenes rather than in the result. `beta` is applied by
substituting the true positives, the predicted count and the support algebraically rather than by
computing precision and recall and combining them, which is what keeps the answer exact at the
edges where one of the two is undefined —
[decision
0032](../../../decisions/0032-fbeta-substitutes-tp-predicted-and-support-algebraically.md) has
the derivation. Nothing about the call changes; it is the reason the undefined cases here agree
with
scikit-learn rather than approximately agreeing.

**Applies to** — net10.0, netstandard2.0.

**See also** — `FBeta.Score`, `F1.PerClass`, `ClassificationReport.Compute`,
[decision
0032](../../../decisions/0032-fbeta-substitutes-tp-predicted-and-support-algebraically.md),
the [Python equivalence table](../../../equivalence.md).
