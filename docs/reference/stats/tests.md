# `Lodestar.Stats`

Ten families of classical hypothesis test, at `scipy.stats` 1.18.0 parity.
Arrays in, a statistic and a p-value out; nothing is fitted, so every entry
point is static.

| test | what it asks | entry point |
| --- | --- | --- |
| Student / Welch *t* | do two samples have the same mean? | [`TTest`](tests/ttest.md) |
| Mann-Whitney *U* | the same question, assuming no shape | [`MannWhitney`](tests/mannwhitney.md) |
| Wilcoxon signed-rank | the same, on paired measurements | [`Wilcoxon`](tests/wilcoxon.md) |
| χ² | do counts match an expected distribution, or are two factors independent? | [`ChiSquare`](tests/chisquare.md) |
| Fisher exact | the same for a 2×2 table, at any sample size | [`FisherExact`](tests/fisherexact.md) |
| Kolmogorov-Smirnov | do two samples share a distribution? | [`KolmogorovSmirnov`](tests/kolmogorovsmirnov.md) |
| one-way ANOVA | do several groups share one mean? | [`OneWayAnova`](tests/onewayanova.md) |
| Kruskal-Wallis | the same, assuming no shape | [`KruskalWallis`](tests/kruskalwallis.md) |
| Shapiro-Wilk | could this sample be normal? | [`ShapiroWilk`](tests/shapirowilk.md) |
| Bonferroni / BH / BY | how many of these results are chance? | [`MultipleComparisons`](tests/multiplecomparisons.md) |

Every family but three returns the same two numbers, [`TestResult`](tests/testresult.md); a
t-test also carries its degrees of freedom ([`TTestResult`](tests/ttestresult.md)), a
contingency table also carries the table independence would have produced
([`Chi2ContingencyResult`](tests/chi2contingencyresult.md)), and Kolmogorov-Smirnov also carries
where and in which direction the two samples parted furthest ([`KsResult`](tests/ksresult.md)).

| result | carries | returned by |
| --- | --- | --- |
| [`TestResult`](tests/testresult.md) | a statistic, a p-value | eight of the ten families |
| [`TTestResult`](tests/ttestresult.md) | + degrees of freedom, a confidence interval | [`TTest`](tests/ttest.md) |
| [`Chi2ContingencyResult`](tests/chi2contingencyresult.md) | + degrees of freedom, the expected table | [`ChiSquare.Contingency`](tests/chisquare-contingency.md) |
| [`KsResult`](tests/ksresult.md) | + where the gap was reached, and its sign | [`KolmogorovSmirnov`](tests/kolmogorovsmirnov.md) |

Five small enums choose what a test asks, and how it answers when the exact and the approximate
route disagree.

| option | chooses |
| --- | --- |
| [`Alternative`](tests/alternative.md) | which tail the p-value covers |
| [`Variance`](tests/variance.md) | whether an independent-samples *t*-test pools the two variances |
| [`Continuity`](tests/continuity.md) | whether a discrete statistic's normal approximation gets the half-unit correction |
| [`ExactMethod`](tests/exactmethod.md) | the exact null distribution, its normal approximation, or a choice between them |
| [`ZeroMethod`](tests/zeromethod.md) | what Wilcoxon does with a pair whose difference is exactly zero |

The [hypothesis-testing guide](../../guides/hypothesis-testing.md) says which
test answers which question, and the
[Python equivalence table](../../equivalence.md) maps each `scipy` call to its
counterpart here.
