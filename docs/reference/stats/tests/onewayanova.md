# OneWayAnova

One-way analysis of variance: do several groups share one mean?

The k-sample generalisation of [`TTest.Independent`](ttest-independent.md) with
[`Variance.Equal`](variance.md): on two groups the F statistic is the square of Student's t, and
the two p-values agree.

A degenerate input where every group is internally constant **and** the groups share that same
constant computes `0.0 / 0.0` and returns a `NaN` statistic and p-value, propagated rather than
guarded against — this matches scipy's own `f_oneway` on the same input, and a `NaN` here is an
honest answer: an F statistic genuinely has no value when both its numerator and its denominator
vanish. This is deliberately unlike [`KruskalWallis`](kruskalwallis.md), whose analogous
degenerate input (every pooled value tied) throws instead, because there the ranks the statistic
is built from are provably meaningless rather than merely indeterminate.

## Members

| Member | What it does |
| --- | --- |
| [`OneWayAnova.Test`](onewayanova-test.md) | Compares the means of two or more groups. |
