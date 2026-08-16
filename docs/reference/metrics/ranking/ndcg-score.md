# Ndcg.Score

The mean normalized discounted gain over the rows, in `[0, 1]` — `sklearn.metrics.ndcg_score`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yScore, int labelCount, int? k = null, bool ignoreTies = false, ReadOnlySpan<double> sampleWeight = default)
```

**Parameters** — `yTrue` is the relevance of each document and `yScore` the scores the ranking was
made from, both row-major: one row per query, `labelCount` values each, and the same length.
`k` scores only the first `k` positions, or `null` for all of them; a `k` past `labelCount` scores
the whole row rather than raising. `ignoreTies` ranks equal scores in descending index order instead
of averaging over their permutations. `sampleWeight` carries one weight per query, or is empty for
an unweighted mean; over a single query it cancels, since it multiplies both halves of the mean.

There is no `logBase`, because `ndcg_score` has none: the discount cancels in the ratio only when
both halves share a base, and scikit-learn shares base 2. Pass one to
[`Dcg.Score`](dcg-score.md) instead, where it changes the answer.

**Returns** — `double` in `[0, 1]`. `1` when the ranking is as good as its relevance allows, and `0`
when no document in the row is relevant — there is no ideal to divide by, and the answer is `0`
rather than a division by zero.

The `[0, 1]` holds for every weight vector but one: a **negative** `sampleWeight` takes the mean
outside it, which the reference does too rather than refusing — frozen in `ranking_weighted.json`,
`-0.7039180890341348` at `k = 2` on weights `[-1, 2]`. [`Dcg.Score`](dcg-score.md) is unbounded
above and so has nothing to lose here.

**Exceptions** — `ArgumentException` when `labelCount` is below `2` (scikit-learn's own sentence,
"Computing NDCG is only meaningful when there is more than 1 document."), when `sampleWeight` is
neither empty nor one value per query, when it sums to zero — `numpy.average`'s own refusal — when
`yTrue` and `yScore`
disagree in length, when the length is not a whole number of rows of `labelCount`, or when any
relevance is **negative** — "ndcg_score should not be used on negative y_true values.", which is
scikit-learn's refusal and the reason the `[0, 1]` above holds. `ArgumentOutOfRangeException` when
`k` is below `1`; scikit-learn refuses the same value.

**Example** — the same relevance ranked perfectly, then backwards.

```csharp
using Lodestar.Metrics;

double[] relevance = [3, 2, 1, 0];
double[] best = [0.9, 0.5, 0.4, 0.1];
double[] worst = [0.1, 0.4, 0.5, 0.9];

double perfect = Ndcg.Score(relevance, best, labelCount: 4);  // => 1
double backwards = Ndcg.Score(relevance, worst, labelCount: 4);  // => 0.6138…
```

**Remarks** — the worst possible ordering scores `0.6138…`, not `0`. That is not a bug: the
logarithmic discount is shallow, so even a reversed list collects most of the ideal gain, and the
floor of this metric on a row with several relevant documents is well above zero. Read NDCG as a
comparison between rankings of the same rows, never as a fraction of "how much better than random".

The ideal is computed without tie averaging, as scikit-learn does — ranking a row by its own
relevance leaves ties only between equal gains, which no ordering can separate.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Dcg.Score`](dcg-score.md), [`ReciprocalRank.Score`](reciprocalrank-score.md), the
[Python equivalence table](../../../equivalence.md).
