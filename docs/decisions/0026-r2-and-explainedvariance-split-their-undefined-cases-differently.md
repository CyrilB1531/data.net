# 0026 — R² and ExplainedVariance split their undefined cases differently

**Status:** accepted · **Date:** 2026-08-14

## Context

`R2.Score` and `ExplainedVariance.Score` both divide by the variance of
`yTrue`, both take `forceFinite`, and both were assumed — before this was
checked against scikit-learn 1.9.0 rather than read off its source — to
handle a zero denominator the same way. They do not.

`sklearn.metrics.r2_score` carries an explicit `n < 2` check ahead of the
division: with fewer than two samples the variance of `yTrue` is not just
zero, it is not a defined quantity at all, and the function returns `nan`
regardless of `force_finite`, with a warning. `explained_variance_score`
carries no such check. A single sample still has a variance — zero, by the
definition of variance over one point — so it reaches the same
zero-denominator branch a constant multi-sample target would, and
`force_finite` decides it exactly as it would for any other zero variance.

`R2.cs` reproduces the split with two independent knobs: `forceFinite`
answers the zero-variance-over-two-or-more-samples branch, and
`ZeroDivision` answers the fewer-than-two-samples branch, which is `nan`
under either setting of `forceFinite` and therefore is not `forceFinite`'s
case at all. `ExplainedVariance.cs` takes only `forceFinite`, because it has
no second branch to route — routing R2's fewer-than-two-samples case through
`ExplainedVariance`'s `forceFinite` would already be correct, since that
case does not exist for this metric.

## Decision

| Case | R² | ExplainedVariance |
| --- | --- | --- |
| Fewer than 2 samples | `ZeroDivision`'s case: `nan` under either `forceFinite` setting | Not a separate case — one sample has zero variance by definition and falls into the row below |
| Zero variance, 2+ samples (or the 1-sample case, for ExplainedVariance) | `forceFinite`'s case: 1 if the numerator also vanished, 0 otherwise, or unclamped `nan`/`-inf` | Same |

The two R² branches must not be merged: routing the fewer-than-two-samples
case through `forceFinite` would return `-inf` where scikit-learn returns
`nan`, on every fixture that reaches it with `forceFinite: false`.

## Consequences

- `R2.Resolve` and `ExplainedVariance.Resolve` carry the full per-branch
  reasoning at the point that implements it; the class-level `<remarks>` on
  `R2` and `ExplainedVariance` point here instead of repeating it.
- Verified by `R2Tests.Fewer_than_two_samples_is_zeroDivisions_case_and_not_forceFinites`,
  `R2Tests.Zero_variance_over_two_samples_is_forceFinites_case_and_not_zeroDivisions`
  and `R2Tests.Explained_variance_is_one_on_a_single_wrong_sample`.
- A metric added later that divides by a variance-like denominator should
  check scikit-learn's own source for an equivalent early-exit before
  assuming its zero-denominator handling matches either of these two.
