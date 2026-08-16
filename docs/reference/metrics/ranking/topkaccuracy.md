# TopKAccuracy

Classification wearing a ranking's clothes. The input is a score per class per sample — the same
shape a multiclass classifier's `predict_proba` produces — and a sample counts as correct when its
true class is anywhere among the `k` highest-scoring, rather than only when it is the single
highest. At `k = 1` it is ordinary accuracy.

It is on this page rather than with the classification metrics because what it measures is a
position in an ordering, and because it shares the tie rule the rest of the page uses: equal scores
are ranked in descending index order, which is what scikit-learn's stable sort gives. A tie
straddling the `k` boundary therefore has a determined answer, not an arbitrary one.

One divergence from `top_k_accuracy_score`, and it is a widening rather than a narrowing.
scikit-learn infers the class set from `y_true` and refuses a score row wider than what it found
unless it is given `labels`; here the class count is a parameter, so a class no sample happens to
carry raises nothing — there is no inference left to be wrong about.

## Members

| Member | What it does |
| --- | --- |
| [`TopKAccuracy.Score`](topkaccuracy-score.md) | The fraction of samples whose true class is among the `k` highest-scoring, or the count of them. |
