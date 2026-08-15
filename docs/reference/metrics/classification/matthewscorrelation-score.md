# MatthewsCorrelation.Score

The correlation between the predicted and the true labels, in `[-1, 1]`.

<!-- docs-declaration -->

```csharp
public static double Score(ConfusionMatrix cm, ZeroDivision zeroDivision = ZeroDivision.Zero)
public static double Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, ZeroDivision zeroDivision = ZeroDivision.Zero, ReadOnlySpan<int> labels = default, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `cm` is a matrix already counted, or pass `yTrue` and `yPred`. `zeroDivision`
decides the answer when the denominator collapses. `labels` fixes the label set and its order, and
`sampleWeight` weights the samples.

**Returns** — `double` in `[-1, 1]`: `1` for a perfect prediction, `0` for one no better than
chance, and negative when the prediction is systematically inverted.

**Exceptions** — `ArgumentNullException` when `cm` is null; `ArgumentException` when the label
spans
disagree in length or are empty; `UndefinedMetricException` when the correlation is undefined and
`zeroDivision` is `ZeroDivision.Throw`.

**Example** — the spam filter, which F1 scores `0.5714…` and this scores far lower.

```csharp
using DataNet.Metrics;

int[] yTrue = [1, 1, 1, 1, 0, 0, 0, 0];
int[] yPred = [1, 1, 0, 0, 1, 0, 0, 0];

double correlation = MatthewsCorrelation.Score(yTrue, yPred);   // => 0.2581…
```

**Remarks** — this is the metric to reach for when you want a single number that cannot be gamed
by
predicting the majority class. Unlike F1 it reads **all four cells**, so a model that says "no" to
everything scores `0` here whatever the class balance is, and unlike accuracy it does not drift
upward as the majority grows. It is symmetric in the two classes as well: swapping which one you
call positive leaves the number alone, which F1 does not.

The `[-1, 1]` range carries information the others cannot. A negative score means the model is
anti-correlated with the truth — reliably wrong, which is a different failure from being random
and
usually points at an inverted label somewhere.

Two things about the undefined case. The denominator collapses when one input is constant — a
truth
with only one class, or a prediction with only one class — and scikit-learn hard-codes `0.0`
there.
This returns the same value by default, and additionally lets you ask to be told instead, with
`ZeroDivision.Throw`; that is an extension beyond parity, not a divergence in value. The
`ConfusionMatrix` overload scores only the classes the matrix holds, and `matthews_corrcoef` has
no
`labels` parameter, so for a restricted matrix there is no reference value to compare against.

The trap is reading it as a percentage. `0.2581…` is not "26% right"; correlations are not shares,
and a Matthews score of `0.3` is a considerably better model than the number looks next to an
accuracy of `0.625` on the same data.

**Applies to** — net10.0, netstandard2.0.

**See also** — `CohenKappa.Score`, `BalancedAccuracy.Score`, `F1.Score`,
the [Python equivalence table](../../../equivalence.md).
