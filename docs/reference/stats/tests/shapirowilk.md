# ShapiroWilk

The Shapiro-Wilk test for normality, by Royston's AS R94.

Written from Royston's 1995 published description and its published polynomial constants
(Applied Statistics 44:547-551), not from any implementation of it.

The transform that turns the statistic into a p-value is fitted for `3 <= n <= 5000`. Outside
that range there is no p-value to give, so this refuses rather than extrapolating a number a
reader would take at face value; scipy warns and answers anyway.

## Members

| Member | What it does |
| --- | --- |
| [`ShapiroWilk.Test`](shapirowilk-test.md) | Tests whether a sample could have come from a normal distribution. |
