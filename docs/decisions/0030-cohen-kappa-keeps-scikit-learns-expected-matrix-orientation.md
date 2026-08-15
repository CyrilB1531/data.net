# 0030 — `CohenKappa` keeps scikit-learn's expected-matrix orientation, and weighting only orders

**Status:** accepted · **Date:** 2026-08-14

## Context

`cohen_kappa_score` computes an expected cell as `outer(s0, s1)[row, col] / n`
with `s0` the column sums and `s1` the row sums — `colSums[row] * rowSums[col]
/ n`, the transpose of what a reader would reach for first,
`outer(rowSums, colSums)`. Every `KappaWeighting` this package defines
(`None`, `Linear`, `Quadratic`) is
symmetric in `row` and `col`, so summing the weighted expected matrix gives the
same total either way and both orientations pass every oracle case. The
scikit-learn orientation is used anyway, and spelled out as
`colSums[row] * rowSums[col] / total` rather than simplified, so a later
reader does not "fix" what only looks backwards and, on some future asymmetric
weighting, silently changes the answer.

`Linear` and `Quadratic` weighting measure a distance between class
*positions* in `ConfusionMatrix.Labels`, not between the class values
themselves, so a weighted kappa depends on that order. A full reversal of the
label order preserves every position's distance to every other position and
so always returns the same kappa; an arbitrary permutation that is not a
reversal generally changes it, though a sufficiently symmetric matrix can
happen to be invariant under one anyway. `KappaWeighting.None` only asks
whether two positions are equal, so it never depends on the order at all.

`Score(ConfusionMatrix)` scores exactly the classes the matrix holds: cells,
sums and total all come from the `ConfusionMatrix.Labels`-sized view, so a
label subset that dropped samples contributes none of them, not even to a
denominator. `cohen_kappa_score` does take a `labels=` argument, so a reference
value exists for such a matrix: on the fixture
`CohenKappaTests.YTrue`/`YPred` (7 samples, 3 classes, unrestricted kappa
`0.575757575758`), restricting to `labels=[1, 2]` drops every sample touching
label 0 and leaves a diagonal — perfect agreement — so the restricted kappa is
`1.0`, matching `cohen_kappa_score(y_true, y_pred, labels=[1, 2])` exactly.
That is agreement measured on one fixture, not a general guarantee: the rule
implemented is "score what the matrix holds", stated without reference to what
any full-label-set scikit-learn call would say.

## Decision

Compute the expected cell as `colSums[row] * rowSums[col] / total`, matching
scikit-learn's `outer(s0, s1) / n` term for term rather than the more
intuitive `outer(rowSums, colSums)`. Read `KappaWeighting.Linear`/`Quadratic`
as a function of label *position*, not label *value*, and document the
order-dependence rather than sorting it away. Score a matrix's own label view,
never the full observed set, whether or not scikit-learn's `labels=` argument
exists for the same call shape.

## Consequences

- The `<remarks>` on
  [`CohenKappa.Score(ConfusionMatrix, ...)`](../reference/metrics/classification/cohenkappa-score.md)
  and the `Weight` loop's inline comment both carry a pointer here instead of
  restating the orientation and order-dependence arguments.
- **The orientation is not verified by anything, and cannot be.** All three
  weightings are symmetric, so the two orientations return the same kappa on
  all 78 corpus fixture × weighting combinations the theory runs — the choice
  rests on term-for-term correspondence with `cohen_kappa_score`'s
  `outer(s0, s1)`, not on a test that would fail if it were transposed. Only
  an asymmetric weighting could tell them apart, and this package defines none.
- Value parity is verified by `CohenKappaTests.Matches_sklearn_cohen_kappa_score`
  (the oracle theory, unweighted and both weightings),
  `Unweighted_kappa_is_invariant_under_any_permutation_of_the_labels`,
  `A_restricted_label_set_reads_over_the_matrix_it_holds` (the `[1, 2]` → `1.0`
  case above), and the permutation-dependence test at
  `tests/DataNet.Metrics.Tests/CohenKappaTests.cs:61-77` for `Linear`/`Quadratic`.
