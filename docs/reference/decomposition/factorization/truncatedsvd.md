# TruncatedSvd

A fitted truncated SVD of a sparse matrix, at the rank you asked for and with nothing centred.
There is no unfitted state: [`TruncatedSvd.Fit`](truncatedsvd-fit.md) is the only way to reach an
instance, so no property has to decide what to do when it is read too early.

`FitTransform` is deliberately absent. scikit-learn's returns `X · Componentsᵀ` for the randomized
solver, while `U · Σ` is the other reading of the same words, and the two differ by exactly the
error the randomized solver leaves behind. Call
[`TruncatedSvd.Transform`](truncatedsvd-transform.md) on the matrix you fitted and the answer is
unambiguous.

## Properties

| Property | What it holds |
| --- | --- |
| `ComponentCount` | `int` — how many components were kept, the `componentCount` passed to the fit. |
| `FeatureCount` | `int` — how many columns the fitted matrix had, and how many every matrix projected afterwards must have. |
| `Components` | `IReadOnlyList<double>` — the right singular vectors, row-major `ComponentCount × FeatureCount`. |
| `SingularValues` | `IReadOnlyList<double>` — the `ComponentCount` singular values kept, largest first. |
| `ExplainedVariance` | `IReadOnlyList<double>` — the variance of each column of the projected training matrix, over `n` rather than `n − 1`. |
| `ExplainedVarianceRatio` | `IReadOnlyList<double>` — each of those over the input's total column variance, so the sum says whether the rank is enough. |

The signs of `Components` are pinned, not arbitrary: each row is flipped so that its
largest-magnitude entry is positive. Without that, two runs of the same fit would agree on the
subspace and disagree on every number in it.

## Members

| Member | What it does |
| --- | --- |
| [`TruncatedSvd.Fit`](truncatedsvd-fit.md) | Factorizes a sparse matrix at a given rank. |
| [`TruncatedSvd.Transform`](truncatedsvd-transform.md) | Projects rows onto the fitted components. |
