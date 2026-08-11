# 0021 — Eleven regression metrics first, and `multioutput` as a method rather than an enum

**Status:** accepted · **Date:** 2026-08-10

## Context

[Issue #92](https://github.com/CyrilB1531/data.net/issues/92) asks for
scikit-learn's regression metrics in `DataNet.Metrics`, beside the
classification surface [`0016`](0016-metrics-package-placement.md) placed there.
`sklearn.metrics` exposes seventeen of them, and they are not seventeen
independent functions: six are defined in terms of another six, and three of
those six are one formula under three names.

Two choices had to be made before the first line was written, and both are hard
to reverse once the package is published: **how many of the seventeen this lot
carries**, and **how `multioutput=` — a keyword that takes a string, a
`None`, or an array — crosses into a language with no such parameter**.

Two further questions that came up while writing it are *not* decided here.
`ZeroDivision`'s per-metric default and the encoding of non-finite values in an
oracle corpus were both settled in
[`0020`](0020-normalize-is-a-projection-not-a-parameter.md), which names this
issue as where they would first be used. R² takes `ZeroDivision.NaN` and the
corpora carry `"NaN"`, `"Infinity"` and `"-Infinity"` as strings because 0020
says so, not because this lot decided it again.

## Decision

### Eleven metrics now, six on a second branch

This lot ships `MeanSquaredError`, `RootMeanSquaredError`, `MeanAbsoluteError`,
`MedianAbsoluteError`, `MeanAbsolutePercentageError`, `MeanSquaredLogError`,
`RootMeanSquaredLogError`, `MaxError`, `R2`, `ExplainedVariance` and
`PinballLoss`. It does not ship `d2_absolute_error_score`, `d2_pinball_score`,
`d2_tweedie_score`, `mean_poisson_deviance`, `mean_gamma_deviance` or
`mean_tweedie_deviance`.

The line is drawn along a dependency, not along a budget. Each of the three D²
scores is a ratio whose numerator and denominator are computed with a metric
from the first group: `d2_absolute_error_score` with the mean absolute error,
`d2_pinball_score` with the pinball loss, `d2_tweedie_score` with the Tweedie
deviance. And the three deviances are one kernel wearing three names — Poisson
is Tweedie at `power=1`, Gamma is Tweedie at `power=2` — so writing any one of
them well means writing the general form, with its own domain restrictions per
power. The second group is therefore *cheaper* after the first exists and
*not* cheaper alongside it.

The rejected alternative is all seventeen in one change. It costs no less
implementation work in total, and it buys a review surface of seventeen public
types, seventeen oracle corpora and one reviewer, on a package whose
correctness argument is entirely "the frozen corpus agrees". A conformance
defect is found by reading a divergence against a reference, and that reading
does not parallelise inside one pull request. The split also lets the eleven be
*used* — and their `multioutput` shape criticised — before six more types adopt
it.

### `multioutput` is a choice of method, not a member of an enum

scikit-learn spells the reduction over outputs as one keyword with four kinds of
value: `"uniform_average"` (the default), `"raw_values"`, `"variance_weighted"`,
or an array of per-output weights. The obvious transcription is a
`Multioutput` enum. It was rejected, and the surface is instead:

```text
Score(yTrue, yPred, outputCount, sampleWeight, outputWeights)  // uniform_average, or an array
PerOutput(yTrue, yPred, outputCount, sampleWeight)             // raw_values
VarianceWeighted(yTrue, yPred, outputCount, sampleWeight)      // variance_weighted, on R2 and ExplainedVariance only
```

Three things break the enum, and each on its own is enough.

**`raw_values` changes the return type.** Every other value of the keyword
reduces to one `double`; `raw_values` yields one per output. That is exactly the
argument [`0016`](0016-metrics-package-placement.md) already made about
`average=None`, which became `PerClass`. An enum member that silently changes
the return type of its method cannot exist in C#, so this one becomes
`PerOutput`, for the same reason and with the same shape.

**`variance_weighted` is accepted by two of the eleven.** `r2_score` and
`explained_variance_score` take it; the other nine raise on it, because the
per-output variance of the truth is not a quantity they compute. An enum would
therefore carry a member that nine of eleven metrics must reject at run time —
one that cannot be reached from the scores alone either, since the weights are
the denominators of the very pass that produced them. As a method,
`MeanAbsoluteError.VarianceWeighted` does not exist, and the invalid call fails
to compile instead of throwing.

**An array of weights is data, not a name.** `multioutput=[0.3, 0.7]` carries
values, and an enum member cannot. It becomes an optional
`ReadOnlySpan<double> outputWeights` on `Score`, empty for the uniform average
— so the enum would have had to coexist with the parameter anyway, and a caller
could then set both and mean two things at once.

The package already contains the shape this avoids. `Averaging.Binary` is a
member of an enum that `RocAuc.MultiClass` refuses at run time, because binary
averaging is meaningless over more than two classes — a compile-time-valid call
that throws. That was unavoidable there: `Averaging` is genuinely shared by
metrics that differ in which members they accept. Here it *is* avoidable, and
the cost of avoiding it is a wider-looking API — three method names instead of
one keyword — in exchange for every call that compiles being a call that runs.

## Consequences

- **Eleven types with up to three entry points each, instead of eleven with
  one.** The surface reads wider than scikit-learn's, and a reader coming from
  Python has to learn that `multioutput=` is spelled by choosing a method.
  [`docs/equivalence.md`](../equivalence.md) carries that mapping on every one of
  the eleven rows, which is where a migrating reader looks first.
- **`samples/DataNet.Sample` gains `Lot6Regression.cs`**, and it is not
  optional: ADR [`0009`](0009-sample-consumes-a-local-feed.md)'s packaging gate
  fails the build when an exported type has no member reference from the sample,
  and this lot adds eleven exported types and no enum. The sample also prints
  every number through `CultureInfo.InvariantCulture`, so the output the guides
  quote does not change with the machine's decimal separator.
- **`median_absolute_error` is the one operation in the lot still below the
  package's 1× processor-time gate**, at 0.80–0.90× against scikit-learn at
  n=100 000 and n=1 000 000. That is after a rewrite, not before one: the first
  measurement was 0.19×, because the implementation sorted the whole residual
  array where NumPy's `median` selects with introselect. Replacing the sort with
  a median-of-three quickselect under an introselect budget recovered most of
  the gap and did not close it — scikit-learn is still marginally faster there,
  and this ADR records that rather than rounding it up. The six measured rows,
  both passes, and the load the machine was under are in
  [the performance guide](../guides/performance.md#regression-metrics--mse-mae-median_ae-r2-issue-92).
- **The second lot inherits this shape.** `d2_absolute_error_score`,
  `d2_pinball_score`, `d2_tweedie_score` and the three deviances all take
  `multioutput` too, so they will arrive as `Score` / `PerOutput` pairs with no
  further decision to make — and `d2_tweedie_score` will arrive after the
  Tweedie deviance it is defined in terms of, which is the dependency the split
  was drawn along.
- **`R2.PerOutput` diverges in shape on one degenerate input**, and it is
  recorded in the equivalence table rather than hidden: with fewer than two
  samples and more than one output it returns one `NaN` per output, where
  `r2_score` returns a single scalar `nan` before it consults `multioutput` at
  all. No value differs — every scalar-returning path still answers `nan` — and
  a one-element array would break `PerOutput`'s own contract that it returns one
  value per output. `ExplainedVariance.PerOutput` has no such case, because it
  has no fewer-than-two-samples rule to apply.
