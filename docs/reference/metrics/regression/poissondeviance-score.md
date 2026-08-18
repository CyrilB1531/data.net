# PoissonDeviance.Score

The mean Poisson deviance — `sklearn.metrics.mean_poisson_deviance`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yPred, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `yTrue` is the true counts, which must be non-negative, and `yPred` the predicted
rates, which must be strictly positive and the same length. `sampleWeight` is one weight per sample,
or empty — the default — to weight every sample by `1`.

**Returns** — `double`, `0` for a perfect prediction and larger the worse it is. Unbounded above.

**Exceptions** — `ArgumentException` when the lengths disagree, the input is empty or holds a
non-finite value, a truth is negative, or a prediction is not strictly positive. The message is
scikit-learn's, and names `power=1`: this is [`TweedieDeviance`](tweediedeviance.md) at that power
and shares its refusal.

**Example** — four counts against a model's predicted rates.

```csharp
using Lodestar.Metrics;

double[] counts = [1.0, 2.0, 3.0, 4.0];
double[] rates = [1.5, 2.5, 2.0, 4.5];

double deviance = PoissonDeviance.Score(counts, rates);  // => 0.1967…
```

A zero count is fine, and contributes only the `ŷ − y` part of the term:

```csharp
using Lodestar.Metrics;

double[] counts = [0.0, 2.0, 3.0];
double[] rates = [1.0, 2.0, 3.0];

double withZero = PoissonDeviance.Score(counts, rates);  // => 0.6666…
```

**Remarks** — identical to [`TweedieDeviance.Score`](tweediedeviance-score.md) at `power: 1.0`,
asserted across the whole frozen corpus rather than on one pair.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`GammaDeviance.Score`](gammadeviance-score.md),
[`TweedieDeviance.Score`](tweediedeviance-score.md), [`D2Tweedie.Score`](d2tweedie-score.md), the
[Python equivalence table](../../../equivalence.md).
