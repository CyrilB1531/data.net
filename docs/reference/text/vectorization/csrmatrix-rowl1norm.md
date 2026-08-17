# CsrMatrix.RowL1Norm

The sum of one row's absolute values — its total mass.

<!-- docs-declaration -->

```csharp
public double RowL1Norm(int row)
```

**Parameters** — `row` is the zero-based row index.

**Returns** — `double`, the sum of `|value|` over that row's stored cells. Zero for an empty row.

**Exceptions** — `ArgumentOutOfRangeException` when `row` is negative or not below `RowCount`.

**Example** — on raw counts, the L1 norm is how many terms the document holds.

```csharp
using Lodestar.Text.Vectorization;

string[] docs = ["the cat eats", "the dog eats", "the cat and the dog"];
CsrMatrix counts = new CountVectorizer().FitTransform(docs);

double first = counts.RowL1Norm(0);   // => 3
double third = counts.RowL1Norm(2);   // => 5
```

**Remarks** — absolute values, which matters only where a matrix can hold negatives:
[`HashingVectorizer`](hashingvectorizer.md) with `AlternateSign` on produces them by design, and
there the L1 norm is the total weight rather than a net one.

Dividing a row by this norm turns it into a distribution over terms, which is what
[`NormalizeRows(SparseNorm.L1)`](csrmatrix-normalizerows.md) does.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CsrMatrix.RowL2Norm`](csrmatrix-rowl2norm.md),
[`CsrMatrix.NormalizeRows`](csrmatrix-normalizerows.md), [`SparseNorm`](sparsenorm.md).
