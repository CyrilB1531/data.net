# 0070 — When the calibration set is too small for the level, the answer is infinite, not the widest score

**Status:** accepted · **Date:** 2026-09-01

## Context

Split conformal prediction reads off the `k`-th smallest calibration score, with
`k = ceil((n + 1) * (1 - alpha))`. `Lodestar.Conformal` reproduces MAPIE 1.5.0
everywhere else, so the two places it does not need a record.

**The rule is the ceiling form, not a numpy quantile.** The spec drafted for
[#441](https://github.com/CyrilB1531/lodestar/issues/441) claimed
`numpy.quantile(scores, (1 - alpha)(n + 1)/n, method="higher")` returns the same value.
Measured over 4000 random `(n, alpha)` draws it disagrees **891 times**: numpy's `higher`
indexes `ceil(p(n - 1))`, which is a different order statistic. `method="inverted_cdf"`
indexes `ceil(pn) - 1` and is the same rule algebraically, but disagrees **7 times** in
the same 4000 — evaluating `(1 - alpha)(n + 1)/n` and multiplying by `n` again moves the
product across an integer that the ceiling form never leaves. MAPIE matches the ceiling
form on every case measured, including `n = 19, alpha = 0.1`, where `(n + 1)(1 - alpha)`
is exactly 18 and `higher` reads the 19th smallest score. So the ceiling form is what
`SplitConformal.Quantile` computes, and the oracle corpus asserts MAPIE rather than numpy.

**The edge this record is about** is `alpha < 1 / (n + 1)`, where `k > n`: the level asks
for a score the calibration set does not hold. Measured against MAPIE 1.5.0 with nine
calibration points at `alpha = 0.05`, where `k = ceil(10 * 0.95) = 10`:

| call | MAPIE 1.5.0 |
| --- | --- |
| `SplitConformalRegressor.predict_interval(X)` | raises `ValueError` — *"Number of samples of the score is too low, 1/confidence_level and 1/(1 - confidence_level) must be lower than the number of samples."* |
| the same, `allow_infinite_bounds=True` | returns a **finite** interval, half-width `0.5`, which is the largest calibration score |
| `SplitConformalClassifier.predict_set(X)` | raises the same `ValueError`; there is no flag |

## Decision

`SplitConformal.Quantile` returns `double.PositiveInfinity`. `Interval` carries it to
`(-inf, +inf)` and `PredictionSet` to the full label set. Neither throws.

## Options refused

**Clamp to the largest calibration score** — MAPIE's answer under `allow_infinite_bounds`.
It is narrower than the level asked for, so it **under-covers**, silently, in exactly the
regime where the calibration set was already too small for anyone to notice. A package
whose front page promises a finite-sample guarantee cannot ship that as its edge case; it
is the same mistake as substituting the arg-max for an empty LAC prediction set, which
this package also refuses.

**Throw** — MAPIE's default, and the closer call. Refused for a smaller reason than the
above: this API hands the calibrated quantile back to the caller rather than holding it,
so an infinity flows through arithmetic they can already see, and `double.IsInfinity(q)`
is a cheaper thing to write at a call site than a `try`/`catch` around a calibration step.
A caller who wants the exception can raise it themselves from that test; a caller who
wants the trivial interval cannot recover it from an exception.

**Return `NaN`** — not considered seriously, and named here because it is the reflex.
`NaN` propagates into every arithmetic downstream and says "this computation was
meaningless", which is the opposite of the truth: an infinite interval is meaningful and
its coverage is exactly what was asked for.

## Consequences

- The interval a caller gets back is useless and says so. That is the point: at that level
  it is the only answer with the coverage the type's name promises.
- A caller who never inspects the quantile gets an infinite interval instead of an
  exception. Three places state the edge so that is a choice rather than a surprise: the
  XML documentation of `Quantile`, `Interval` and `PredictionSet`; the **Remarks** of the
  reference entries; and the guide's *When the calibration set is too small* section.
- The oracle corpus cannot carry these cases, because MAPIE produces no value for them.
  They are asserted by `SplitConformalEdgeTests` against this record instead, which is why
  that file names it.
- `docs/equivalence.md` marks both rows as diverging here rather than at parity.
