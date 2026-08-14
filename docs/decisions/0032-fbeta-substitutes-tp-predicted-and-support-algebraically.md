# 0032 — `FScore` substitutes tp/predicted/support algebraically, not via precision and recall

**Status:** accepted · **Date:** 2026-08-14

## Context

The textbook F-beta formula is `(1 + beta^2) * P * R / (beta^2 * P + R)` for
precision `P` and recall `R`. The obvious implementation computes `P` and `R`
first — each already available as `Precision.Score`/`Recall.Score` — and
substitutes. scikit-learn's `fbeta_score` does not do this: it derives F-beta
from the raw `tp`/`predicted`/`support` counts directly. Substituting
`P = tp/predicted` and `R = tp/support` into the textbook formula and
cancelling `tp` leaves `score = (1 + beta^2) * tp / (predicted + beta^2 * support)`
— algebraically the same value when `predicted` and `support` are both
nonzero, but not the same *computation*.

Going through `P` and `R` first applies `Prf.Divide`'s zero-division policy up
to three times for one F-beta value: once for `P`, once for `R`, and once more
for the combined denominator, which can itself look like zero even when the
uncombined one is not. It also fails to reproduce scikit-learn whenever `tp`
is zero but `predicted` or `support` is not: the textbook route replaces an
undefined `P` or `R` with the zero-division policy's value (`0`, `1` or `NaN`)
before combining, while `scikit-learn` — and the direct-substitution formula —
still returns a well-defined `0` from the combined fraction, since the
numerator `(1 + beta^2) * 0` is exactly `0` and the denominator is not.

## Decision

`Prf.FScore` computes `(1 + beta^2) * tp / (predicted + beta^2 * support)`
directly from the three raw counts, routed through the single `Prf.Divide`
call the substituted formula needs — never through `Precision`/`Recall`'s own
already-divided values.

## Consequences

- `Prf.FScore`'s comment carries a pointer here instead of restating the
  derivation and the divergence it prevents.
- Verified by `PrfOracleTests.Matches_sklearn_precision_recall_fscore_support`,
  whose corpus `fbeta` field spans multiple `beta` values including the
  `tp == 0` edge this derivation exists for, compared at
  `MetricsCorpus.Tolerance` (`1e-9`).
