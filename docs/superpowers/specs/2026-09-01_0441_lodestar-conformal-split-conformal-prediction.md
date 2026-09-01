# 0441 — `Lodestar.Conformal`: split conformal prediction

**Issue:** [#441](https://github.com/CyrilB1531/lodestar/issues/441) ·
**Status:** accepted · **Date:** 2026-09-01

## Problem

Phase 3 of [#427](https://github.com/CyrilB1531/lodestar/issues/427) is the shortest phase because
[#441](https://github.com/CyrilB1531/lodestar/issues/441) verified the domain is already shipped —
classification, regression, clustering and ranking metrics all exist, and the roadmap's own
protocol ("check our own repo before declaring a gap") exists because an earlier draft proposed a
metrics phase that was already done.

One thing is genuinely absent: **split conformal prediction**, with zero C# repositories anywhere
per the survey. It turns a point prediction into an interval, or a class into a set, with a
finite-sample coverage guarantee — and it is post-hoc arithmetic over scores and labels, which is
the same boundary `Lodestar.Metrics` already draws.

## What the algorithm is, measured against MAPIE 1.5.0 rather than read

Both halves share one quantile rule, and it is the part an implementation gets wrong. With `n`
calibration scores and a miscoverage level `α`:

```text
k = ceil((n + 1) * (1 - alpha))
q = the k-th smallest score, 1-based
```

Probed against MAPIE before any C# was written, because a rule taken from a paper and a rule a
library ships are not reliably the same thing:

- `numpy.quantile(scores, (1 - alpha) * (n + 1) / n, method="higher")` does **not** return that
  k-th smallest, which an earlier draft of this section claimed. Over 4000 random `(n, alpha)`
  draws the two disagree 891 times: `higher` indexes `ceil(p(n - 1))`, a different order statistic.
  `method="inverted_cdf"` is the same rule algebraically and still disagrees 7 times in the same
  4000, because evaluating the level and multiplying by `n` again moves the product across an
  integer the ceiling form never leaves. MAPIE matches the ceiling form on every case measured —
  including `n = 19` at `alpha = 0.1`, where `(n + 1)(1 - alpha)` is exactly 18 and `higher` reads
  the 19th smallest. The ceiling form is therefore what is implemented, and the corpus asserts
  MAPIE rather than numpy. [Decision 0070](../../decisions/0070-k-greater-than-n-returns-an-infinite-interval.md)
  records the measurement.
- **Regression.** `SplitConformalRegressor(prefit=True)` over 30 calibration points at α = 0.1
  returns intervals whose half-width equals the hand-computed `q` to the last bit, on every test
  row. Score is `|y − ŷ|`; interval is `ŷ ± q`.
- **Classification (LAC).** `SplitConformalClassifier(conformity_score="lac", prefit=True)` over 80
  calibration points at α = 0.2 returns prediction sets identical to `p̂ⱼ ≥ 1 − q`, where the
  calibration score is `1 − p̂(true class)`.

**The empty prediction set is reproduced, not repaired.** One of the six probe rows came back
empty, and that is what LAC does when no class clears the threshold. A package that quietly
substituted the arg-max there would be returning something with no coverage guarantee under a name
that promises one.

**`k > n` is the other edge**, and it is the one place this package does not reproduce MAPIE. When
`α < 1 / (n + 1)` the rule asks for a score that does not exist. Measured: MAPIE 1.5.0 raises
`ValueError` in both halves, and under `allow_infinite_bounds=True` its regressor returns a
*finite* interval whose half-width is the largest calibration score. That last answer is narrower
than the level asked for — it under-covers — so `Quantile` returns `double.PositiveInfinity`
instead, and `Interval` and `PredictionSet` carry it through to the whole line and the full label
set. That is a real answer with real coverage, and hiding it would be the same mistake as repairing
the empty prediction set.
[Decision 0070](../../decisions/0070-k-greater-than-n-returns-an-infinite-interval.md) has the
three measurements and why throwing was the runner-up.

## Scope

Two functions' worth of algorithm, both static, arrays in and numbers out — the same shape and the
same boundary as `Lodestar.Metrics`: no model, no training loop, no `IDataView`, nothing to
serialize.

The calibrated quantile is returned to the caller rather than held in an object. That is the
smaller API, and it has a second merit: the number that carries the guarantee is visible, so the
exchangeability warning below attaches to something the caller is holding rather than to a
constructor they called once.

## Exchangeability is half the deliverable

The coverage guarantee holds under **exchangeability** of the calibration and test data. It does
not hold for time series, for data with drift, or for any split that leaks. The intervals still
come out — they simply do not cover, and nothing in the output says so.

Issue #441 states the consequence plainly and this spec adopts it: *a conformal package whose front page
does not lead with that is worse than no package, because it hands people a number they will
trust.* So the warning is not a remark at the bottom of a page. It is a named section in the guide,
it is in the package description, and it is in the XML documentation of every member that returns
a quantile or an interval.

## Placement

Core tier per [decision 0069](../../decisions/0069-the-package-layout-as-built-and-what-enforces-it.md):
`net10.0;netstandard2.0`, zero dependencies, no inter-package edge in either direction. It takes
arrays and returns numbers and has no reason to need anything else — which is also why it does not
raise the `Abstractions` question that decision leaves open for Phase 2.

0069's first rule is what admits a fifth package at all: *split only for a distinct dependency
profile, audience or release cadence — never for tidiness.* This is a distinct audience — someone
reaching for a coverage guarantee is not the caller reaching for `Levenshtein` — on a distinct
cadence, and it adds no dependency to anyone who does not install it. Folding it into
`Lodestar.Metrics` would be the tidiness that rule refuses: metrics score a model that has already
predicted, conformal prediction changes what the model outputs.

`tools/check_nuspec_dependencies.py` gains its expected graph: nothing on `net10.0`, the two
polyfills on `netstandard2.0`, exactly as `Lodestar.Metrics` carries.

## Testing

- A frozen oracle corpus generated from MAPIE 1.5.0, replayed at `1e-9`: both halves, several
  calibration sizes and levels, including the two edges above.
- MAPIE joins `tools/requirements.txt`; its base dependencies (numpy, scikit-learn, scipy) are
  already in the lock, so the graph grows by one package rather than a tree.
- The same suite runs against the `netstandard2.0` assembly, as every package's does.

## Benchmarks

There is no .NET incumbent to measure against — that is the survey's finding, not an omission, and
per [#438](https://github.com/CyrilB1531/lodestar/issues/438) the benchmark section says so rather
than being left blank.
