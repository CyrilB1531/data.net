# TruncatedSvd.Fit

Factorizes a sparse matrix at a given rank, by randomized SVD, without centring it.

<!-- docs-declaration -->

```csharp
public static TruncatedSvd Fit(CsrMatrix matrix, int componentCount, TruncatedSvdOptions options = null)
```

**Parameters** — `matrix` is the term-document matrix, rows as samples and columns as features; it
is read, never modified. `componentCount` is the rank to keep, at least 1 and strictly below the
number of columns — a factorization that kept them all would be an SVD, not a truncated one.
`options` carries the randomized solver's settings, or is left out for scikit-learn's defaults:
ten extra columns of oversampling, five power iterations, and the normalizer chosen automatically.

**Returns** — a `TruncatedSvd` holding the components, the singular values and the explained
variance. Every property is populated; there is no second call to make.

**Exceptions** — `ArgumentNullException` when `matrix` is null. `ArgumentOutOfRangeException` when
`componentCount` is below 1, at or above the number of columns, above the number of rows, when an
option is negative, or when `Oversampling` and `componentCount` do not add up within an `int`. `ArgumentException` when
[`RandomMatrix`](truncatedsvdoptions.md) is given and is not
`FeatureCount × (componentCount + Oversampling)` values long.

**Example** — four documents over three terms, kept at rank 2.

```csharp
using Lodestar.Abstractions;
using Lodestar.Decomposition;

// Row-major CSR: values, the column each sits in, and where each row starts.
CsrMatrix matrix = new(
    4, 3,
    [3.0, 1.0, 2.0, 1.0, 1.0, 4.0, 2.0, 3.0],
    [0, 1, 0, 2, 1, 2, 0, 2],
    [0, 2, 4, 6, 8]);

TruncatedSvd fitted = TruncatedSvd.Fit(matrix, 2);

int kept = fitted.ComponentCount;                                 // => 2
double first = Math.Round(fitted.SingularValues[0], 3);           // => 5.614
double covered = Math.Round(fitted.ExplainedVarianceRatio.Sum(), 3);   // => 0.928
```

**Remarks** — the answer is randomized. Ω, the block that probes the matrix's range, is drawn
from this package's own generator when only `Seed` is given, and the numbers below the fourth or
fifth digit move with it. Hand it
[`RandomMatrix`](truncatedsvdoptions.md) instead and the run
is reproducible across implementations rather than merely across runs.

Oversampling and power iterations both buy accuracy and both cost time. The defaults are
scikit-learn's, and are the right starting point; raising `PowerIterations` is what helps when the
singular values decay slowly, which is the case a randomized method finds hardest.

`ExplainedVarianceRatio` is not sorted. Its denominator is the input's *total* column variance, so
the entries sum to less than one — but the first component of an uncentred factorization carries
the mean direction, which is large in norm and small in variance, and it is routinely not the
largest share. That is the arithmetic, not a bug: use the sum to judge the rank, not the order.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TruncatedSvd.Transform`](truncatedsvd-transform.md),
[`TruncatedSvdOptions`](truncatedsvdoptions.md),
[`PowerIterationNormalizer`](poweriterationnormalizer.md), the
[Python equivalence table](../../../equivalence.md).
