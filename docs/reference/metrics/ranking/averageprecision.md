# AveragePrecision

The precision-recall curve summarised as one number — but as a **sum over its steps**, not as the
area under it. Walk the samples from the highest score down; every time the recall moves, add how
much it moved times the precision at that point. `1` means every positive sample outranks every
negative one.

**This is deliberately not a trapezoid.** scikit-learn's `auc(recall, precision)` over the same
curve interpolates between two neighbouring thresholds as though the curve were a straight line
there, which it is not, and the result comes out optimistic. Measured on `y_true = [0, 0, 1, 1]`
and `y_score = [0.1, 0.4, 0.35, 0.8]`: the sum is `0.8333…` and the trapezoid `0.7916…`. On a row
whose scores are all tied the gap is wider still — `0.5` against `0.75`. Reproducing the wrong one
of the two is the mistake this type exists to avoid, and the frozen corpus carries the trapezoid
beside every binary case so a test can assert the two never converge by accident.

Where [`RocAuc`](../classification/rocauc.md) asks how well the ranking separates the two classes
over the whole range of thresholds, this asks how much of the top of the ranking is positive — which
is the question worth asking when positives are rare, because a ROC curve barely moves when a few
thousand negatives are ranked above a handful of positives and a precision-recall curve collapses.

## The label matrix, and the three averagings

Over a boolean matrix — one label per column, the shape
[`LabelRankingAveragePrecision`](labelrankingaverageprecision.md) and its two siblings take — each
column is scored on its own and the columns are then combined.
[`AveragePrecision.PerLabel`](averageprecision-perlabel.md) returns them uncombined.

| `Averaging` | What it does |
| --- | --- |
| `Macro` | The plain mean of the per-label scores, a column no sample carries included at its `0`. |
| `Micro` | The whole matrix read as one binary problem, row by row. A sample's weight repeats across its labels. |
| `Weighted` | The per-label scores averaged by how much positive weight each label carries. |

`Averaging.Binary` scores one positive label of two and means nothing over a matrix, so it is
refused rather than silently treated as `Macro`.

**`average='samples'` is not offered.** scikit-learn has a fourth mode that averages over rows
rather than columns, and it has no member in this package's `Averaging` — which
[`Precision`](../classification/precision.md), [`Recall`](../classification/recall.md),
[`F1`](../classification/f1.md) and [`FBeta`](../classification/fbeta.md) share, and none of them
implements it either. Adding a member here would promise it on four types that do not have it.

## Three inputs answer outside the reference's range

Each is a weight vector the reference itself struggles with, and each is measured on both sides
rather than reasoned about. They are listed on
[`AveragePrecision.Score`](averageprecision-score.md), beside the numbers.

## Members

| Member | What it does |
| --- | --- |
| [`AveragePrecision.Score`](averageprecision-score.md) | The step sum over the precision-recall curve, binary or over a label matrix. |
| [`AveragePrecision.PerLabel`](averageprecision-perlabel.md) | One score per label of a matrix, uncombined. |
