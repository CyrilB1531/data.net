# Recall.Score

True positives over the true size of the class, reduced to one number by `average`.

<!-- docs-declaration -->

```csharp
public static double Score(ConfusionMatrix cm, Averaging average = Averaging.Binary, int posLabel = 1, ZeroDivision zeroDivision = ZeroDivision.Zero)
public static double Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, Averaging average = Averaging.Binary, int posLabel = 1, ZeroDivision zeroDivision = ZeroDivision.Zero, ReadOnlySpan<int> labels = default, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `cm` is a matrix already counted, or pass `yTrue` and `yPred`. `average` is how
the
per-class scores are reduced, `Averaging.Binary` by default. `posLabel` is the class reported
under
`Averaging.Binary`. `zeroDivision` decides what comes back when the class has no true samples at
all. `labels` fixes the label set and its order, and `sampleWeight` weights the samples.

**Returns** — `double` in `[0, 1]`, larger meaning fewer misses.

**Exceptions** — `ArgumentNullException` when `cm` is null; `ArgumentException` when
`Averaging.Binary` is used on more than two classes, or `posLabel` does not occur;
`UndefinedMetricException` when the class has no true samples and `zeroDivision` is
`ZeroDivision.Throw`.

**Example** — four messages were spam and the filter caught two.

```csharp
using Lodestar.Metrics;

int[] yTrue = [1, 1, 1, 1, 0, 0, 0, 0];
int[] yPred = [1, 1, 0, 0, 1, 0, 0, 0];

double recall = Recall.Score(yTrue, yPred);   // => 0.5
```

**Remarks** — report this when a miss is the expensive mistake: an undetected tumour, a fraud that
went through, a security alert nobody raised. It answers "of the things that were really there,
how
many did we get", and says nothing about how much noise it made getting them.

Which is the mirror trap of precision's: **recall alone is trivially gamed too.** Flag everything
and
recall is `1.0`. The pair is the claim; either one on its own is a half-sentence.

Recall is also the metric the other pages here are built out of: `BalancedAccuracy.Score` is the
macro average of per-class recall, and a `Normalization.True` confusion matrix has per-class
recall
on its diagonal. If you are already looking at one of those, you have this.

The undefined case is a class with no true samples — the denominator is its support. That returns
`0.0` by default, which is scikit-learn's value, and `ZeroDivision.NaN` is what keeps such a class
out of a macro average instead of scoring it zero.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Recall.PerClass`, `Precision.Score`, `F1.Score`, `BalancedAccuracy.Score`,
the [Python equivalence table](../../../equivalence.md).
