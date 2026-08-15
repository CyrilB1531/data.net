# ClassificationReport.Compute

Builds the report: one row per class, the accuracy, and two or three averaged rows.

<!-- docs-declaration -->

```csharp
public static ClassificationReport Compute(ConfusionMatrix cm, IReadOnlyList<string> targetNames = null, ZeroDivision zeroDivision = ZeroDivision.Zero)
public static ClassificationReport Compute(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, IReadOnlyList<string> targetNames = null, ZeroDivision zeroDivision = ZeroDivision.Zero, ReadOnlySpan<int> labels = default, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `cm` is a matrix already counted, or pass `yTrue` and `yPred` instead.
`targetNames`
puts readable names on the rows, one per label and in label order; leave it null and the rows are
named by the label value. `zeroDivision` decides what an undefined per-class score becomes.
`labels`
fixes the label set and its order, and `sampleWeight` gives each sample its own weight.

**Returns** — a `ClassificationReport`, whose `Classes` list holds one `ClassRow` per label in the
matrix's label order, and whose `MacroAverage`, `WeightedAverage` and possibly `MicroAverage` hold
the averaged rows. `Accuracy` and `TotalSupport` are on the report itself.

**Exceptions** — `ArgumentNullException` when `cm` is null; `ArgumentException` when `targetNames`
has a different length from the label set, or the label spans disagree in length or are empty.

**Example** — a three-way triage, with names on the classes.

```csharp
using DataNet.Metrics;

int[] yTrue = [0, 0, 1, 1, 2, 2, 2];
int[] yPred = [0, 1, 1, 1, 2, 2, 0];

ClassificationReport report = ClassificationReport.Compute(yTrue, yPred, ["urgent", "normal", "spam"]);
double spamF1 = report.Classes[2].F1;   // => 0.8
double accuracy = report.Accuracy;      // => 0.7142…
```

**Remarks** — this is the thing to reach for when you are looking rather than monitoring. One call
gives every per-class score at once, so it replaces four calls to `Precision.PerClass` and its
siblings and counts the matrix once instead of four times. `ToText` then renders it exactly as
Python prints it, which makes a C# result and a Python result comparable by eye rather than by
transcription.

`MicroAverage` is `null` almost always, and that is the interesting part. scikit-learn prints an
`accuracy` row normally and swaps in a `micro avg` row when an explicit label subset has left some
samples out — because then the diagonal over the total is no longer accuracy over the dataset.
This
reproduces that rule exactly: the property is non-null precisely when `labels` was given **and**
something fell outside it.

The trap is `targetNames`: it is positional, matched to the label set by index and not by value.
If
`labels` is omitted the order is the sorted union of both inputs, so names written in the order
the
classes occur in your data will be silently attached to the wrong rows. Pass `labels` explicitly
whenever you pass `targetNames`, or sort the names yourself.

**Applies to** — net10.0, netstandard2.0.

**See also** — `ClassificationReport.ToText`, `ConfusionMatrix.Compute`, `Precision.PerClass`,
the [Python equivalence table](../../../equivalence.md).
