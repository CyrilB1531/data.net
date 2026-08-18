# LogLoss.MultiClass

The cross-entropy over a probability matrix — `sklearn.metrics.log_loss` with 2-D probabilities.

<!-- docs-declaration -->

```csharp
public static double MultiClass(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yProba, int classCount, bool normalize = true, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `yTrue` is the true class index of each sample, in `[0, classCount)`. `yProba` is
the class probabilities row-major: sample 0's classes, then sample 1's. `classCount` is how many
classes each row scores. `normalize` divides by the total weight; pass `false` for the sum.
`sampleWeight` is one weight per sample, or empty.

**Returns** — `double`, `0` or above and unbounded. Only the column of the true class contributes,
so the score depends on the other columns solely through whatever normalisation the caller applied.

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

double loss = LogLoss.MultiClass(truth, probabilities, classCount: 3);  // => 0.5017…
```

**Remarks** — a row that does not sum to `1` is neither refused nor renormalised. The reference warns
and scores the values as given; there is no warning channel here, so the number is the only signal —
measured, halving every row above takes the loss to `1.1948…`. This is the one place where
[`RocAuc.MultiClass`](rocauc-multiclass.md) is stricter than its own reference and this is not:
that one refuses a row that does not sum to `1`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`LogLoss.Score`](logloss-score.md), [`BrierScore.MultiClass`](brierscore-multiclass.md),
the [Python equivalence table](../../../equivalence.md).
