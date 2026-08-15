# Accuracy.Score

The share of samples whose predicted label equals the true one.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, bool normalize = true, ReadOnlySpan<double> sampleWeight = default)
public static double Score(ConfusionMatrix cm, bool normalize = true)
```

**Parameters** — `yTrue` and `yPred` are the true and predicted labels, one per sample and the
same
length. `cm` is the alternative to both: a matrix already counted, which is what you pass when
several metrics are being read off one dataset. `normalize` chooses between the fraction (`true`,
the default) and the raw weight of the correct samples (`false`). `sampleWeight` gives each sample
its own weight; omit it and every sample counts 1.

**Returns** — `double` in `[0, 1]` when `normalize` is `true`, `1` meaning every sample was right.
With `normalize: false` it is a count instead — a weight, not a fraction, and unbounded.

**Exceptions** — `ArgumentException` when the two label spans disagree in length or are empty;
`ArgumentNullException` when `cm` is null.

**Example** — four spam messages and four legitimate ones; the filter caught two of the four and
raised one false alarm.

```csharp
using DataNet.Metrics;

int[] yTrue = [1, 1, 1, 1, 0, 0, 0, 0];
int[] yPred = [1, 1, 0, 0, 1, 0, 0, 0];

double share = Accuracy.Score(yTrue, yPred);   // => 0.625
```

**Remarks** — this is the right metric when the classes are roughly balanced and every mistake
costs
about the same. Both conditions matter, and the first one is where people get hurt.

The trap has a number attached. Take ten samples of which two belong to the class you care about,
predict the majority class for all ten, and this returns `0.8` while
`BalancedAccuracy.Score` returns `0.5` — the score a coin gets. Accuracy is a weighted average of
the per-class recalls in which each class is weighted by how common it is, so a class that is 2%
of
the data moves it by at most 0.02. If your positive class is rare, this number is measuring the
negative class and telling you about it.

The `ConfusionMatrix` overload has one behaviour of its own worth knowing. It is accuracy over the
samples the matrix **kept**: a matrix built with an explicit `labels` subset drops every sample
whose true or predicted label falls outside that subset, so on a three-class problem restricted to
two labels this can read `0.75` where the same data scored over every sample reads `0.7142…`. That
is not a bug in either number — they are answers to different questions — but only the span
overload matches `accuracy_score`.

**Applies to** — net10.0, netstandard2.0.

**See also** — `BalancedAccuracy.Score`, `ConfusionMatrix.Compute`,
`ClassificationReport.Compute`,
the [Python equivalence table](../../../equivalence.md).
