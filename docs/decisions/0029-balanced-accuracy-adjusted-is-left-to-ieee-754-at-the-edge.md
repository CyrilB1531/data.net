# 0029 — `BalancedAccuracy`'s `adjusted` is left to IEEE 754 at the one-class edge

**Status:** accepted · **Date:** 2026-08-14

## Context

`balanced_accuracy_score` is the mean recall over the classes that occur at
least once in `y_true` — not over every class a caller declared. A class that
is predicted but never true has an undefined recall (`0/0`) and scikit-learn
drops it from the mean rather than reading it as zero: on
`y_true=[0,0,1], y_pred=[0,2,1]`, class 2 is predicted but never true, and
`mean(recall_0=0.5, recall_1=1.0) = 0.75`, not `mean(0.5, 1.0, 0) = 0.5`.
`adjusted=True` rescales that mean by how many classes were kept, not by how
many were declared: `(score - 1/kept) / (1 - 1/kept)`.

With exactly one class kept, `1/kept` is `1`, so the rescale's denominator is
`1 - 1 = 0`. What that division returns then depends only on the numerator,
which IEEE 754 already defines: `0.0 / 0.0` is `NaN`, and a negative numerator
over zero is `-Infinity`. Measured against the oracle venv (scikit-learn
1.9.0): `balanced_accuracy_score([1,1], [1,1], adjusted=True)` is `nan` (the
one kept class's recall is exactly `1.0`, so the numerator is `0.0`), and
`balanced_accuracy_score([0,0], [0,1], adjusted=True)` is `-inf` (recall
`0.5`, numerator `-0.5`).

## Decision

[`BalancedAccuracy.Score`](../reference/metrics/classification.md#balancedaccuracyscore)
does not special-case the single-kept-class edge. `chance = 1.0 / kept` and
`(score - chance) / (1.0 - chance)` are computed exactly as written; when
`kept == 1` the denominator is `0.0` and .NET's own IEEE 754 division produces
the same `NaN`/`-Infinity` split scikit-learn does, with no branch needed to
reproduce it.

## Consequences

- The `<remarks>` on
  [`BalancedAccuracy.Score(ConfusionMatrix, bool)`](../reference/metrics/classification.md#balancedaccuracyscore)
  carries a pointer here instead of restating the averaging rule and the edge
  case.
- Verified by
  `BalancedAccuracyTests.Adjusted_divides_by_zero_when_a_single_class_is_kept`,
  which asserts `double.IsNaN` and `double.IsNegativeInfinity` for the two
  cases above, and by the `Matches_sklearn_with_adjusted` oracle theory, whose
  corpus (`tests/oracles/classification_metrics.json`) carries further
  `"NaN"` fixtures that this same code path produces with no dedicated branch.
- The "average runs over kept classes, not declared ones" rule itself is
  pinned separately by
  `BalancedAccuracyTests.Averages_over_the_classes_it_kept_not_over_all_of_them`
  and `Adjusted_divides_by_the_classes_it_kept`.
