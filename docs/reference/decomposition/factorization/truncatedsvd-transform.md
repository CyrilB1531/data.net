# TruncatedSvd.Transform

Projects rows onto the fitted components — `X · Componentsᵀ`, and nothing else.

<!-- docs-declaration -->

```csharp
public double[] Transform(CsrMatrix matrix)
```

**Parameters** — `matrix` holds the rows to project, and must have exactly `FeatureCount` columns.
It does not have to be the matrix that was fitted, and usually is not.

**Returns** — `double[]`, row-major and `ComponentCount` wide: `matrix.RowCount × ComponentCount`
values, row `i`'s coordinates starting at `i * ComponentCount`.

**Exceptions** — `ArgumentNullException` when `matrix` is null. `ArgumentException` when `matrix`
does not have `FeatureCount` columns.

**Example** — the same four documents, projected onto the two components fitted from them.

```csharp
using Lodestar.Abstractions;
using Lodestar.Decomposition;

CsrMatrix corpus = new(
    4, 3,
    [3.0, 1.0, 2.0, 1.0, 1.0, 4.0, 2.0, 3.0],
    [0, 1, 0, 2, 1, 2, 0, 2],
    [0, 2, 4, 6, 8]);

TruncatedSvd model = TruncatedSvd.Fit(corpus, 2);
double[] projected = model.Transform(corpus);

int values = projected.Length;                             // => 8
double firstDocument = Math.Round(projected[0], 3);        // => 1.672
```

**Remarks** — this is *not* `U · Σ`. The two agree for an exact SVD and differ by the randomized
solver's approximation error for this one, which on a real corpus is visible in the third decimal
rather than the last bit. scikit-learn's `TruncatedSVD.transform` computes the projection, so this
does too, and `ExplainedVariance` is measured on it for the same reason.

Nothing is centred and nothing is scaled, here or in the fit, so a row of zeros projects to the
origin and a document twice as long projects twice as far. Normalize the rows before fitting if
that is not what you want — [`CsrMatrix.NormalizeRows`](../../abstractions/sparse/csrmatrix-normalizerows.md)
does it in place.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TruncatedSvd.Fit`](truncatedsvd-fit.md), the
[Python equivalence table](../../../equivalence.md).
