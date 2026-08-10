# Regression metrics — design

**Issue:** [#92](https://github.com/CyrilB1531/data.net/issues/92) ·
**Date:** 2026-08-10 · **Package:** `DataNet.Metrics`

## Problem

Issue #61 shipped classification metrics at scikit-learn parity and deliberately
left regression out under the one-branch-one-concern rule. `DataNet.Metrics` can
score a classifier to the character and cannot score a regressor at all.

scikit-learn 1.9.0 exposes **17** regression metrics — not the "MSE, MAE, R² and
friends" the issue names. The full list, read from the installed package rather
than from memory:

`mean_squared_error`, `root_mean_squared_error`, `mean_absolute_error`,
`median_absolute_error`, `mean_absolute_percentage_error`,
`mean_squared_log_error`, `root_mean_squared_log_error`, `max_error`, `r2_score`,
`explained_variance_score`, `mean_pinball_loss`, `mean_tweedie_deviance`,
`mean_poisson_deviance`, `mean_gamma_deviance`, `d2_absolute_error_score`,
`d2_pinball_score`, `d2_tweedie_score`.

## Scope: eleven now, six next

This lot covers the first eleven. The remaining six — the three GLM deviances and
the three D² scores — are a second branch, and the split follows a dependency
rather than a convenience:

- `d2_absolute_error_score` is defined in terms of the mean absolute error.
- `d2_pinball_score` is defined in terms of the pinball loss.
- `d2_tweedie_score` is defined in terms of the Tweedie deviance.
- The three deviances are one kernel: Poisson is Tweedie at `power=1`, Gamma at
  `power=2`.

So the second lot builds on the first, and building the first alone is not an
arbitrary halving. A follow-up issue is opened when this spec is accepted.

## Public API

Eleven static types, one per metric, following the package's existing naming:
whole words except where the abbreviation *is* the conventional name, which is
the precedent `F1`, `FBeta` and `RocAuc` already set.

| Type | scikit-learn |
| --- | --- |
| `MeanSquaredError` | `mean_squared_error` |
| `RootMeanSquaredError` | `root_mean_squared_error` |
| `MeanAbsoluteError` | `mean_absolute_error` |
| `MedianAbsoluteError` | `median_absolute_error` |
| `MeanAbsolutePercentageError` | `mean_absolute_percentage_error` |
| `MeanSquaredLogError` | `mean_squared_log_error` |
| `RootMeanSquaredLogError` | `root_mean_squared_log_error` |
| `MaxError` | `max_error` |
| `R2` | `r2_score` |
| `ExplainedVariance` | `explained_variance_score` |
| `PinballLoss` | `mean_pinball_loss` |

`RootMeanSquaredError` is a separate type rather than a flag, because 1.9.0
**removed** `mean_squared_error`'s `squared=` parameter and replaced it with a
second function. `R2` rather than `RSquared` because `r2_score` is the name, and
because `F1` already settled that a conventional short name is acceptable here.
`PinballLoss` drops the `mean_` that the other ten keep, because the quantity the
field names is "the pinball loss" and the mean is how it is always taken;
`MeanPinballLoss` was considered and rejected as a transcription of the Python
identifier rather than of the concept.

### The four shapes of `multioutput`, as methods

scikit-learn's `multioutput` takes four values, and one of them — `raw_values` —
changes the return type. ADR 0016 already ruled on that exact shape for
`average=None`: an enum member that silently changes a method's return type
cannot exist, so it becomes a method. The same ruling applies, and it extends
further here:

```csharp
MeanSquaredError.Score(yTrue, yPred, outputCount)                 // uniform_average
MeanSquaredError.Score(yTrue, yPred, outputCount, outputWeights)  // an array of weights
MeanSquaredError.PerOutput(yTrue, yPred, outputCount)             // raw_values
R2.VarianceWeighted(yTrue, yPred, outputCount)                    // R2 and ExplainedVariance only
```

**No `MultiOutput` enum is introduced.** `variance_weighted` is accepted by only
two of the eleven — `r2_score` and `explained_variance_score`; the other nine
refuse it with an `InvalidParameterError`. An enum passed to all eleven would
therefore carry a member nine of them must reject at run time. As methods, the
invalid call does not compile: `MeanAbsoluteError.VarianceWeighted` does not
exist. The package already has the opposite shape — `Averaging.Binary` refused at
run time by multiclass ROC-AUC — and this is the case where it can be avoided.

Every entry point **except `MaxError`'s** also takes
`ReadOnlySpan<double> sampleWeight`, defaulting to empty, exactly as the
classification metrics do; `max_error` accepts no weights in scikit-learn either,
and the section below says why that asymmetry is deliberate. `PinballLoss` carries
one parameter none of the others have, `double alpha = 0.5`, which is the quantile
it scores — `mean_pinball_loss`'s own default, and the value at which the loss is
half the mean absolute error.

### `forceFinite` on two, `zeroDivision` on one

`R2` and `ExplainedVariance` both take `bool forceFinite = true`. Only `R2` takes
`zeroDivision`, and the asymmetry is measured rather than assumed:
`explained_variance_score([3], [5])` returns `1.0` — no warning, no `nan`, even
though the prediction is wrong. One residual has zero variance, so explained
variance is genuinely 1 by its own definition and `force_finite` covers it.
`r2_score([3], [3])` returns `nan` and warns.

So `ExplainedVariance` must **not** grow a `zeroDivision` parameter for symmetry's
sake: it has no undefined case to route. A test pins
`ExplainedVariance` at `1.0` on a single imperfect sample, so a later attempt to
harmonise the two signatures fails rather than silently changing a value.

### The 2-D input

Row-major flat span plus `outputCount`, defaulting to 1 — the shape
`RocAuc.MultiClass` already uses for its score matrix. A single-output caller
never sees any of it.

### `MaxError` is narrower, and that is parity

`max_error(y_true, y_pred)` takes neither `sample_weight` nor `multioutput` in
scikit-learn. `MaxError` therefore has one method and no overloads. The XML
documentation must say so explicitly, because otherwise the asymmetry reads as an
oversight rather than as fidelity.

## The degenerate cases

Every row below was measured against scikit-learn 1.9.0 in this repository's
`.venv-oracles`, not recalled.

| Case | scikit-learn | This design |
| --- | --- | --- |
| R² / explained variance, zero true variance, `force_finite=True` | `1.0` when the prediction is perfect, `0.0` otherwise | reproduce |
| the same, `force_finite=False` | `-inf` when imperfect, `nan` when perfect | reproduce; exposed as a `bool` |
| R² on fewer than two samples | warns, returns `nan` | a `zeroDivision` parameter, default `NaN` |
| MAPE with a zero true value | clamps `abs(y_true)` to `2**-52`, returning a large finite number | reproduce the clamp |
| MSLE / RMSLE with any target ≤ −1 | `ValueError` | `ArgumentException` naming the offending side |
| `PinballLoss` with `alpha` outside [0, 1] | `InvalidParameterError` | `ArgumentOutOfRangeException` |
| any non-finite input | `ValueError`, two distinct messages | `ArgumentException`, see below |
| weighted median absolute error | an *averaged* weighted percentile — it interpolates, rather than taking the value at the 50 % point: the mean of the first value whose cumulative weight reaches half the total and the one just past the last that stays at or below it | reproduce; see the invariant below |

Concretely: `r2_score([2,2,2], [2,2,2])` is `1.0`, `r2_score([2,2,2], [1,2,3])` is
`0.0`, and with `force_finite=False` those become `nan` and `-inf`.
`mean_absolute_percentage_error([0], [1])` is `4503599627370496.0`, which is
exactly `1 / 2**-52`.

**`forceFinite` and `zeroDivision` do not overlap on R², and the implementation
must not let them.** The two undefined cases are distinct and were measured
apart:

- **Fewer than two samples** — `r2_score([2], [2])` and `r2_score([2], [5])` are
  both `nan`, and **stay `nan` under `force_finite=False`**. `force_finite` never
  reaches this case; it is `zeroDivision`'s alone.
- **Two or more samples whose truth has zero variance** — `r2_score([2,2], [2,2])`
  is `1.0` and `r2_score([2,2], [2,3])` is `0.0`, becoming `nan` and `-inf` under
  `force_finite=False`. `zeroDivision` never reaches this case.

Routing the first through `forceFinite` would return `-inf` where scikit-learn
returns `nan`, and every ordinary fixture would still agree.

### `double.Epsilon` is the wrong constant, and the mistake is silent

MAPE's clamp uses numpy's **machine epsilon**, `np.finfo(np.float64).eps`, which
is `2**-52` ≈ `2.22e-16`. .NET's `double.Epsilon` is the smallest positive
subnormal, ≈ `4.94e-324` — **292 orders of magnitude away**. Both compile, both
"clamp", and only the oracle would say which is right. There is no built-in .NET
constant for machine epsilon, so the value is declared once, as a named constant,
with a comment saying what it is and what it is not.

### `ZeroDivision`'s default here has already been decided

The enum defaults to `Zero` for precision, recall and F1, because that is
scikit-learn's value there. It defaults to `NaN` for R², because that is
scikit-learn's value *here*. Same enum, different defaults, both faithful.

That is no longer this lot's decision to make.
[ADR 0020](../../decisions/0020-normalize-is-a-projection-not-a-parameter.md)
recorded it while issue #93 was in flight, listing `NaN` for R² among the four
defaults it enumerates and naming this issue as where it lands. This spec
therefore *implements* the default rather than arguing for it, and the new ADR
cites 0020 instead of restating it. `Throw` remains available, and remains the
package's answer to an `UndefinedMetricWarning` that a log swallows.

## Internal design

### `Inputs` gains a `double` overload, with a fourth check

`Inputs.Validate` today takes `ReadOnlySpan<int>` and makes three checks: equal
lengths, non-empty, and a matching `sampleWeight` length. The regression overload
takes `ReadOnlySpan<double>` and adds a fourth that classification never needed:
**non-finite inputs are refused**, reproducing scikit-learn's two distinct
messages — `Input contains NaN.` and
`Input contains infinity or a value too large for dtype('float64').`

That is an extra `O(samples)` pass per call. It is unavoidable for parity, and it
is stated in the XML documentation rather than left to be discovered in a profile.

### `outputCount` validation

The span's length must be a multiple of `outputCount`, the rule
`MultiClassRoc.Validate` already applies to its score matrix. The message names
both numbers. scikit-learn's own wording — `y_true and y_pred have different
number of output (2!=3)` — is not copied, because a flat span cannot have that
disagreement in the first place; what can go wrong here is a length that does not
divide.

### One reduction helper, and file discipline

A single internal helper reduces a per-output array to a scalar under
`uniform_average`, an explicit weight array, or `variance_weighted`. Nothing else
is shared: each metric owns its own pass over the data.

Eleven small public files plus short internal helpers — **not** one large
`RegressionMetrics.cs`. `Internal/MultiClassRoc.cs` reached 704 lines during #86
and was the file that cost the most fix rounds; that is a pattern to avoid rather
than repeat.

### No shared accumulator, deliberately

ADR 0016 made `ConfusionMatrix` public because four classification metrics read
one `O(samples)` pass, and a caller reaching only `Precision.Score` paid that pass
four times. The argument does not transfer: an accumulator would serve six of the
eleven — median absolute error needs the whole residual vector and a sort, MAPE
its own clamped sum, pinball its own — so the API would teach a shortcut that
stops halfway. And unlike `sampleWeight`, **adding one later is not a breaking
change**, so it can wait for a caller who wants it.

## Oracle validation

### Non-finite values in a corpus: the plumbing now exists

This section was written expecting to invent it. It no longer has to. Issue #93
landed the identical mechanism on 2026-08-10, one branch ahead of this one:

- `_finite_or_name` in `tools/generate_oracles.py` encodes a non-finite as the
  string `"NaN"`, `"Infinity"` or `"-Infinity"`;
- `json.dump(…, allow_nan=False)` makes a non-finite that nobody encoded on
  purpose fail generation loudly, rather than writing a bare `NaN` — not JSON,
  and refused by `System.Text.Json` at load time;
- `OracleLoader.Number(JsonElement)` decodes the three names back on the C# side,
  and throws on any other string.

[ADR 0020](../../decisions/0020-normalize-is-a-projection-not-a-parameter.md)
anticipated this lot by name: *"Issue #92 needs the identical plumbing for R²,
and consumes this rather than rebuilding it."* So this lot writes no encoder, no
decoder and no writer flag. It freezes `nan` and `-inf` through machinery that is
already under test, and `classification_metrics.json` already carries six `"NaN"`
values — Cohen's kappa in its three weightings, on two fixtures — proving the
path works end to end.

Keeping the degenerate cases out of the corpus and asserting them by hand in C#
was the alternative, and it is still rejected: those values *are* reference
outputs, and they are exactly where divergence hides. Holding them outside the
oracle would assume what this repository requires to be proven.

### The comparison rule cannot be a single absolute tolerance

`CONTRIBUTING.md` prescribes `1e-9` for floating-point oracle comparisons. The
values in this lot span `0.0` to `4.5e15`, where an absolute `1e-9` is
meaningless. Therefore:

- **relative** comparison for ordinary values, at `1e-9` — that is,
  `abs(actual - expected) <= 1e-9 * abs(expected)`, which reduces to the existing
  absolute rule for values near 1 and stays meaningful at `4.5e15`,
- **exact** comparison for the defined degenerate values — `1.0`, `0.0`, `NaN`,
  `-inf` — because scikit-learn does not approximate them, it defines them.
  `NaN` is compared with `double.IsNaN`, not with `==`, which is false for `NaN`
  against itself.

### Corpus shape

`tools/generate_oracles.py` gains a `generate_regression()` section writing
`tests/oracles/regression.json`, following the shape of `roc_auc.json`: a list of
cases, each with a fixture name, `y_true`, `y_pred`, `sample_weight`,
`output_count`, and a `values` map keyed `metric|shape[|flags]` — for example
`mse|uniform`, `mse|raw`, `r2|uniform|force_finite`, `r2|variance_weighted`.

Fixtures cover single-output and multi-output, weighted and unweighted, plus one
case per degenerate row above. The generator is seeded, because the "Oracles are
reproducible" CI job regenerates every corpus and compares.

### Six invariants the oracle does not give

These constrain the implementation from the inside, where the oracle constrains
it from the outside:

1. The weighted median absolute error on uniform weights equals the unweighted
   one — including the average of the two middle values for an even sample count.
   Measured: both are `3.0` for residuals `[0, 2, 4, 10]`.
2. `RootMeanSquaredError` equals the square root of `MeanSquaredError`.
3. `RootMeanSquaredLogError` equals the square root of `MeanSquaredLogError`.
4. `PerOutput` at `outputCount = 1` equals `Score`.
5. `Score` under uniform averaging equals the mean of `PerOutput`.
6. `PinballLoss` at `alpha = 0.5` is half the mean absolute error. Measured:
   both sides give `0.25` on `y_true = [3, -0.5, 2, 7]`,
   `y_pred = [2.5, 0, 2, 8]`. This is the one invariant that ties the quantile
   loss to a metric already in the lot, so an `alpha` wired into the wrong side
   of the `max` fails it.

### Refusals under test

Non-finite input on either side; MSLE with a target ≤ −1 on the truth side *and*
on the prediction side; `alpha` outside [0, 1]; an `outputCount` that does not
divide the length; empty input; disagreeing lengths. The netstandard2.0 mirror
replays all of it.

## Benchmarks

The cross-language comparison is extended. Issue #61 established processor time
against scikit-learn as the **merge gate** for this package — 29 operations, every
one at or above 1× — and a lot that skips it breaks that pattern and ships eleven
metrics with no measured claim. `bench/python/bench_metrics.py` and
`bench/DataNet.Text.Benchmarks/CrossLang/MetricsCrossLang.cs` each gain a few
regression operations over the existing corpus shapes. The harness already exists,
so the cost is low and the consistency is preserved.

Issue #93 has since added 18 rows to that table and set the shape a later lot
follows: its own section in `docs/guides/performance.md`, and **its own load
average recorded beside it** rather than inherited from the original run's
conditions, because the two runs did not share a machine state. This lot does the
same. A row below 1× is a finding to report, not a run to repeat until it passes.

## Plumbing that would fail the build if forgotten

- `samples/DataNet.Sample/PackagingGate.cs` requires a **member reference** to
  every exported public type. Eleven new ones need a `Lot6Regression.cs` and its
  call from `Program.cs` — eleven exercised calls, not decorative ones.
- Eleven rows in `docs/equivalence.md`, each naming its scikit-learn function and
  its divergences: `MaxError`'s absent parameters, `ZeroDivision`'s different
  default, MAPE's epsilon clamp.
- The ADR is **`0021`** — `0018` is issue #86's parallelism record, `0019` is
  #107's analyser record, `0020` is #93's. Check `ls docs/decisions/` before
  writing anyway: this document has now named the wrong number twice, by standing
  still while other branches landed. It records **two** decisions, not four: the
  two-lot split, and `multioutput` as methods rather than an enum.
  `ZeroDivision`'s per-metric default and the corpus encoding of non-finite
  values are both already in 0020, and are cited rather than restated.
- The CHANGELOG heading depends on a release that has not happened yet.
  `DataNet.Metrics` is at `0.1.0`, unreleased. If `0.1.0` ships before this lands,
  the entry goes under `0.2.0` and replacing any signature becomes a genuine
  break; otherwise it joins `0.1.0`. Resolve it by reading `Version.props` and
  the published feed at implementation time, not now.

## Out of scope

The three GLM deviances and the three D² scores, which are the second lot and get
their own issue. A metrics guide under `docs/guides/`: there is none today, and
writing one here would mean documenting nineteen metrics rather than eleven,
including the eight classification metrics that shipped in #61 — a gap older and
wider than this lot, and widening this branch to cover it is what
`CONTRIBUTING.md` forbids. It gets its own issue too.
