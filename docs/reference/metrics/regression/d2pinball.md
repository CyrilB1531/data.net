# D2Pinball

[`R2`](r2.md)'s question asked of a quantile prediction: what fraction of the
[`PinballLoss`](pinballloss.md) the model explains, against the baseline of predicting one constant —
the weighted quantile of the truth at the same `alpha`.

`1` is a perfect prediction, `0` is one no better than that constant, and negative is worse. It is
the score to report beside a quantile model, because the raw pinball loss is in the target's units
and says nothing about whether the model beat a flat guess.

**At `alpha = 0.5` it is [`D2AbsoluteError`](d2absoluteerror.md) exactly.** That is an invariant no
oracle states — the two reach their denominator through different code, a quantile at one half and a
median — so the test suite asserts it across every fixture of the frozen corpus rather than trusting
it.

## A column whose truth never varies scores 0

The constant baseline is already perfect and the denominator is zero. The reference masks that case
and returns `0` rather than dividing, and so does this — unlike [`D2Tweedie`](d2tweedie.md), which
raises on the same input because its own reference does.

Below two samples the answer is `nan`, which scikit-learn warns about and returns; `zeroDivision`
offers `0`, `1` or a refusal instead, as [`R2.Score`](r2-score.md) does for the identical case.

## Members

| Member | What it does |
| --- | --- |
| [`D2Pinball.Score`](d2pinball-score.md) | The explained fraction, one number for the whole prediction. |
| [`D2Pinball.PerOutput`](d2pinball-peroutput.md) | The same, one number per output column. |
