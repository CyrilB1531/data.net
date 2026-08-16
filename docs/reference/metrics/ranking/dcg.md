# Dcg

The unnormalized half of the pair. It answers "how much relevance is near the top?" in the units the
relevance was given in, so it grows with those values and has no upper bound — two rows are
comparable only when their relevance judgements are on the same scale.

That is the reason it is rarely the number reported, and the reason it is here anyway:
[`Ndcg`](ndcg.md) is this divided by its own ideal, and a surprising NDCG is usually explained by
looking at the two halves separately.

Alone among the four types on this page it takes a `logBase`, because `dcg_score` does — the
discount does not cancel here, so changing its base changes the answer.

## Members

| Member | What it does |
| --- | --- |
| [`Dcg.Score`](dcg-score.md) | The mean discounted gain over the rows. |
