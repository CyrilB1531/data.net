# Ndcg

The one to report. Each row's discounted gain is divided by the gain of its own perfect ranking, so
the result is in `[0, 1]` regardless of how relevance was scaled and rows with different judgement
scales can be averaged together.

The ideal is computed *without* tie averaging, as scikit-learn computes it: ranking a row by its own
relevance leaves ties only between equal gains, which no ordering can separate. A row where nothing
is relevant has no ideal to divide by and scores `0`.

No `logBase`, unlike [`Dcg`](dcg.md), because `ndcg_score` has none — the discount cancels in the
ratio when both halves share a base, and scikit-learn shares base 2.

## Members

| Member | What it does |
| --- | --- |
| [`Ndcg.Score`](ndcg-score.md) | The mean normalized discounted gain over the rows, in `[0, 1]`. |
