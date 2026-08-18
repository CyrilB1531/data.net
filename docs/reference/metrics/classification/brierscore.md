# BrierScore

The mean squared error of a probabilistic prediction — the gentler half of the calibration question
[`LogLoss`](logloss.md) asks. `0` is a perfect, perfectly confident prediction, and a confident
mistake costs at most `1` where the logarithm makes it unbounded.

Both are **proper scoring rules**: both are minimised by predicting the truth, so neither can be
gamed by shading a probability toward the safer answer. What they disagree about is how much a single
overconfident sample should matter. Two samples predicted `0.0` for a class that occurred score
exactly `1` here and above `36` there.

## `scaleByHalf` reads the shape, and the default follows it

The reference's `scale_by_half='auto'` resolves differently by input shape, and the two entry points
here take that default rather than a string:

| input | reference | here |
| --- | --- | --- |
| one probability per sample | halves the two-class sum | [`Score`](brierscore-score.md), `scaleByHalf: true` |
| a probability matrix | does not halve | [`MultiClass`](brierscore-multiclass.md), `scaleByHalf: false` |

Measured on the same four-sample matrix: `0.245` unhalved and `0.1225` halved. On a binary vector:
`0.0375` halved — the familiar Brier score — and `0.075` unhalved.

## Members

| Member | What it does |
| --- | --- |
| [`BrierScore.Score`](brierscore-score.md) | The binary case, from one probability per sample. |
| [`BrierScore.MultiClass`](brierscore-multiclass.md) | The same over a probability matrix. |
