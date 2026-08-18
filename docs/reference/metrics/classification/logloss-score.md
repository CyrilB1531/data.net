# LogLoss.Score

The binary cross-entropy — `sklearn.metrics.log_loss`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yProba, int posLabel = 1, bool normalize = true, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `yTrue` is the true labels, one per sample. `yProba` is the probability of
`posLabel` for each sample, in `[0, 1]`. `posLabel` is the label that probability is about, `1` by
default. `normalize` divides by the total weight; pass `false` for the sum, which is
`normalize=False`. `sampleWeight` is one weight per sample, or empty — the default.

**Returns** — `double`, `0` or above and unbounded. A perfect prediction scores
`2.2204460492503136e-16` rather than `0`, because the clip applies at the top end too.

**Exceptions** — `ArgumentException` when the lengths disagree, the input is empty, or a probability
falls outside `[0, 1]`. The message is the reference's — "y_prob contains values greater than 1: 1.5"
above, and "y_prob contains values lower than 0: -0.1" below.
[`BrierScore.Score`](brierscore-score.md) words that second one as *less than*, which is its own
reference's wording rather than an inconsistency here.

**Example** — four samples, well calibrated.

```csharp
using Lodestar.Metrics;

int[] truth = [0, 1, 1, 0];
double[] confidence = [0.1, 0.9, 0.8, 0.3];

double loss = LogLoss.Score(truth, confidence);  // => 0.1976…
```

One confident mistake is what the metric is built to punish:

```csharp
using Lodestar.Metrics;

int[] truth = [1, 0];
double[] certainAndWrong = [0.0, 0.5];

double punished = LogLoss.Score(truth, certainAndWrong);  // => 18.3684…
```

**Remarks** — `posLabel` is a **widening**. `log_loss` has no such parameter: a one-dimensional
probability column always describes the greater of the two labels present, and passing `labels` in
the other order does not change that — measured, it warns and returns the same number. Scoring about
the other class is the same call on the complement, which is what this parameter reaches, and the
frozen corpus pins that equivalence.

The clip is machine epsilon; [the type page](logloss.md) has what that decides.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`LogLoss.MultiClass`](logloss-multiclass.md),
[`BrierScore.Score`](brierscore-score.md), [`RocAuc.Score`](rocauc-score.md), the
[Python equivalence table](../../../equivalence.md).
