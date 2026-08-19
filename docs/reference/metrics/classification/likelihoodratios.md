# LikelihoodRatios

How much a prediction should move a belief — and, unlike every other classification metric here,
**independently of how common the class is**.

A positive prediction multiplies the prior odds by [`Positive`](likelihoodratios-compute.md) and a
negative one by [`Negative`](likelihoodratios-compute.md). That is what makes the pair worth
reporting on a rare class: [`Precision`](precision.md) falls as the class gets rarer even though the
classifier has not changed, and these do not. A test asserts exactly that — holding sensitivity and
specificity fixed while adding negatives leaves both ratios where they were and moves precision.

## Two numbers, so a small type of its own

`class_likelihood_ratios` returns a pair, and the two are not interchangeable: `LR+` above `1` says a
positive prediction is evidence *for* the class, `LR-` below `1` says a negative prediction is
evidence *against* it. A tuple would have carried no names and no documentation, so this is a sealed
class with two named properties — the shape
[decision 0040](../../../decisions/0040-a-curve-is-a-sealed-class-per-curve.md) settled for the
curves, applied to two scalars instead of three arrays.

## Four ways a ratio has no value, and they do not answer alike

| what is missing | `Positive` | `Negative` |
| --- | --- | --- |
| nothing false-positive — specificity is `1` | undefined | a value |
| nothing true-negative — specificity is `0` | a value | undefined |
| no negative sample in the truth | undefined | undefined |
| **no positive sample in the truth** | undefined, and **not replaceable** | the same |

The last row is the one worth knowing. `undefinedPositive` and `undefinedNegative` substitute for the
first three; on the fourth the reference returns `nan` **regardless** of what was asked for, and this
reproduces that. Measured: with the replacement set to `1`, a truth of all negatives gives `(nan, nan)`
and a truth of all positives gives `(1, 1)`. Nothing in the reference's signature says so.

## Members

| Member | What it does |
| --- | --- |
| [`LikelihoodRatios.Compute`](likelihoodratios-compute.md) | Both ratios, from labels. |
