# Nmf

A fitted non-negative matrix factorization of a sparse matrix: `X ≈ W H`, with every entry of both
factors at or above zero. There is no unfitted state — [`Nmf.Fit`](nmf-fit.md) is the only way to
reach an instance — and no property has to decide what to do when it is read too early.

The non-negativity is the whole point. A truncated SVD's components are signed and orthogonal, so
a "topic" may subtract a word as readily as add one; these components only ever add, which is what
makes a row of `H` readable as a set of terms and a row of `W` readable as a mixture of them.
[`TruncatedSvd`](truncatedsvd.md) is the answer when the subspace matters and the signs do not.

## Properties

| Property | What it holds |
| --- | --- |
| `ComponentCount` | `int` — how many components were fitted, the rank `W` and `H` share. |
| `FeatureCount` | `int` — how many columns the factorized matrix had. |
| `Iterations` | `int` — how many multiplicative updates ran, scikit-learn's `n_iter_`. Equal to `MaxIterations` when the tolerance is zero. |
| `ReconstructionError` | `double` — the beta divergence between `X` and `W H` at the end, square-rooted: scikit-learn's `reconstruction_err_`. |
| `Weights` | `IReadOnlyList<double>` — `W`, row-major `rows × ComponentCount`. Row `i`'s mixture starts at `i * ComponentCount`. This is what scikit-learn's `fit_transform` returns. |
| `Components` | `IReadOnlyList<double>` — `H`, row-major `ComponentCount × FeatureCount`, scikit-learn's `components_`. Component `c` starts at `c * FeatureCount`. |

`ReconstructionError` is comparable only between fits that minimised the *same*
[`NmfBetaLoss`](nmfbetaloss.md): the Frobenius number is a distance and the Kullback–Leibler one
is a divergence, and they are not on one scale.

There is no `Transform`. Projecting an unseen row onto a non-negative basis is itself a
factorization — the same multiplicative loop with `H` held fixed — rather than the product a name
borrowed from the SVD would suggest, and shipping it under that name would promise a cost it does
not have.

## Members

| Member | What it does |
| --- | --- |
| [`Nmf.Fit`](nmf-fit.md) | Factorizes a sparse matrix, from an initialisation it computes or one you supply. |
