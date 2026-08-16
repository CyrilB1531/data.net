# FBeta.Score

The weighted harmonic mean of precision and recall, `beta` being what recall is worth relative to
precision.

<!-- docs-declaration -->

```csharp
public static double Score(ConfusionMatrix cm, double beta, Averaging average = Averaging.Binary, int posLabel = 1, ZeroDivision zeroDivision = ZeroDivision.Zero)
public static double Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, double beta, Averaging average = Averaging.Binary, int posLabel = 1, ZeroDivision zeroDivision = ZeroDivision.Zero, ReadOnlySpan<int> labels = default, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `cm` is a matrix already counted, or pass `yTrue` and `yPred`. `beta` is the
weight
of recall relative to precision and must be finite and non-negative. `average` reduces the
per-class
scores, `posLabel` is the class reported under `Averaging.Binary`, `zeroDivision` decides an
undefined score, `labels` fixes the label set and its order, and `sampleWeight` weights the
samples.

**Returns** — `double` in `[0, 1]`, larger meaning better.

**Exceptions** — `ArgumentOutOfRangeException` when `beta` is negative, `NaN` or infinite;
`ArgumentNullException` when `cm` is null; `ArgumentException` when `Averaging.Binary` is used on
more than two classes, or `posLabel` does not occur; `UndefinedMetricException` when the metric is
undefined and `zeroDivision` is `ZeroDivision.Throw`.

**Example** — the same filter scored twice: once as if a missed spam cost twice a false alarm,
once
the other way round.

```csharp
using Lodestar.Metrics;

int[] yTrue = [1, 1, 1, 1, 0, 0, 0, 0];
int[] yPred = [1, 1, 0, 0, 1, 0, 0, 0];

double recallHeavy = FBeta.Score(yTrue, yPred, 2.0);        // => 0.5263…
double precisionHeavy = FBeta.Score(yTrue, yPred, 0.5);     // => 0.625
```

**Remarks** — `beta` has a reading that makes it easy to choose: it is how many times more you
care
about recall than about precision. `beta = 2` is the standard "a miss is worse than a false alarm"
setting — screening for a disease, catching fraud — and `beta = 0.5` the standard opposite, where
a
false alarm is the expensive one, as in a filter that deletes mail. `beta = 1` is exactly `F1`,
and
`F1.Score` is the same call with the argument spelled into the name.

The two numbers above are the whole idea in one line: the recall-heavy score is *below* F1 because
this filter's recall is its weak side, and the precision-heavy score is above it.

Two traps. `beta = 0` is legal and collapses the metric to plain precision, which is a surprising
amount of nothing to get from a call that looks like it is measuring both; if that is what you
want,
say `Precision.Score` so the reader knows. And scikit-learn accepts `beta = inf` — the limit that
collapses to recall — where this refuses it with `ArgumentOutOfRangeException`; use
`Recall.Score`.

**Applies to** — net10.0, netstandard2.0.

**See also** — `FBeta.PerClass`, `F1.Score`, `Precision.Score`, `Recall.Score`,
the [Python equivalence table](../../../equivalence.md).
