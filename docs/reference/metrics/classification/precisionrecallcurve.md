# PrecisionRecallCurve

The precision-recall curve as plot data. Where [`RocCurve`](roccurve.md) barely moves when a few
thousand negatives are ranked above a handful of positives, this collapses — which is why it is the
curve to plot when positives are rare.

## Its thresholds array is one shorter, deliberately

`Precision` and `Recall` have one more entry than `Thresholds`. The extra point is the endpoint at
recall `0` and precision `1`, which **no threshold produces** — it is where the model predicts
nothing positive at all.

Padding the array to match would invent a threshold for that point, and a caller plotting thresholds
against precision would silently plot one pair too many. The asymmetry is the reference's, and
[decision 0040](../../../decisions/0040-a-curve-is-a-sealed-class-per-curve.md) keeps it.

## The area under it is not the average precision

[`Auc.Trapezoid`](auc-trapezoid.md) over these points interpolates between two thresholds as though
the curve were straight there, and reads optimistic:
[`AveragePrecision.Score`](../ranking/averageprecision-score.md) sums the steps instead. Measured on
the worked case, `0.7916…` against `0.8333…`. A test holds the two apart rather than a reader having
to.

`dropIntermediate` is `false` here, as the reference has it, and drops by a different rule from
[`RocCurve`](roccurve.md)'s — see that page's table.

## Members

| Member | What it does |
| --- | --- |
| [`PrecisionRecallCurve.Compute`](precisionrecallcurve-compute.md) | Draws the curve from labels and scores. |
