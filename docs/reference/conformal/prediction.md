# Split conformal prediction — `Lodestar.Conformal`

A model gives you one number, or one class. This page turns it into an **interval**, or a **set**,
that contains the truth a stated fraction of the time — 90 % of the time, say — and the fraction is
a finite-sample guarantee rather than an asymptotic hope. It costs a held-out calibration set and
nothing else: no retraining, no distributional assumption, and no assumption that the model is any
good. A bad model gets wide intervals, which is the correct answer.

There is one type, [`SplitConformal`](prediction/splitconformal.md), and it is static. The
calibrated quantile is handed back to you rather than kept inside an object, because it is the
number that carries the guarantee and you should be able to look at it.

The whole procedure is three calls:

1. Score the calibration set —
   [`AbsoluteResiduals`](prediction/splitconformal-absoluteresiduals.md) for a regressor,
   [`LeastAmbiguousScores`](prediction/splitconformal-leastambiguousscores.md) for a classifier.
2. Turn the scores into one number — [`Quantile`](prediction/splitconformal-quantile.md).
3. Apply it to a new prediction — [`Interval`](prediction/splitconformal-interval.md) or
   [`PredictionSet`](prediction/splitconformal-predictionset.md).

> **The guarantee assumes exchangeability.** It does not hold for time series, for data with drift,
> or for any split that leaks. The intervals still come out; they simply do not cover, and nothing
> in the output says so. The guide's
> [*Exchangeability*](../../guides/conformal.md#exchangeability) section is the part of this
> documentation worth reading before the API.

| Member | What it does |
| --- | --- |
| [`SplitConformal.Quantile`](prediction/splitconformal-quantile.md) | The calibrated quantile: the `k`-th smallest score, with `k = ceil((n + 1)(1 − α))`. |
| [`SplitConformal.AbsoluteResiduals`](prediction/splitconformal-absoluteresiduals.md) | A regressor's calibration scores, `\|y − ŷ\|`. |
| [`SplitConformal.Interval`](prediction/splitconformal-interval.md) | `[ŷ − q, ŷ + q]` around a point prediction. |
| [`SplitConformal.LeastAmbiguousScores`](prediction/splitconformal-leastambiguousscores.md) | A classifier's LAC calibration scores, `1 − p̂(true class)`. |
| [`SplitConformal.PredictionSet`](prediction/splitconformal-predictionset.md) | Every class whose probability clears `1 − q`. Possibly none. |
