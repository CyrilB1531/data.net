# TweedieDeviance

The deviance of a generalised linear model, which is what a squared error becomes when the target is
not normally distributed. One type with a `power`, because the reference is one function with a
`power`: the Poisson and the gamma deviances are this at `1` and `2`, and
[`PoissonDeviance`](poissondeviance.md) and [`GammaDeviance`](gammadeviance.md) exist only so a
caller need not know that.

## The power picks a distribution, and each has its own domain

The deviance's formula and the inputs it will accept both change with the power. This is the whole
content of the family, and every boundary below is measured against scikit-learn 1.9.0 rather than
inferred:

| `power` | distribution | `yTrue` | `yPred` |
| --- | --- | --- | --- |
| below `0` | stable, positive support | any real | strictly positive |
| `0` | normal — the squared error | any real | any real |
| `(0, 1)` | **none** — refused | — | — |
| `1` | Poisson | non-negative | strictly positive |
| `(1, 2)` | compound Poisson-gamma | non-negative | strictly positive |
| `2` | gamma | strictly positive | strictly positive |
| above `2` | inverse gaussian and beyond | strictly positive | strictly positive |

**There is no distribution between the normal and the Poisson.** A `power` in the open interval
`(0, 1)` is refused with `ArgumentOutOfRangeException`, where scikit-learn raises
`InvalidParameterError` saying the parameter "must be a float in the range (-inf, 0.0] or a float in
the range [1.0, inf)". Everything else in the table is an `ArgumentException` carrying scikit-learn's
own sentence.

The one boundary worth remembering: **a zero truth is legal from `1` up to but not including `2`**,
and illegal from `2` on. Measured, `y_true = [0, 2, 3]` against `y_pred = [1, 2, 3]` scores
`0.6666…` at power `1` and `1.3333…` at power `1.5`, and is refused at power `2`.

## At power 0 it is the mean squared error

Not approximately — the same number. That regime's deviance is `(y − ŷ)²`, so
[`TweedieDeviance.Score`](tweediedeviance-score.md) at the default power and
[`MeanSquaredError.Score`](meansquarederror-score.md) agree, and so do
[`D2Tweedie`](d2tweedie.md) and [`R2`](r2.md). It is worth knowing which of the two you are reading
in a table.

## Members

| Member | What it does |
| --- | --- |
| [`TweedieDeviance.Score`](tweediedeviance-score.md) | The mean deviance at the given power. |
