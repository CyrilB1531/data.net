# Ranking metrics — `Lodestar.Metrics`

You have an ordered list — search results, recommendations, retrieved passages — and a judgement of
how relevant each item actually was. Every type on this page scores that ordering, and what
separates them from the classification metrics is that *position matters*: the same set of documents
scores differently depending on where in the list the good ones landed.

Three of the four reproduce scikit-learn exactly. The fourth does not, and says so on its own page.

## The gains are linear, and much of the literature's are not

`Σ relevance / log(rank + 1)` is what [`Dcg.Score`](ranking/dcg-score.md) computes — the relevance
enters the sum as it was given. A large part of the information-retrieval literature, and several
other libraries, use `2^relevance − 1` instead, which rewards a single highly relevant document far
more steeply. Neither is wrong; they are different definitions, and a reader checking a number
against a paper will find the other one. Measured, the row `relevance = [3, 2, 1, 0]` ranked
perfectly scores `4.7618…` linearly and `9.3927…` exponentially. This page follows scikit-learn,
because everything else in this package does.

## Ties are averaged, not broken

Two documents with the same score have no order, and ranking them by the order they happened to
arrive in makes the metric depend on something the model never said. scikit-learn averages the
discounted gain over every permutation of a tied group, and that is the default here too. It has a
closed form — within a group, the mean relevance is what each position sees on average, so the group
contributes that mean times the sum of the discounts of the positions it occupies — so nothing is
enumerated and the cost is the same.

The difference is not decoration. On a row whose four scores are all equal,
[`Ndcg.Score`](ranking/ndcg-score.md) returns `0.8069…` averaged and `0.6138…` with
`ignoreTies: true`, a 30% gap on the same input. `ignoreTies` is faster and is what you want when
the scores are continuous and genuine ties cannot occur.

**On a row that does have ties, `ignoreTies` is not a parity claim on either side.** scikit-learn
reaches that path through a bare `np.argsort`, whose default is an unstable quicksort, so the order
it gives a tied group is not defined by anything. The order here *is* defined — equal scores rank by
descending index, which is what `top_k_accuracy_score`'s explicit `kind="mergesort"` gives, and what
[`TopKAccuracy.Score`](ranking/topkaccuracy-score.md) needs to agree with scikit-learn exactly. The
two coincide on every row of the frozen corpus; that they coincide on a wider one is luck, not a
guarantee, and it is the reason `ignoreTies` defaults to `false`.

## `Dcg` takes a `logBase` and `Ndcg` does not

That mirrors the reference's own surface, and the reason is arithmetic rather than taste: the
discount cancels in `Ndcg`'s ratio only when both halves share a base, so exposing it there would
offer a parameter that changes nothing. `Dcg` is unbounded above and grows with the relevance
values, which is why it is rarely reported on its own; `Ndcg` divides by the best that row could
have scored and lands in `[0, 1]`.

**Two degenerate cases answer deliberately.** A row where nothing is relevant scores `0` on
[`Ndcg.Score`](ranking/ndcg-score.md) rather than dividing by zero, and a `k` past the end of the
row is not an error — it scores the whole row, which is what `k` past the label count means. A row
of fewer than two documents *is* refused, in scikit-learn's own sentence.

| Type | What it measures |
| --- | --- |
| [`Dcg`](ranking/dcg.md) | How much relevance the ranking puts near the top, discounted by position. |
| [`Ndcg`](ranking/ndcg.md) | The same, divided by the best that row could have scored — `[0, 1]`. |
| [`ReciprocalRank`](ranking/reciprocalrank.md) | How high the first relevant document lands, averaged over queries. **No reference implementation.** |
| [`TopKAccuracy`](ranking/topkaccuracy.md) | How often the true class is among the highest-scoring few. |
