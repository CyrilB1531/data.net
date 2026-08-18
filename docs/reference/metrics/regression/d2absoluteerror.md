# D2AbsoluteError

[`R2`](r2.md)'s question asked of the absolute error: what fraction of it the model explains against
the baseline of always predicting the weighted **median** of the truth.
[`D2Pinball`](d2pinball.md) at `alpha = 0.5`.

**Where `R2` compares against the mean and is pulled by an outlier, this compares against the
median and is not.** That is the whole reason to prefer it: one wild value in the truth inflates
`R2`'s baseline and flatters every model measured against it, while the median baseline barely
moves. On a truth of `[5, 5, 5, 1]` against a prediction of `[1, 2, 3, 4]` this scores `-2` — the
model is worse than the flat guess, and says so.

A truth that never varies scores `0` rather than raising, unlike [`D2Tweedie`](d2tweedie.md); below
two samples the answer is `nan`, which `zeroDivision` can change.

## Members

| Member | What it does |
| --- | --- |
| [`D2AbsoluteError.Score`](d2absoluteerror-score.md) | The explained fraction, one number for the whole prediction. |
| [`D2AbsoluteError.PerOutput`](d2absoluteerror-peroutput.md) | The same, one number per output column. |
