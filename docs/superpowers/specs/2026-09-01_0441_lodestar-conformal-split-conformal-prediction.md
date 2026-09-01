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

- `numpy.quantile(scores, (1 - alpha) * (n + 1) / n, method="higher")` returns exactly that k-th
  smallest — the two conventions agree, and the ceiling form is the one implemented because it says
  what it means.
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

**`k > n` is the other edge**: when α is small relative to the calibration size the rule asks for a
score that does not exist, and the honest answer is the trivial one — an infinite interval, or the
full label set. That is a real answer with real coverage, and hiding it would be the same mistake.

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
