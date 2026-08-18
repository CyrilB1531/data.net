# GammaDeviance.Score

The mean gamma deviance — `sklearn.metrics.mean_gamma_deviance`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted values, of the same length, and both
must be **strictly positive**. `sampleWeight` is one weight per sample, or empty — the default — to
weight every sample by `1`.

**Returns** — `double`, `0` for a perfect prediction and larger the worse it is. Unbounded above,
and unchanged by rescaling both arguments together.

**Exceptions** — `ArgumentException` when the lengths disagree, the input is empty or holds a
non-finite value, or either operand is not strictly positive — including a zero truth, which
[`PoissonDeviance.Score`](poissondeviance-score.md) accepts and this does not. The message is
scikit-learn's, naming `power=2`.

**Example** — the same four samples the Poisson page scores, read as a positive quantity instead.

```csharp
using Lodestar.Metrics;

double[] truth = [1.0, 2.0, 3.0, 4.0];
double[] predicted = [1.5, 2.5, 2.0, 4.5];

double deviance = GammaDeviance.Score(truth, predicted);  // => 0.0982…
```

Scaling both by ten leaves it alone, which is the property the type exists for:

```csharp
using Lodestar.Metrics;

double[] truth = [10.0, 20.0, 30.0, 40.0];
double[] predicted = [15.0, 25.0, 20.0, 45.0];

double scaled = GammaDeviance.Score(truth, predicted);  // => 0.0982…
```

**Remarks** — identical to [`TweedieDeviance.Score`](tweediedeviance-score.md) at `power: 2.0`,
asserted across the whole frozen corpus.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`PoissonDeviance.Score`](poissondeviance-score.md),
[`TweedieDeviance.Score`](tweediedeviance-score.md), [`D2Tweedie.Score`](d2tweedie-score.md), the
[Python equivalence table](../../../equivalence.md).
