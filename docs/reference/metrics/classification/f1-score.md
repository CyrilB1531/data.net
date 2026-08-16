# F1.Score

The harmonic mean of precision and recall, reduced to one number by `average`.

<!-- docs-declaration -->

```csharp
public static double Score(ConfusionMatrix cm, Averaging average = Averaging.Binary, int posLabel = 1, ZeroDivision zeroDivision = ZeroDivision.Zero)
public static double Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, Averaging average = Averaging.Binary, int posLabel = 1, ZeroDivision zeroDivision = ZeroDivision.Zero, ReadOnlySpan<int> labels = default, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `cm` is a matrix already counted, or pass `yTrue` and `yPred`. `average` is how
the
per-class scores are reduced, `Averaging.Binary` by default. `posLabel` is the class reported
under
`Averaging.Binary`, `1` by default. `zeroDivision` decides what an undefined score becomes.
`labels`
fixes the label set and its order, and `sampleWeight` weights the samples.

**Returns** — `double` in `[0, 1]`, larger meaning better.

**Exceptions** — `ArgumentNullException` when `cm` is null; `ArgumentException` when
`Averaging.Binary` is used on more than two classes, or `posLabel` does not occur;
`UndefinedMetricException` when the metric is undefined and `zeroDivision` is
`ZeroDivision.Throw`.

**Example** — the spam filter, whose precision is `0.6666…` and whose recall is `0.5`.

```csharp
using Lodestar.Metrics;

int[] yTrue = [1, 1, 1, 1, 0, 0, 0, 0];
int[] yPred = [1, 1, 0, 0, 1, 0, 0, 0];

double f1 = F1.Score(yTrue, yPred);   // => 0.5714…
```

**Remarks** — this is the number to report when you have one class you care about, both kinds of
mistake matter, and you do not want to argue about which matters more. Being a *harmonic* mean is
the whole design: it sits much closer to the smaller of the two than an ordinary average would, so
a
model with precision `1.0` and recall `0.01` scores `0.0198`, not `0.505`. You cannot buy an F1 by
being perfect at one thing.

The trap is that F1 is not symmetric in the way people assume it is. It ignores `TN` completely,
so
it is not invariant under swapping which class you call positive: on the same predictions,
`posLabel: 0` gives `0.6666…` here where `posLabel: 1` gives `0.5714…`. Fix which class is
positive
before you compare two models, and say so in the report.

`Averaging.Binary` is the default and throws on more than two classes rather than guessing, so a
multiclass call has to name `Macro`, `Weighted` or `Micro`. For a beta other than 1 — recall worth
more than precision, or less — use `FBeta.Score` rather than post-processing this.

**Applies to** — net10.0, netstandard2.0.

**See also** — `F1.PerClass`, `FBeta.Score`, `Precision.Score`, `Recall.Score`,
the [Python equivalence table](../../../equivalence.md).
