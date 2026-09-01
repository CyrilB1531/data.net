# SplitConformal

An interval instead of a point, a set instead of a class — and a coverage guarantee attached to
each. Static, stateless, and it never sees your model.

## Members

| Member | What it does |
| --- | --- |
| [`SplitConformal.Quantile`](splitconformal-quantile.md) | The calibrated quantile: the `k`-th smallest calibration score. |
| [`SplitConformal.AbsoluteResiduals`](splitconformal-absoluteresiduals.md) | A regressor's calibration scores, `\|y − ŷ\|`. |
| [`SplitConformal.Interval`](splitconformal-interval.md) | `[ŷ − q, ŷ + q]` around a point prediction. |
| [`SplitConformal.LeastAmbiguousScores`](splitconformal-leastambiguousscores.md) | A classifier's LAC calibration scores, `1 − p̂(true class)`. |
| [`SplitConformal.PredictionSet`](splitconformal-predictionset.md) | Every class whose probability clears `1 − q`. Possibly none. |
