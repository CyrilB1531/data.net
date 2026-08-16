# TopKAccuracy.Score

How often the true class is among the `k` highest-scoring — `sklearn.metrics.top_k_accuracy_score`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<int> yTrue, ReadOnlySpan<double> yScore, int classCount, int k = 2, bool normalize = true)
```

**Parameters** — `yTrue` is the true class of each sample, as an index into that sample's score row.
`yScore` holds the scores row-major: one row per sample, `classCount` values each, so its length is
`yTrue.Length * classCount`. `classCount` is how many classes each row scores. `k` is how many of
the highest-scoring classes count as a hit, `2` as in scikit-learn. `normalize` returns the fraction
when true and the number of hits when false.

**Returns** — `double`: a fraction in `[0, 1]`, or a count when `normalize` is false.

**Exceptions** — `ArgumentOutOfRangeException` when `k` is below `1`. `ArgumentException` when
`classCount` is below `2`, when `yTrue` is empty, when `yScore` is not exactly `yTrue.Length` rows
of `classCount`, or when `yTrue` names a class outside `[0, classCount)` — that last one would
otherwise be counted as a miss, and read as a bad model rather than as the caller error it is.

**Example** — four samples over three classes, as a fraction and as a count.

```csharp
using Lodestar.Metrics;

int[] truth = [0, 1, 2, 2];
double[] scores =
[
    0.7, 0.2, 0.1,
    0.3, 0.5, 0.2,
    0.2, 0.3, 0.5,
    0.5, 0.3, 0.2,
];

double fraction = TopKAccuracy.Score(truth, scores, classCount: 3, k: 2);  // => 0.75
double hits = TopKAccuracy.Score(truth, scores, classCount: 3, k: 2, normalize: false);  // => 3
```

**Remarks** — `classCount` is a parameter where scikit-learn infers the class set from `y_true` and
refuses a score row wider than what it found. That is a widening, not a divergence in value: a class
no sample happens to carry raises nothing here, and on any input scikit-learn accepts the two agree.

Equal scores are ranked in descending index order, which is what scikit-learn's stable sort gives —
so a tie straddling the `k` boundary has a determined answer rather than an arbitrary one. At
`k = 1` this is ordinary accuracy, and `Accuracy.Score` on the arg-max of the same rows returns the
same number.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Ndcg.Score`](ndcg-score.md), the [Python equivalence table](../../../equivalence.md).
