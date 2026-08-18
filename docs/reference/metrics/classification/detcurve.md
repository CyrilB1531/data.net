# DetCurve

The detection error tradeoff curve: the same two errors [`RocCurve`](roccurve.md) plots, but **both
as errors** — false positives against false *negatives* rather than against true positives.

That is the whole difference, and it matters for reading a plot: on a DET curve both axes are things
you want small, so a better model sits nearer the origin, where on a ROC curve a better model bows
away from the diagonal.

## It is the shortest of the three on the same input

Neither endpoint is carried. The curve starts where false positives stop being zero and stops where
false negatives reach zero, because the region outside that is where one of the two errors is
constant and the plot says nothing. Measured on the worked case: 3 points, where the ROC curve has
5 and the precision-recall curve 5.

Its points also run the other way — thresholds **ascending**, so the false-positive rate descends.

`dropIntermediate` is `false` here, as the reference has it, and shares
[`PrecisionRecallCurve`](precisionrecallcurve.md)'s rule rather than [`RocCurve`](roccurve.md)'s.

## Members

| Member | What it does |
| --- | --- |
| [`DetCurve.Compute`](detcurve-compute.md) | Draws the curve from labels and scores. |
