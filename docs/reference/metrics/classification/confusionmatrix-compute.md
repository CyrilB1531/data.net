# ConfusionMatrix.Compute

Counts the samples into a table whose rows are true labels and whose columns are predicted ones.

<!-- docs-declaration -->

```csharp
public static ConfusionMatrix Compute(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, ReadOnlySpan<int> labels = default, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted labels, one per sample and the
same
length. `labels` fixes which labels get a row and a column, and in what order; omit it for the
sorted union of both inputs. `sampleWeight` gives each sample its own weight, so a cell holds a
weight rather than a count.

**Returns** — a `ConfusionMatrix`, whose `Labels` gives the row and column order, whose indexer
reads a cell, and whose `TotalWeight` is what it counted.

**Exceptions** — `ArgumentException` when the spans disagree in length, are empty, contain
duplicate
labels, or no supplied label occurs in `yTrue`.

**Example** — the four cells of the spam filter, read by index.

```csharp
using DataNet.Metrics;

int[] yTrue = [1, 1, 1, 1, 0, 0, 0, 0];
int[] yPred = [1, 1, 0, 0, 1, 0, 0, 0];

ConfusionMatrix cm = ConfusionMatrix.Compute(yTrue, yPred);
double missed = cm[1, 0];    // => 2
double caught = cm[1, 1];    // => 2
double falseAlarms = cm[0, 1];   // => 1
```

**Remarks** — compute this once and pass it to every metric you are reporting. All of `Accuracy`,
`Precision`, `Recall`, `F1`, `FBeta`, `BalancedAccuracy`, `CohenKappa`, `MatthewsCorrelation` and
`ClassificationReport` have an overload that takes it, and the counting pass is the expensive
part.

Two properties of the shape are worth fixing in your head, because both directions exist in the
wild. **Rows are truth, columns are prediction** — scikit-learn's orientation, and the transpose
of
what some textbooks draw. And the index is a position in `Labels`, not a label value: on labels
`[3, 7]`, `cm[0, 1]` means "truly 3, predicted 7". `Labels` is the sorted union when `labels` was
omitted, and the caller's order **left unsorted** when it was given, which is also scikit-learn's
rule and the one that lets a diagonal be moved on purpose.

Cells are `double` rather than `int` because `sampleWeight` exists. Unweighted counts stay exact
up
to 2^53, so nothing is lost by it.

The trap is `labels` as a filter. A sample whose true or predicted label falls outside the set is
**not counted anywhere** — not in a row, not in a total — so the matrix's `TotalWeight` can be
less
than the number of samples you passed, and every metric read off it inherits that. That is
`confusion_matrix(labels=…)`'s own behaviour; it just surprises people who expected a filter on
rows
rather than on samples.

**Applies to** — net10.0, netstandard2.0.

**See also** — `ConfusionMatrix.ToArray`, `Normalization`, `ClassificationReport.Compute`,
the [Python equivalence table](../../../equivalence.md).
