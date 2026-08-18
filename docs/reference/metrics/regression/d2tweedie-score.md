# D2Tweedie.Score

The fraction of Tweedie deviance explained — `sklearn.metrics.d2_tweedie_score`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, double power = 0, ReadOnlySpan<double> sampleWeight = default, ZeroDivision zeroDivision = ZeroDivision.NaN)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, of the same length. `power`
selects which deviance is explained; [`TweedieDeviance`](tweediedeviance.md) has the table of regimes
and what each admits, and this applies exactly the same rules. `sampleWeight` is one weight per
sample, or empty — the default. `zeroDivision` decides the answer for fewer than two samples, the one
case scikit-learn leaves undefined; the default reproduces its `nan`.

**Returns** — `double`. `1` for a perfect prediction, `0` for one no better than predicting the
weighted average of the truth, and negative below that. Unbounded below.

**Exceptions** — `ArgumentOutOfRangeException` when `power` lies in `(0, 1)`. `ArgumentException`
when the lengths disagree, the input is empty or non-finite, or an operand is outside the regime's
domain. `UndefinedMetricException` when every truth is the same value — the constant baseline is
already perfect, so there is nothing to explain — or when there are fewer than two samples and
`zeroDivision` is `ZeroDivision.Throw`.

**Example** — the worked case, read as a normal and then as a Poisson.

```csharp
using Lodestar.Metrics;

double[] truth = [1.0, 2.0, 3.0, 4.0];
double[] predicted = [1.5, 2.5, 2.0, 4.5];

double normal = D2Tweedie.Score(truth, predicted);  // => 0.65
```

```csharp
using Lodestar.Metrics;

double[] truth = [1.0, 2.0, 3.0, 4.0];
double[] predicted = [1.5, 2.5, 2.0, 4.5];

double poisson = D2Tweedie.Score(truth, predicted, power: 1.0);  // => 0.6302…
```

The first is [`R2.Score`](r2-score.md) on the same input, to the last bit.

**Remarks** — scikit-learn warns and returns `nan` below two samples rather than refusing, which
`ZeroDivision.NaN` reproduces; pass `ZeroDivision.Zero`, `One` or `Throw` for another answer, exactly
as [`R2.Score`](r2-score.md) takes it for the same case.

A constant truth is the other undefined case and is **not** governed by `zeroDivision`: it always
throws, because the reference always raises there. [`D2AbsoluteError.Score`](d2absoluteerror-score.md)
answers `0` on that input instead, which is its own reference's behaviour.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`TweedieDeviance.Score`](tweediedeviance-score.md), [`R2.Score`](r2-score.md),
[`D2AbsoluteError.Score`](d2absoluteerror-score.md), the
[Python equivalence table](../../../equivalence.md).
