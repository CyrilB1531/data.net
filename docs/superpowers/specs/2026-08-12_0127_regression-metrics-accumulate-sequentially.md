# 0127 — The regression metrics accumulate sequentially where numpy sums pairwise

**Issue:** [#127](https://github.com/CyrilB1531/data.net/issues/127) · **Date:** 2026-08-12 ·
**Branch:** `fix/127-compensated-regression-sums` · **Follow-up to:** [#92](https://github.com/CyrilB1531/data.net/issues/92)

## Context

Every regression metric in `DataNet.Metrics` accumulates in a plain sequential loop. numpy sums pairwise,
so on an ill-conditioned target — a large offset over a small spread — the two answers separate well past
the `1e-9` the oracle corpus compares at.

The issue measured it on DataNet's own arithmetic, n = 200 000, `y_true ≈ 1e9 + U(0, 1e-2)`: the sequential
mean gives `1000000000.0028958` where a Neumaier-compensated sum gives `1000000000.004993`. The gap,
`2.1e-3`, is **21% of the entire range the data occupies**. On the Python side the same shape gives
`1000000000.0028974` naively against `1000000000.0050054` from both `numpy.mean` and `math.fsum` — numpy is
exact there and the sequential loop is not, so the error is entirely ours. R² and explained variance centre
on that mean before squaring, so it propagates into the score rather than cancelling.

The corpus cannot see it: `tests/oracles/regression.json` caps at 200 samples over targets in `[0.5, 40]`,
where the conditioning does not exist. At n = 1 000 the relative difference on R² is about `4e-10`, just
inside `RegressionCorpus.AssertClose`'s `1e-9`.

## Measurements taken before deciding

Two of these change the shape of the work, and both were taken by reading this repository rather than
carried over from the issue.

| Question | Answer |
| --- | --- |
| How many places actually accumulate? | **Three**, not the eleven files the issue estimated. `Outputs.WeightedMean<TKernel>` carries the walk for seven metrics — `MeanSquaredError`, `RootMeanSquaredError`, `MeanAbsoluteError`, `MeanAbsolutePercentageError`, `MeanSquaredLogError`, `RootMeanSquaredLogError`, `PinballLoss`. `R2` and `ExplainedVariance` keep their own two passes. `MaxError` sums nothing and `MedianAbsoluteError` sorts. |
| What does the corpus cost per case? | `tests/oracles/regression.json` stores `y_true` and `y_pred` as plain JSON arrays: 18 cases, the largest 450 values, 105 KB in total. A 200 000-sample case stored the same way is several megabytes. |
| What does the hot loop cost today? | `docs/guides/performance.md` §"Regression metrics" times four operations over 24 rows. `mse` at n = 1 000 000 sits at **1.04× / 1.00×** against numpy — the two members of an identical-workload pair, which is also what bounds the run-to-run spread there. |

The first answer is why this is a contained change rather than a sweep. The second is why the evidence
cannot be an ordinary corpus case. The third is why the hot loop gets a decision rule rather than a
reflex.

## Decisions

### D1 — Neumaier compensation, in a struct, not Kahan and not numpy's pairwise blocks

`src/DataNet.Metrics/Internal/CompensatedSum.cs`: a `struct` with `Add(double)` and a `Value` that returns
`sum + compensation`. A struct for the reason `IResidualKernel` beside it is one — the runtime specializes
it, the call is resolved statically, and the loops keep the shape they have now with no allocation per
column beyond the array they already build.

**Not Kahan.** Classic Kahan drops the correction when the incoming term is *larger* than the running sum.
That is precisely this shape: an accumulator that starts near zero taking terms near `1e9`. Neumaier
branches on which of the two is larger and keeps the low part either way, which is the whole reason it
exists.

**Not pairwise.** Reproducing numpy's blocked algorithm would make the difference zero by construction, but
it needs buffering or recursion over a weighted span where the loop today is a single pass. The goal is not
to reproduce numpy's *algorithm*; it is to agree with its *answer* inside the tolerance the corpus states.
Neumaier's error is `O(eps)` independent of `n`, where pairwise is `O(eps·log n)` — so this is at least as
accurate as the reference everywhere, and the corpus stays the judge.

**One comment the code must carry.** .NET never reassociates floating-point arithmetic — there is no
`-ffast-math` — so the compensation cannot be optimized away. That is the first question a reader arriving
from C or C++ will have, and leaving it unanswered invites someone to "simplify" the struct later.

### D2 — the three sites, and nothing else

`Outputs.WeightedMean`'s per-column accumulator and its `totalWeight`; `R2`'s centring pass and its
numerator/denominator pass, including that pass's own `totalWeight`; `ExplainedVariance`'s equivalent
passes. The per-column `double[]` becomes a `CompensatedSum[]` of the same length, so the allocation
profile does not change.

`MaxError` and `MedianAbsoluteError` are untouched: one compares, the other sorts, and neither accumulates
anything that could drift.

### D3 — the hot loop gets a rule, written before the number is known

Compensating `Outputs.WeightedMean` adds two or three flops per element to the path `mse_n1000000_k10`
measures at 1.00×. The damage the issue measured is in the *centring* mean, where an offset survives;
the seven kernel metrics sum quantities that are already differenced, where it does not.

So the rule, stated here so the measurement cannot be read to suit the outcome:

> All three sites are compensated **unless** the benchmark cost on `mse` at n = 1 000 000 exceeds **10%**
> *and* the measured relative error of the uncompensated kernel sum, on the ill-conditioned shape at
> n = 1 000 000, stays below **1e-12**. If both hold, `Outputs.WeightedMean` keeps its plain loop and this
> spec is amended with the two numbers that decided it.

Either outcome is a result. What is not allowed is deciding after seeing the number and calling the
decision principled.

### D4 — a procedural fixture, with probes compared bit for bit

The corpus keeps its shape; the large case carries **parameters, not data**. `tools/generate_oracles.py`
builds the arrays from a closed form, computes scikit-learn's answers, and stores the parameters, the
reference values, and a handful of **probe values at fixed indices**. The C# side rebuilds the arrays from
the same closed form and asserts the probes are bit-identical *before* scoring.

The form is deterministic and free of randomness — no shared generator to reconcile across two languages,
and every value comes from the same expression evaluated in the same order under IEEE-754 on both sides:

```text
y_true[i] = 1e9 + i * step,  step = 1e-2 / n
y_pred[i] = y_true[i] + ((i % 7) - 3) * step
```

The probes exist because the failure mode of a procedural fixture is silent: two sides that build slightly
different arrays compare their scores happily and prove nothing. A bit-exact check on the first value, the
last, and one from the middle turns that into a failed assertion naming the index.

### D5 — the mechanism is pinned by a test that owes the corpus nothing

The fixture proves parity with scikit-learn. It does not prove the compensation works, because a corpus
case can only ever say "these two agree". So `CompensatedSum` also gets a unit test on a shape whose exact
sum is known in closed form — a large offset plus a run of small equal terms — asserting that the
compensated result is exact where the naive one is measurably not. That test is what fails if someone
later "simplifies" the struct.

### D6 — the record

- `docs/guides/performance.md` — the four regression rows re-measured, before and after, with the machine
  named and the load stated, in the voice §"Regression metrics" already uses. A `perf`-shaped change to a
  hot loop without numbers is not something this repository accepts.
- `CHANGELOG.md` — the `DataNet.Metrics — 0.1.0` entry under `[Unreleased]` is **amended**, not joined by a
  *Fixed* entry. The regression metrics merged hours before this was written and no published package
  contains them, so there is no user to tell about a fix; there is a description of unreleased behaviour to
  keep true.
- `docs/equivalence.md` — one clause on the regression rows: the sums are compensated, so the answers are
  at least as accurate as numpy's pairwise reduction rather than merely close to it.
- **No ADR.** An ADR records a deliberate divergence from the reference. This is not one: it converges
  *toward* the reference, and the corpus remains the arbiter of whether it agrees.

## Evidence

- The unit test of D5 — the mechanism, against arithmetic rather than against another implementation.
- The procedural oracle case of D4 — parity with scikit-learn at a scale and a conditioning the existing
  corpus cannot reach.
- The 18 existing cases, unchanged in value: compensation moves their last bits, and they compare at
  `1e-9`, so they must all still pass. One that moves further is a result to understand before it is
  filed away.
- The benchmark table of D6, which is also what closes D3's rule.

## Out of scope

- The second regression lot — the three D² scores. This lands **before** them, on the issue's own
  instruction: each is defined in terms of a metric from the first lot and would replicate whatever loop it
  finds.
- The classification metrics. `ConfusionMatrix`, `Prf` and `MatrixSums` accumulate counts and small
  products, not offsets over spreads, and nothing has been measured to suggest otherwise.
- SIMD or any other change to how the loops are shaped. The one deliberate `net10.0`/`netstandard2.0`
  behavioural split in this repository is `VectorMath.Dot`; `CompensatedSum` is plain scalar arithmetic and
  must stay identical on both targets.

## Risks

- **The compensation changes existing outputs in their last bits.** Every current regression assertion
  compares at a tolerance, so this should be invisible — but a test elsewhere that compares a metric
  exactly would break, and the branch has to look rather than assume.
- **The procedural fixture can drift between the two languages.** D4's probes are the guard, and they are
  the difference between a fixture that fails loudly and one that quietly compares different data.
- **The benchmark window is noisy.** The existing regression rows were taken at a one-minute load of 8.05
  falling to 6.05, and the guide already says no conclusion should rest on an n = 1 000 row. The before/after
  for D3 must be taken in one window, interleaved, and must name its load — a comparison across two windows
  measures the machine.
