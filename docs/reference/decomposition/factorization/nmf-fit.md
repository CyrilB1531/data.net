# Nmf.Fit

Factorizes a sparse non-negative matrix into two non-negative factors, by Lee and Seung's
multiplicative updates.

<!-- docs-declaration -->

```csharp
public static Nmf Fit(CsrMatrix matrix, int componentCount, NmfOptions options = null)
public static Nmf Fit(CsrMatrix matrix, double[] initialWeights, double[] initialComponents, NmfOptions options = null)
```

**Parameters** — `matrix` is the matrix to factorize, rows as samples and columns as features; it
is read, never modified, and it must hold no negative value. `componentCount` is the rank to keep,
at least 1, strictly below the number of columns and no greater than the number of rows.
`initialWeights` is `W₀`, row-major `matrix.RowCount × componentCount`, and `initialComponents` is
`H₀`, row-major `componentCount × matrix.ColumnCount`; both are non-negative, both are copied, and
the rank is read off their lengths rather than passed again. `options` carries the solver's
settings, or is left out for scikit-learn's defaults: the Frobenius loss, NNDSVD, 200 iterations
and a tolerance of `1e-4`.

**Returns** — an `Nmf` holding `W`, `H`, the iteration count and the reconstruction error. Every
property is populated; there is no second call to make.

**Exceptions** — `ArgumentNullException` when `matrix`, `initialWeights` or `initialComponents` is
null. `ArgumentOutOfRangeException` when `componentCount` is below 1, at or above the number of
columns, above the number of rows, or when `MaxIterations` is below one or `Tolerance` is negative.
`ArgumentException` when `matrix` holds a negative value or a `NaN`, on either overload — the
precondition is checked rather than assumed, since a negative entry does not fail the loop, it
returns signed factors under the Frobenius loss and `NaN` under Kullback–Leibler. `ArgumentException`
too when the two blocks do not agree on a component count, do not fit the matrix, or hold a negative
number — and when [`RandomMatrix`](nmfoptions.md) is given and is not
`matrix.ColumnCount × (componentCount + 10)` values long.

**Example** — four documents over three terms, factorized at rank 2.

```csharp
using Lodestar.Abstractions;
using Lodestar.Decomposition;

// Row-major CSR: values, the column each sits in, and where each row starts.
CsrMatrix matrix = new(
    4, 3,
    [3.0, 1.0, 2.0, 1.0, 1.0, 4.0, 2.0, 3.0],
    [0, 1, 0, 2, 1, 2, 0, 2],
    [0, 2, 4, 6, 8]);

Nmf fitted = Nmf.Fit(matrix, 2);

int rounds = fitted.Iterations;                             // => 50
double error = Math.Round(fitted.ReconstructionError, 3);   // => 1.066
double firstTerm = Math.Round(fitted.Components[0], 3);     // => 0.049
```

**Example** — the same matrix from an initialisation written down rather than computed, with the
early stop disabled so the iteration count is an input.

```csharp
using Lodestar.Abstractions;
using Lodestar.Decomposition;

CsrMatrix corpus = new(
    4, 3,
    [3.0, 1.0, 2.0, 1.0, 1.0, 4.0, 2.0, 3.0],
    [0, 1, 0, 2, 1, 2, 0, 2],
    [0, 2, 4, 6, 8]);

// W₀ is 4 × 2 and H₀ is 2 × 3, both row-major and neither holding a negative number.
double[] initialWeights = [1.0, 0.5, 0.5, 1.0, 0.8, 0.8, 1.0, 0.2];
double[] initialComponents = [1.0, 0.5, 1.0, 0.5, 1.0, 1.0];

Nmf model = Nmf.Fit(
    corpus, initialWeights, initialComponents,
    new NmfOptions { MaxIterations = 100, Tolerance = 0.0 });

int ran = model.Iterations;                                  // => 100
double reached = Math.Round(model.ReconstructionError, 3);   // => 1.066
```

**Remarks** — the two overloads are one algorithm. The first computes `W₀` and `H₀` with
[`NmfInitialization`](nmfinitialization.md) and then calls the second, so anything the
initialisation decides — most of all which entries are zero, because a multiplicative update can
never revive one — is decided before the loop starts.

Each iteration updates `W` first and `H` second, against the already-updated `W`. Updating both
against the old pair is a different algorithm that converges more slowly, and it is why an
implementation that looks right can be close and never equal.

The stopping rule is scikit-learn's: the relative improvement is measured every tenth iteration
and never on the others, against the divergence at the initialisation. A
[`Tolerance`](nmfoptions.md) of zero disables it, which turns `MaxIterations` into an exact
iteration count — the form the oracle corpus freezes, and the form to use when two runs must be
compared step for step.

The answer is a local minimum, not the minimum. Two initialisations reach two different
factorizations of the same rank, both non-negative and both valid; `NndSvd` is deterministic once
Ω is fixed, which is what makes a run repeatable at all.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Nmf`](nmf.md), [`NmfOptions`](nmfoptions.md), [`NmfBetaLoss`](nmfbetaloss.md),
[`NmfInitialization`](nmfinitialization.md), the
[Python equivalence table](../../../equivalence.md).
