# 0023 — The weighted median averages two order statistics within scikit-learn's epsilon, not exactly at half

**Status:** accepted · **Date:** 2026-08-14

## Context

[Issue #92](https://github.com/CyrilB1531/data.net/issues/92) gives
`median_absolute_error` a `sample_weight` parameter, which needs a weighted
median. The unweighted case is not "the value at the halfway point": on four
uniformly weighted residuals, scikit-learn returns the mean of the two middle
values. `WeightedPercentile.Average` therefore always averages a pair of order
statistics — the first index whose cumulative weight reaches half the total,
and the one just past the last index that comes within one machine epsilon of
it — rather than branching between "one value" and "two values" as separate
cases. Where the two indices coincide, which happens on every odd count and
every sufficiently lopsided weighting, the average is that single value with
no separate code path for it.

The epsilon is not this library's choice; it is scikit-learn's own.
`_weighted_percentile` in `sklearn/utils/stats.py` compares its
`fraction_above` against `np.finfo(np.float64).eps` rather than against zero,
and reproducing the tolerance turned out to be load-bearing, not decorative.

## Decision

Compare the cumulative weight to half the total with the same absolute
tolerance scikit-learn uses, `np.finfo(np.float64).eps` = `2.220446049250313e-16`,
written out as `WeightedPercentile.MachineEpsilon` because .NET has no built-in
constant for it (`double.Epsilon` is the smallest positive subnormal, 292
orders of magnitude smaller, and answers a different question). The same
constant backs the denominator clamp in `MeanAbsolutePercentageError`, for an
unrelated reason — division by an exact zero there, not a cumulative-sum
tolerance here — so it is written twice rather than shared, to avoid coupling
two features that only coincide in value.

**Why an exact comparison is wrong, measured.** On a uniform *fractional*
weight — `[0.1] × 10`, or NumPy's own `np.ones(n) / n` — the cumulative sum
overshoots half the total by a few units in the last place of a `double`. An
exact `cumulative <= half` test then takes a single order statistic where
scikit-learn averages two. Replayed against scikit-learn 1.9.0 on the
residuals `0..9` (`y_true = 0..9`, `y_pred = 0`):

- Unweighted: `median_absolute_error(y_true, y_pred)` = 4.5.
- `sample_weight = [0.1] * 10`: 4.5 — the epsilon tolerance recovers the
  unweighted answer. This is the frozen corpus case
  `tests/oracles/regression.json`, fixture `uniform_fractional_weights`
  (`weighted: true`), field `median_ae|uniform`.
- `sample_weight = [0.7] * 10`: 5.0 — a *different* answer on the same
  residuals, from floating-point summation order alone (`0.7 × 5` lands
  exactly on `3.5`; the running total of ten additions of `0.7` does not land
  exactly on `7.0`). This case is not in the committed corpus; it was checked
  by running `sklearn.metrics.median_absolute_error` under
  `.venv-oracles/bin/python` (scikit-learn 1.9.0) during this sweep, on
  2026-08-14, and is not re-verified by any committed test.

**It does not follow that a uniform weight always reproduces the unweighted
median.** The `[0.7] × 10` case above is the proof: same shape of weighting,
same residuals, a different result, because the overshoot there is wider than
an epsilon. The tolerance is a width scikit-learn measured against `eps`, not
a licence to average whenever the weights happen to be equal.

## Consequences

- `WeightedPercentile.Average`, `WeightedPercentile.MachineEpsilon` and the
  epsilon comparison at the point of use each carry a one-line pointer to this
  record instead of restating the argument.
- `tests/oracles/regression.json`'s `uniform_fractional_weights` fixture is the
  only committed replay of this rule; a change to the epsilon comparison that
  keeps that one case green is not proven correct on inputs where the
  overshoot is wider, such as `[0.7] × 10` above, which no committed test
  covers.
