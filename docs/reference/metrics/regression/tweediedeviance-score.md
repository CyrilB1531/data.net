# TweedieDeviance.Score

The mean Tweedie deviance at one power — `sklearn.metrics.mean_tweedie_deviance`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, double power = 0, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, of the same length.
`power` selects the distribution whose deviance is taken: `0` the normal, `1` the Poisson, `2` the
gamma, `3` the inverse gaussian, and anything at most `0` or at least `1` in between. `sampleWeight`
is one weight per sample, or empty — the default — to weight every sample by `1`.

**Returns** — `double`, `0` for a perfect prediction and larger the worse it is. Unbounded above,
and not comparable across powers: the same pair scores `0.4375` at power `0`, `0.1967…` at `1` and
`0.0982…` at `2`.

**Exceptions** — `ArgumentOutOfRangeException` when `power` lies in the open interval `(0, 1)`,
which names no distribution. `ArgumentException` when the lengths disagree, the input is empty or
holds a non-finite value, or an operand falls outside the regime's domain —
[the table on the type page](tweediedeviance.md) has all four regimes, and the message is
scikit-learn's own sentence, naming the power.

**Example** — the same four samples read as three different distributions.

```csharp
using Lodestar.Metrics;

double[] truth = [1.0, 2.0, 3.0, 4.0];
double[] predicted = [1.5, 2.5, 2.0, 4.5];

double normal = TweedieDeviance.Score(truth, predicted);  // => 0.4375
```

```csharp
using Lodestar.Metrics;

double[] truth = [1.0, 2.0, 3.0, 4.0];
double[] predicted = [1.5, 2.5, 2.0, 4.5];

double poisson = TweedieDeviance.Score(truth, predicted, power: 1.0);  // => 0.1967…
```

The number falls as the power rises here because a higher power expects the variance to grow with
the mean, and these predictions miss most where the values are largest.

**Remarks** — at `power` `0` this is [`MeanSquaredError.Score`](meansquarederror-score.md) exactly,
since that regime's deviance is the squared residual. At `1` and `2` it is
[`PoissonDeviance.Score`](poissondeviance-score.md) and
[`GammaDeviance.Score`](gammadeviance-score.md), which the test suite asserts across the whole
corpus rather than on one pair.

`y × log(y / ŷ)` is taken as `0` when `y` is `0`, which is its limit and what numpy's `xlogy`
gives — that is what makes a zero truth legal in the `[1, 2)` regimes at all.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`D2Tweedie.Score`](d2tweedie-score.md),
[`PoissonDeviance.Score`](poissondeviance-score.md), [`GammaDeviance.Score`](gammadeviance-score.md),
the [Python equivalence table](../../../equivalence.md).
