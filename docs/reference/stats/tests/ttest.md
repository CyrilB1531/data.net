# TTest

Student's and Welch's *t*-tests: independent, paired and one-sample.

Arrays in, a statistic and a p-value out; every entry point is static.
[`Independent`](ttest-independent.md) defaults to [`Variance.Welch`](variance.md), where
`scipy.stats.ttest_ind` defaults to Student's `equal_var=True` — pooling is only correct when
the two populations really share a variance, so the safer default costs a word at the call site
rather than a wrong answer.

## Members

| Member | What it does |
| --- | --- |
| [`TTest.Independent`](ttest-independent.md) | Compares the means of two independent samples. |
| [`TTest.Paired`](ttest-paired.md) | The paired *t*-test: a one-sample test on the differences. |
| [`TTest.OneSample`](ttest-onesample.md) | Compares a sample's mean against a stated population mean. |
