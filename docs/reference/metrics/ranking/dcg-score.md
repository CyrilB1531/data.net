# Dcg.Score

The mean discounted gain over the rows — `sklearn.metrics.dcg_score`.

<!-- docs-declaration -->

```csharp
public static double Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yScore, int labelCount, int? k = null, double logBase = 2, bool ignoreTies = false)
```

**Parameters** — `yTrue` is the relevance of each document and `yScore` the scores the ranking was
made from, both row-major: one row per query, `labelCount` values each, and the same length.
`k` scores only the first `k` positions, or `null` for all of them; a `k` past `labelCount` scores
the whole row rather than raising. `logBase` is the base of the positional discount, `2` as in
scikit-learn. `ignoreTies` ranks equal scores in descending index order instead of averaging over
their permutations — faster, and correct only when genuine ties cannot occur.

**Returns** — `double`, the mean over the rows of `Σ relevance / log(rank + 1)`. **Unbounded
above**: it grows with the relevance values, so two rows are comparable only on the same judgement
scale. Use [`Ndcg.Score`](ndcg-score.md) for a number in `[0, 1]`.

**Exceptions** — `ArgumentException` when `labelCount` is below `2` (scikit-learn's own sentence,
"Computing NDCG is only meaningful when there is more than 1 document."), when `yTrue` and `yScore`
disagree in length, or when the length is not a whole number of rows of `labelCount`.
`ArgumentOutOfRangeException` when `k` is below `1`; scikit-learn refuses the same value.

A negative relevance is **not** refused here, and the result can be negative — `dcg_score` accepts
it too. [`Ndcg.Score`](ndcg-score.md) does refuse it, because there the ratio would leave `[0, 1]`.

**Example** — four documents whose scores are all equal, scored both ways.

```csharp
using Lodestar.Metrics;

double[] relevance = [3, 2, 1, 0];
double[] tied = [0.5, 0.5, 0.5, 0.5];

double averaged = Dcg.Score(relevance, tied, labelCount: 4);  // => 3.8424…
double arbitrary = Dcg.Score(relevance, tied, labelCount: 4, ignoreTies: true);  // => 2.9229…
```

**Remarks** — the gains are **linear**. Much of the literature uses `2^relevance − 1` instead, which
on the row above ranked perfectly gives `9.3927…` where this gives `4.7618…`; the difference is the
definition, not an error on either side. The averaged and arbitrary values in the example differ by
almost a third, which is the whole reason `ignoreTies` defaults to `false`.

**Applies to** — net10.0, netstandard2.0.

**See also** — [`Ndcg.Score`](ndcg-score.md), [`ReciprocalRank.Score`](reciprocalrank-score.md), the
[Python equivalence table](../../../equivalence.md).
