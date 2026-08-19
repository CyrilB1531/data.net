# RocCurve

The receiver operating characteristic as **plot data** rather than as a number:
[`RocAuc.Score`](rocauc-score.md) tells you the area, this tells you the shape, and the shape is
what says where to put a threshold.

Three parallel arrays of the same length — the false-positive rate, the true-positive rate, and the
score at each point. A class rather than a record, for the reason
[decision 0040](../../../decisions/0040-a-curve-is-a-sealed-class-per-curve.md) gives.

## The first threshold is infinite

[`Thresholds`](roccurve-compute.md)`[0]` is `+∞`, and both rates are `0` there: no sample scores
above infinity, so nothing is predicted positive, which is the origin the curve has to start from.
The reference prepends that point rather than deriving it, and so does this — a caller iterating the
arrays in parallel must expect it.

## `dropIntermediate` defaults to `true` here and to `false` on the other two

That asymmetry is scikit-learn's, and it is reproduced rather than normalised so that a caller
porting from Python gets the same array lengths without reading a signature. The three curves also
drop by **two different rules**:

- This one drops a point the curve does not bend at — where the second difference of both counts
  vanishes, so the point is collinear with its neighbours.
- [`PrecisionRecallCurve`](precisionrecallcurve.md) and [`DetCurve`](detcurve.md) drop a point whose
  true-positive count matches both neighbours, because such points share a recall and stack on one
  vertical line.

Measured on a ten-sample fixture: this curve goes from 11 points to 5, the precision-recall curve
from 11 to 8, and the DET curve from 11 to 8.

## Members

| Member | What it does |
| --- | --- |
| [`RocCurve.Compute`](roccurve-compute.md) | Draws the curve from labels and scores. |
