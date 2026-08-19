# CalibrationCurve

The reliability curve as plot data: for each bin of predicted probability, what the model said and
what actually happened. A well-calibrated model puts the two on the diagonal — of the samples it
called 70% likely, about 70% were positive.

Where [`BrierScore`](brierscore.md) and [`LogLoss`](logloss.md) answer *how badly calibrated* with
one number, this shows *where*: a model can score well overall and still be systematically
over-confident at the top of its range.

## Its arrays are as long as the bins that held something

`ProbTrue` and `ProbPred` always share a length, and that length is **not** `nBins`. A bin no sample
fell into is dropped rather than reported as empty, so the length depends on the data.

Measured on the worked case: four probabilities over five uniform bins return **four** points, and
four probabilities that all land inside one bin return **one**. A caller sizing an array from
`nBins` would be wrong on both.

## It lives in `sklearn.calibration`, not `sklearn.metrics`

The reference is `sklearn.calibration.calibration_curve` — the one member of the calibration family
outside `sklearn.metrics`. [`docs/equivalence.md`](../../../equivalence.md) names the real module
rather than filing it beside its siblings.

## The two strategies do not divide the same thing

`BinStrategy.Uniform` cuts `[0, 1]` into equal widths, whatever the data does. `BinStrategy.Quantile`
reads the edges off the probabilities themselves, so each bin holds about the same number of
samples — **about**, because repeated probabilities collapse edges onto each other and empty bins
rather than balancing them. The strategy equalises rank, not count.

Its edges come from the linear interpolation `np.percentile` computes, which is **not** the weighted
percentile [decision 0024](../../../decisions/0024-weighted-median-averages-within-scikit-learns-epsilon.md)
pinned for the medians: the two disagree, and reusing the weighted one would move the third decimal.

## Members

| Member | What it does |
| --- | --- |
| [`CalibrationCurve.Compute`](calibrationcurve-compute.md) | Draws the curve from labels and predicted probabilities. |
