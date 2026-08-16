# ConfusionMatrix.ToArray

Copies the cells into a rectangular array, raw or scaled.

<!-- docs-declaration -->

```csharp
public double[,] ToArray()
public double[,] ToArray(Normalization normalization)
```

**Parameters** — `normalization` says which sum each cell is divided by: none, its row, its
column,
or the grand total. The parameterless overload is `Normalization.None`.

**Returns** — a fresh `double[,]` of `Labels.Count` rows and columns. The matrix keeps its own
storage, so writing into the result changes nothing.

**Exceptions** — `ArgumentOutOfRangeException` when `normalization` is not one of the four modes.

**Example** — the same matrix as counts and as per-class recalls.

```csharp
using Lodestar.Metrics;

int[] yTrue = [1, 1, 1, 1, 0, 0, 0, 0];
int[] yPred = [1, 1, 0, 0, 1, 0, 0, 0];

ConfusionMatrix cm = ConfusionMatrix.Compute(yTrue, yPred);
double counted = cm.ToArray()[1, 1];                             // => 2
double recallOfSpam = cm.ToArray(Normalization.True)[1, 1];      // => 0.5
double shareOfAll = cm.ToArray(Normalization.All)[0, 0];         // => 0.375
```

**Remarks** — the array is what you hand to a plotting library or a serializer; the indexer is
what
you use to read one cell. Normalizing is a **projection** and not a state: the matrix is
unchanged,
and asking for it twice with different modes is legal and cheap. That choice is deliberate,
because
several metrics read a `ConfusionMatrix` and would be silently wrong if its cells had become
fractions —
[decision 0020](../../../decisions/0020-normalize-is-a-projection-not-a-parameter.md) has the
argument.

Each mode answers a different question. `True` divides each row by its own sum, so the diagonal
becomes per-class recall — the most useful heat map of the four. `Pred` divides each column by its
sum, giving per-class precision on the diagonal. `All` turns every cell into a share of the
dataset.

The trap is the zero row. A row, column or total that counted nothing yields **zeros**, not `NaN`,
which is what scikit-learn's `nan_to_num` does to the same division. A row of zeros in a
`Normalization.True` array therefore means "this class never occurred", and is indistinguishable
from "this class was never once predicted correctly" if you only look at the diagonal. Check the
support before reading a normalized row as a recall.

**Applies to** — net10.0, netstandard2.0.

**See also** — `ConfusionMatrix.Compute`, `Normalization`, `Recall.PerClass`,
[decision 0020](../../../decisions/0020-normalize-is-a-projection-not-a-parameter.md),
the [Python equivalence table](../../../equivalence.md).
