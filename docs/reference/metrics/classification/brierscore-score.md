# BrierScore.Score

The binary Brier score — `sklearn.metrics.brier_score_loss`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yProba, int posLabel = 1, bool scaleByHalf = true, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `yTrue` is the true labels, one per sample. `yProba` is the probability of
`posLabel` for each sample, in `[0, 1]`. `posLabel` is the label that probability is about, `1` by
default. `scaleByHalf` halves the two-class sum, which is what `scale_by_half='auto'` resolves to for
a one-dimensional probability; `false` doubles the number. `sampleWeight` is one weight per sample,
or empty.

**Returns** — `double` in `[0, 1]` when `scaleByHalf` holds, `[0, 2]` when it does not. `0` is a
perfect, perfectly confident prediction.

**Exceptions** — `ArgumentException` when the lengths disagree, the input is empty, or a probability
falls outside `[0, 1]` — "y_prob contains values greater than 1: 1.5" above, and "y_prob contains
values **less** than 0: -0.1" below, which is this reference's wording where
[`LogLoss.Score`](logloss-score.md)'s says *lower*.

**Example** — the four samples the log-loss page scores, read the other way.

```csharp
using Lodestar.Metrics;

int[] truth = [0, 1, 1, 0];
double[] confidence = [0.1, 0.9, 0.8, 0.3];

double brier = BrierScore.Score(truth, confidence);  // => 0.0374…
```

The same input scored about the other class is a different question and a different number:

```csharp
using Lodestar.Metrics;

int[] truth = [0, 1, 1, 0];
double[] confidence = [0.1, 0.9, 0.8, 0.3];

double aboutZero = BrierScore.Score(truth, confidence, posLabel: 0);  // => 0.6875
```

**Remarks** — scikit-learn infers `pos_label` as the greater of the two labels present, and refuses
to guess at all for non-numeric labels; here it is a parameter with a default, as
[`RocAuc.Score`](rocauc-score.md)'s already is. `-1`/`1` and `1`/`2` labels therefore need no
special handling on either side, and both score `0.0375` on the example above.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BrierScore.MultiClass`](brierscore-multiclass.md),
[`LogLoss.Score`](logloss-score.md), the [Python equivalence table](../../../equivalence.md).
