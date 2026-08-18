# PoissonDeviance

The deviance for a count: how far a prediction is from the truth when the target is a number of
events and its variance grows with its mean. [`TweedieDeviance`](tweediedeviance.md) at `power = 1`,
which is how scikit-learn defines it too.

A type of its own because the reference exposes one, and because a caller counting claims, visits or
failures should not have to know that `1` is the Poisson. The refusal sentence still names the
power, as scikit-learn's does.

**A zero truth is legal; a zero prediction is not.** Counting zero events is ordinary data, and the
deviance has a finite limit there — `y × log(y / ŷ)` is taken as `0` at `y = 0`. Predicting zero
events is not: the logarithm has no value, and the reference refuses it rather than returning an
infinity.

## Members

| Member | What it does |
| --- | --- |
| [`PoissonDeviance.Score`](poissondeviance-score.md) | The mean deviance for a count target. |
