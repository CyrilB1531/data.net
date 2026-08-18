# 0039 — `MutualInformation` returns `0.0` on an empty input; scikit-learn raises

**Status:** accepted · **Date:** 2026-08-18

## Context

Every clustering-agreement metric in `Lodestar.Metrics` treats an empty input as a case rather
than an error: [`AdjustedRand.Score`](../reference/metrics/clustering/adjustedrand-score.md), [`NormalizedMutualInformation.Score`](../reference/metrics/clustering/normalizedmutualinformation-score.md),
[`AdjustedMutualInformation.Score`](../reference/metrics/clustering/adjustedmutualinformation-score.md), [`Homogeneity.Score`](../reference/metrics/clustering/homogeneity-score.md) and the rest all answer `1.0` on `([], [])`,
because agreeing about nothing is agreeing. That is a deliberate choice #172 made, not an
accident, and it is followed here.

`mutual_info_score(labels_true, labels_pred)` is the odd one measured against it.

### What was measured

`sklearn.metrics.mutual_info_score([], [])`, on scikit-learn 1.9.0:

```text
ValueError: math domain error
```

Raised from `sklearn/metrics/cluster/_supervised.py:929`, inside `log(pi.sum())` — the marginal
sums are zero, so the logarithm is undefined. There is no parameter guard, no docstring note and
no dedicated exception type: it is `numpy`'s `math.log(0.0)` surfacing through eight call frames.
It was found by the corpus generator crashing rather than by reading the source, which is itself
evidence that it is not a designed refusal — a designed one would say what was refused and why.

Contrast with [`decision 0034`](0034-dropout-is-refused-for-want-of-a-user.md), where a refusal
this package raises is *itself* the deliberate choice, argued and named. Here the reference's
behaviour is the accident and this package's is the considered one.

## Decision

[`MutualInformation.Score(ReadOnlySpan<int>, ReadOnlySpan<int>)`](../reference/metrics/clustering/mutualinformation-score.md) returns `0.0` on an empty input,
rather than throwing to match the reference exactly. `mutual_info_score` is `0.0` on every input
with one class on either side — verified separately, not only inferred — and an empty input is
the boundary of that shape, not a different one. Zero shared information between two empty
labellings is at least as defensible an answer as an exception raised by an unguarded logarithm,
and it is consistent with every sibling metric in this file.

## Consequences

- The frozen corpus (`tests/oracles/clustering_agreement.json`) records `mutual_information: null`
  for the `"empty"` fixture rather than a number, so the divergence is visible in the fixture a
  reader would check first, not only in prose.
- `docs/equivalence.md`'s row for `mutual_info_score` names this ADR.
- If a later `gammaln`-based rewrite of `ExpectedMutualInformation` or `Contingency.MutualInformation`
  ever needs the reference's exact failure mode reproduced (unlikely — nothing here has asked for
  it), that is a new decision, not a silent reversal of this one.
