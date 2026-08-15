# CohenKappa.Score

Observed agreement minus expected agreement, scaled so that perfect agreement is `1`.

<!-- docs-declaration -->

```csharp
public static double Score(ConfusionMatrix cm, KappaWeighting weighting = KappaWeighting.None, ZeroDivision zeroDivision = ZeroDivision.NaN)
public static double Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<int> yPred, KappaWeighting weighting = KappaWeighting.None, ZeroDivision zeroDivision = ZeroDivision.NaN, ReadOnlySpan<int> labels = default, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `cm` is a matrix already counted, or pass `yTrue` and `yPred`. `weighting` says
how
far apart two different classes count as being — `KappaWeighting.None` by default, which charges
every disagreement the same. `zeroDivision` decides the answer when the expected agreement
collapses,
and defaults to `ZeroDivision.NaN`, which is scikit-learn's value here rather than the `Zero` the
precision family defaults to. `labels` fixes the label set and its order, and `sampleWeight`
weights
the samples.

**Returns** — `double` at most `1`: `1` for total agreement, `0` for agreement no better than
chance, and negative for agreement worse than chance.

**Exceptions** — `ArgumentNullException` when `cm` is null; `ArgumentOutOfRangeException` when
`weighting` is not one of the three defined values; `ArgumentException` when the label spans
disagree in length or are empty; `UndefinedMetricException` when the expected agreement collapses
and `zeroDivision` is `ZeroDivision.Throw`.

**Example** — a model scored against a human rater on a three-point scale, with the disagreements
charged by how far apart the two ratings were.

```csharp
using DataNet.Metrics;

int[] rater = [1, 1, 2, 2, 3, 3, 1, 3];
int[] model = [1, 3, 2, 1, 3, 2, 1, 3];

double flat = CohenKappa.Score(rater, model);                                // => 0.4285…
double linear = CohenKappa.Score(rater, model, KappaWeighting.Linear);       // => 0.4666…
double quadratic = CohenKappa.Score(rater, model, KappaWeighting.Quadratic); // => 0.5
```

**Remarks** — kappa is the metric for "two annotators, how much do they really agree" and, by
extension, for a model scored against a human. Its whole point is the subtraction: two raters who
both say "no" 95% of the time agree 90% of the time by accident, and accuracy will report that as
`0.9` while kappa reports something near `0`. Use it when the class distribution is skewed enough
that plain agreement flatters everyone.

`weighting` is what makes it usable on an **ordinal** scale — a five-point severity, a star rating
—
where confusing 1 with 2 is a smaller error than confusing 1 with 5. `Linear` charges the distance
in positions, `Quadratic` its square, so quadratic weighting forgives near misses much more than
it
forgives distant ones. Above, the same predictions score `0.4285…` flat and `0.5` quadratic,
because
most of the disagreements are one step wide.

The trap is that distance is measured between **positions in the label order**, not between label
values, so any weighting other than `None` depends on the order of `labels`. Reorder the same
three
labels as `[3, 1, 2]` and the quadratic score above becomes `0.3846…`; the unweighted score does
not
move at all. If your labels are ordinal, pass `labels` in the ordinal order every time, and never
let it default to the sorted union without checking that sorted *is* the ordinal order. The
reasoning, and the expected-matrix orientation this keeps from scikit-learn, are in
[decision
0030](../../../decisions/0030-cohen-kappa-keeps-scikit-learns-expected-matrix-orientation.md).

The parameter is named `weighting` and not scikit-learn's `weights` because `sampleWeight` sits in
the same signature and the two are unrelated senses of the word.

**Applies to** — net10.0, netstandard2.0.

**See also** — `KappaWeighting`, `MatthewsCorrelation.Score`, `BalancedAccuracy.Score`,
[decision
0030](../../../decisions/0030-cohen-kappa-keeps-scikit-learns-expected-matrix-orientation.md),
the [Python equivalence table](../../../equivalence.md).
