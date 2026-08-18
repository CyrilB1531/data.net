# GammaDeviance

The deviance for a strictly positive quantity with no natural unit — a duration, a cost, a claim
size. [`TweedieDeviance`](tweediedeviance.md) at `power = 2`.

**It is scale-invariant, and the Poisson is not.** Multiplying both the truth and the prediction by
the same factor leaves this number exactly where it was, because the deviance depends on the two only
through their ratio. That is what makes it the deviance to reach for when "out by 10%" means the same
thing at every magnitude, and [`PoissonDeviance`](poissondeviance.md) the one to reach for when a
count is a count.

**Both operands must be strictly positive.** Unlike the Poisson, a zero truth is refused here too:
the term carries `log(ŷ / y)`, which has no value at `y = 0`, and the reference refuses rather than
returning an infinity.

## Members

| Member | What it does |
| --- | --- |
| [`GammaDeviance.Score`](gammadeviance-score.md) | The mean deviance for a positive, scale-free target. |
