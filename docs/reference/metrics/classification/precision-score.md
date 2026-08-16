# Precision.Score

True positives over everything predicted into the class, reduced to one number by `average`.

<!-- docs-declaration -->

```csharp
public static double Score(ConfusionMatrix cm, Averaging average = Averaging.Binary, int posLabel = 1, ZeroDivision zeroDivision = ZeroDivision.Zero)
public static double Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, Averaging average = Averaging.Binary, int posLabel = 1, ZeroDivision zeroDivision = ZeroDivision.Zero, ReadOnlySpan<int> labels = default, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `cm` is a matrix already counted, or pass `yTrue` and `yPred`. `average` is how
the
per-class scores are reduced, `Averaging.Binary` by default. `posLabel` is the class reported
under
`Averaging.Binary`. `zeroDivision` decides what comes back when nothing at all was predicted into
the class. `labels` fixes the label set and its order, and `sampleWeight` weights the samples.

**Returns** — `double` in `[0, 1]`, larger meaning fewer false alarms.

**Exceptions** — `ArgumentNullException` when `cm` is null; `ArgumentException` when
`Averaging.Binary` is used on more than two classes, or `posLabel` does not occur;
`UndefinedMetricException` when nothing was predicted into the class and `zeroDivision` is
`ZeroDivision.Throw`.

**Example** — the filter flagged three messages as spam and two of them were.

```csharp
using Lodestar.Metrics;

int[] yTrue = [1, 1, 1, 1, 0, 0, 0, 0];
int[] yPred = [1, 1, 0, 0, 1, 0, 0, 0];

double precision = Precision.Score(yTrue, yPred);   // => 0.6666…
```

**Remarks** — report this when a false alarm is the expensive mistake: mail deleted that was not
spam, a customer wrongly declined, a page taken down that was fine. It answers "when this thing
fires, can I trust it", and says nothing at all about how much it missed.

Which is the trap, and it is not subtle: **precision alone is trivially gamed.** A model that
flags
exactly one sample, and is right about it, has a precision of `1.0`. Precision is only a claim
about
a model when it is quoted next to a recall, or folded into `F1.Score`, which is why every report
on
this page carries both.

The undefined case is worth setting deliberately. A class nothing was predicted into has no
precision — the denominator is zero — and by default that returns `0.0`, which is scikit-learn's
value and reads in a report as "terrible" rather than as "not asked". `ZeroDivision.NaN` keeps it
out of a macro average honestly; `ZeroDivision.Throw` tells you rather than letting a silent zero
drag a number down.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Precision.PerClass`, `Recall.Score`, `F1.Score`, `ZeroDivision`,
the [Python equivalence table](../../../equivalence.md).
