# FisherExact

Fisher's exact test on a 2x2 contingency table.

Exact rather than asymptotic: the p-value is a sum of hypergeometric probabilities over the
tables with the same margins, so it is right at any sample size, where the chi-square
approximation needs the cells to be large.

## Members

| Member | What it does |
| --- | --- |
| [`FisherExact.Test`](fisherexact-test.md) | Tests a 2x2 table for association. |
