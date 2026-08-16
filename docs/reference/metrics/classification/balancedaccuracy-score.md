# BalancedAccuracy.Score

The mean recall over the classes that have at least one true sample.

<!-- docs-declaration -->

```csharp
public static double Score(ConfusionMatrix cm, bool adjusted = false)
public static double Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, bool adjusted = false, ReadOnlySpan<int> labels = default, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `cm` is a matrix already counted, or pass `yTrue` and `yPred` and let it be
counted
here. `adjusted` rescales the result so that chance scores `0` instead of `1/k`. `labels` fixes
the
label set and its order; omit it for the sorted union of both inputs. `sampleWeight` gives each
sample its own weight.

**Returns** — `double` in `[0, 1]` normally, `1` meaning every class was recalled perfectly. With
`adjusted: true` the range becomes `[-1/(k-1) … 1]`, so a below-chance model returns a negative
number.

**Exceptions** — `ArgumentException` when the label spans disagree in length or are empty;
`ArgumentNullException` when `cm` is null.

**Example** — ten samples, two of them positive, and a model that predicts the majority class
every
time. `Accuracy.Score` on this data is `0.8`.

```csharp
using Lodestar.Metrics;

int[] yTrue = [0, 0, 0, 0, 0, 0, 0, 0, 1, 1];
int[] yPred = [0, 0, 0, 0, 0, 0, 0, 0, 0, 0];

double balanced = BalancedAccuracy.Score(yTrue, yPred);   // => 0.5
```

**Remarks** — reach for this the moment the classes are unbalanced and you still want one number.
It
is the honest version of accuracy for that case: a model that ignores the minority class cannot
get
above `0.5` on two classes however large the majority is, because each class contributes its own
recall and nothing else. On more than two classes it is exactly macro-averaged recall, so
`BalancedAccuracy.Score` and `Recall.Score(…, Averaging.Macro)` are two names for one number when
no
label subset is in play.

`adjusted: true` answers a different complaint: that `0.5` on two classes and `0.333…` on three
both
mean "no better than guessing", and cannot be compared. Adjusting maps chance to `0` and perfect
to
`1` whatever `k` is, at the price of a range that now goes negative.

Two traps, and the second is subtle. The average runs over the classes that **appear in the
truth**,
not the classes you asked for — a class named in `labels` with no true sample is dropped rather
than
scored `0`, which is scikit-learn's behaviour and means the divisor is not always `labels.Length`.
And when only one class survives that filter, `adjusted: true` divides by `1 - 1/1`, so the result
is `NaN` or `-∞` rather than a number; that is left to IEEE 754 on purpose, and the reasoning is
in
[decision
0029](../../../decisions/0029-balanced-accuracy-adjusted-is-left-to-ieee-754-at-the-edge.md).

The `ConfusionMatrix` overload divides each recall by its own row sum in the `Labels`-sized view,
where `Recall.Score` divides by scikit-learn's `true_sum` over every observed label. The two agree
whenever nothing was dropped, and part company on a matrix built with an explicit label subset.

**Applies to** — net10.0, netstandard2.0.

**See also** — `Accuracy.Score`, `Recall.Score`, `CohenKappa.Score`,
[decision
0029](../../../decisions/0029-balanced-accuracy-adjusted-is-left-to-ieee-754-at-the-edge.md),
the [Python equivalence table](../../../equivalence.md).
