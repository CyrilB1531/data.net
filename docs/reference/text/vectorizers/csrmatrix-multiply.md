# CsrMatrix.Multiply

The matrix times a dense vector, in one pass over the stored cells.

<!-- docs-declaration -->

```csharp
public double[] Multiply(ReadOnlySpan<double> vector)
```

**Parameters** — `vector` is the dense vector to multiply by, and must hold exactly `ColumnCount`
values.

**Returns** — `double[]` of length `RowCount`, each entry the dot product of that row with
`vector`.

**Exceptions** — `ArgumentException` when `vector` is not `ColumnCount` long.

**Example** — multiplying by all ones totals each row, which is that document's term count.

```csharp
using Lodestar.Text.Vectorization;

string[] docs = ["the cat eats", "the dog eats", "the cat and the dog"];
CsrMatrix counts = new CountVectorizer().FitTransform(docs);

double[] totals = counts.Multiply([1, 1, 1, 1, 1]);

double first = totals[0];   // => 3
double third = totals[2];   // => 5
```

**Remarks** — the cost is `NonZeroCount`, not `RowCount × ColumnCount`, which is the whole reason
to keep the matrix sparse. The third document totals `5` rather than `4` because `the` appears
twice in it and the count is a count.

This is the operation behind scoring a corpus against a linear model: the weights are the vector,
and each row's dot product is that document's score.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`CsrMatrix`](csrmatrix.md), [`CsrMatrix.ToDense`](csrmatrix-todense.md).
