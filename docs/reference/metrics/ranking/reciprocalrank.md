# ReciprocalRank

**The one member of this package not verified against a reference.** Every other number here replays
a corpus frozen from Python; measured on scikit-learn 1.9.0, `dir(sklearn.metrics)` carries nothing
matching `reciprocal`, so there is nothing to freeze. It ships under
[decision 0036](../../../decisions/0036-a-member-may-ship-without-an-oracle-if-it-says-so.md), which
also says what would retire that exception — a reference implementation worth capturing — and its
definition is pinned by tests instead of by a corpus.

Mean reciprocal rank asks a narrower question than [`Ndcg`](ndcg.md): not how well the whole
ordering was arranged, but how far a user had to read before finding *anything* useful. That makes
it the right number when one good answer ends the search — a lookup, a question with one answer, a
navigational query — and the wrong one when the reader will consume the whole list.

Because it counts only the first relevant document, it is blind to everything after it: a ranking
that puts one relevant document first and buries nine others scores `1`, the same as a ranking that
found only that one. Report it beside an NDCG, not instead of one.

## Members

| Member | What it does |
| --- | --- |
| [`ReciprocalRank.Score`](reciprocalrank-score.md) | The mean of `1 / rank` over the queries, where `rank` is the position of the first relevant document. |
