# D2Tweedie

[`R2`](r2.md)'s question asked of a deviance: what fraction of it the model explains, against the
baseline of predicting one constant. One minus the model's mean
[`TweedieDeviance`](tweediedeviance.md) over the mean deviance of predicting the weighted average of
the truth.

`1` is a perfect prediction, `0` is one no better than that constant, and negative is worse than
doing nothing. Unlike the deviance itself, it is unitless and comparable across models on the same
data.

**At power 0 it is `R2` exactly**, since that regime's deviance is the squared error and its
constant baseline is the mean. Measured on the worked case, both give `0.65`.

## A truth that never varies is refused here and answered elsewhere

If every `yTrue` is the same value, the constant baseline is already perfect, the denominator is
zero, and there is no deviance to explain. This throws `UndefinedMetricException`;
[`D2AbsoluteError`](d2absoluteerror.md) and [`D2Pinball`](d2pinball.md) answer `0` on the same input.

That split is the reference's, not this library's: `d2_absolute_error_score` masks the zero
denominator and returns `0`, while `d2_tweedie_score` divides by it and raises `ZeroDivisionError`.
Reproducing the asymmetry copies a divergence rather than inventing one.

**Multioutput is not offered**, because the reference refuses it — `d2_tweedie_score` on a 2-D input
raises "Multioutput not supported in d2_tweedie_score". The two pinball scores do take it.

## Members

| Member | What it does |
| --- | --- |
| [`D2Tweedie.Score`](d2tweedie-score.md) | The fraction of Tweedie deviance the model explains. |
