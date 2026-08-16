# Normalization

Which sum `ConfusionMatrix.ToArray` divides each cell by.

<!-- docs-declaration -->

```csharp
public enum Normalization { None, True, Pred, All }
```

**Members** — `None` leaves the raw counts, or weights when the matrix is weighted. `True` divides
each row by its own sum, so the diagonal reads as per-class recall. `Pred` divides each column by
its own sum, so the diagonal reads as per-class precision. `All` divides every cell by the grand
total, turning each into a share of the dataset.

**Example** — one matrix, three readings of the same cell.

```csharp
using Lodestar.Metrics;

int[] yTrue = [1, 1, 1, 1, 0, 0, 0, 0];
int[] yPred = [1, 1, 0, 0, 1, 0, 0, 0];

ConfusionMatrix cm = ConfusionMatrix.Compute(yTrue, yPred);
double count = cm.ToArray(Normalization.None)[1, 1];      // => 2
double recall = cm.ToArray(Normalization.True)[1, 1];     // => 0.5
double precision = cm.ToArray(Normalization.Pred)[1, 1];  // => 0.6666…
```

**Remarks** — `True` is the one to reach for when drawing a heat map, because a row that sums to 1
lets a rare class and a common class be compared by eye; raw counts make every rare class look
black. `Pred` answers the mirror question — "when the model says this, how often is it right" —
and
`All` is for reporting shares of a dataset.

The trap is that this is a projection and not a parameter on `Compute`. There is no such thing as
a
normalized `ConfusionMatrix` here, and that is deliberate: `Accuracy`, `Precision` and the rest
read
a matrix's cells directly, and would be silently wrong if those cells had become fractions —
[decision 0020](../../../decisions/0020-normalize-is-a-projection-not-a-parameter.md).

A row, column or total that counted nothing divides to **zero**, not `NaN`, matching
scikit-learn's
`nan_to_num`.

**Applies to** — net10.0, netstandard2.0.

**See also** — `ConfusionMatrix.ToArray`, `ConfusionMatrix.Compute`,
[decision 0020](../../../decisions/0020-normalize-is-a-projection-not-a-parameter.md),
the [Python equivalence table](../../../equivalence.md).

## Members

| Member | What it does |
| --- | --- |
