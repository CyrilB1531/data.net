# R2.Score

One score for the whole prediction: `1` minus the residual variance over the truth's variance.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, int outputCount = 1, ReadOnlySpan<double> sampleWeight = default, ReadOnlySpan<double> outputWeights = default, bool forceFinite = true, ZeroDivision zeroDivision = ZeroDivision.NaN)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, row-major when there is
more
than one output. `outputCount` is how many outputs each row holds, `sampleWeight` weights the
rows,
and `outputWeights` weights the outputs in the reduction. `forceFinite` answers the case where the
truth has no variance over two or more samples, clamping to `1` or `0` instead of `nan` or `-inf`.
`zeroDivision` answers the *different* case of fewer than two samples, and defaults to
`ZeroDivision.NaN`, which is scikit-learn's value.

**Returns** — `double` at most `1`: `1` for a perfect prediction, `0` for one exactly as good as
always predicting the mean, and negative — with no lower bound — for one that is worse.

**Exceptions** — `ArgumentException` when a length disagrees with the shape, the input is empty,
or
it holds a non-finite value; `ArgumentOutOfRangeException` when `outputCount` is below one;
`UndefinedMetricException` when there are fewer than two samples and `zeroDivision` is
`ZeroDivision.Throw`.

**Example** — four predictions, close but not exact.

```csharp
using DataNet.Metrics;

double[] yTrue = [3.0, -0.5, 2.0, 7.0];
double[] yPred = [2.5, 0.0, 2.0, 8.0];

double score = R2.Score(yTrue, yPred);   // => 0.9486…
```

**Remarks** — this is the number to report when a reader has to judge the model without knowing
the
target's units, and the only one on this page that can rank two different problems. Its zero point
is
the thing to hold on to: `0` is what you get by ignoring the inputs entirely and predicting the
mean
of the truth every time. A model that scores `0.1` is barely doing anything; a model that scores
below `0` is worse than that baseline, which is possible and much more common on held-out data
than
people expect. There is no floor — a bad enough model scores `-14`.

The trap that catches people moving from `ExplainedVariance.Score` is that **`R2` charges for a
constant bias.** Predicting `y + 1` for every sample tracks the truth perfectly and scores `-0.5`
here, where explained variance scores `1`. That is the intended behaviour: an offset is a real
error
unless you are going to remove it.

The two undefined cases are deliberately kept apart and must not be merged.

- **Fewer than two samples** is `zeroDivision`'s case: the truth has no variance to compare against
  because there is only one of it, and scikit-learn returns `nan` here whatever `force_finite`
says.
  `R2.Score([2.0], [1.0])` is `NaN`.
- **A constant truth over two or more samples** is `forceFinite`'s case. With `forceFinite: true` —
  the default — a perfect prediction of that constant scores `1` and any other scores `0`; with
  `false` you get `nan` and `-inf` instead.

[Decision
0026](../../../decisions/0026-r2-and-explainedvariance-split-their-undefined-cases-differently.md)
has the argument for keeping them separate. Both passes are Neumaier-compensated, which is
load-bearing on an ill-conditioned target: a sequential sum was measured 357 times outside the
oracle's tolerance —
[decision 0033](../../../decisions/0033-compensated-sum-is-neumaiers-variant.md).

**Applies to** — net10.0, netstandard2.0.

**See also** — `R2.PerOutput`, `R2.VarianceWeighted`, `ExplainedVariance.Score`,
[decision
0026](../../../decisions/0026-r2-and-explainedvariance-split-their-undefined-cases-differently.md),
[the ZeroDivision entry](../classification/zerodivision.md),
the [Python equivalence table](../../../equivalence.md).
