# Wilcoxon

The Wilcoxon signed-rank test on paired measurements.

The rank-based counterpart to [`TTest.Paired`](ttest-paired.md). What it does with a pair whose
difference is exactly zero is not a detail but part of the test's definition, which is why
[`ZeroMethod`](zeromethod.md) is a parameter and not a hidden convention.

## Members

| Member | What it does |
| --- | --- |
| [`Wilcoxon.Paired`](wilcoxon-paired.md) | Compares two paired samples by the ranks of their differences. |
| [`Wilcoxon.OneSample`](wilcoxon-onesample.md) | Compares a sample of differences against a median of zero. |
