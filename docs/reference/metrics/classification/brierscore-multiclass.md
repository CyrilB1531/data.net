# BrierScore.MultiClass

The Brier score over a probability matrix — `sklearn.metrics.brier_score_loss` with 2-D probabilities.

<!-- docs-declaration -->

```csharp
public static double MultiClass(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yProba, int classCount, bool scaleByHalf = false, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `yTrue` is the true class index of each sample, in `[0, classCount)`. `yProba` is
the class probabilities row-major. `classCount` is how many classes each row scores. `scaleByHalf`
halves the sum over classes; `false`, the default, is what `scale_by_half='auto'` resolves to for a
matrix. `sampleWeight` is one weight per sample, or empty.

**Returns** — `double`, `0` or above. Unlike [`LogLoss.MultiClass`](logloss-multiclass.md), **every**
column contributes: the score is the squared distance from the one-hot truth across the whole row,
so a probability moved between two wrong classes changes it.

**Exceptions** — `ArgumentException` when `yProba` is not `yTrue.Length × classCount`, when a label
is not a class index below `classCount`, or when a probability falls outside `[0, 1]`.
`ArgumentOutOfRangeException` when `classCount` is below two.

**Example** — four samples over three classes.

```csharp
using Lodestar.Metrics;

int[] truth = [0, 1, 2, 1];
double[] probabilities =
[
    0.7, 0.2, 0.1,
    0.1, 0.8, 0.1,
    0.2, 0.2, 0.6,
    0.3, 0.4, 0.3,
];

double brier = BrierScore.MultiClass(truth, probabilities, classCount: 3);  // => 0.245
```

**Remarks** — the default of `scaleByHalf` is `false` here and `true` on
[`BrierScore.Score`](brierscore-score.md), deliberately: the reference's `'auto'` reads the input's
shape rather than the caller's intent, and reproducing it as a default per entry point is what keeps
both numbers the reference's. Halving the example above gives `0.1225`.

Rows that do not sum to `1` are scored as given, for the reason
[`LogLoss.MultiClass`](logloss-multiclass.md) states.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`BrierScore.Score`](brierscore-score.md),
[`LogLoss.MultiClass`](logloss-multiclass.md), the [Python equivalence table](../../../equivalence.md).
