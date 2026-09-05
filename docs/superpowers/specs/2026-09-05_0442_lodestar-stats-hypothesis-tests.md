# 0442 — `Lodestar.Stats`: the ten hypothesis-test families, at scipy parity

**Status:** accepted, 2026-09-05. Written before the work.

**Issue:** [#442](https://github.com/CyrilB1531/lodestar/issues/442), the `Stats` checkbox of Phase 4.

## Problem

.NET can compute a mean and a standard deviation. It cannot tell you whether a
difference is real.

The survey claim in #442 is that this is the best void-to-effort ratio on the
roadmap. #427's protocol says a gap is declared only after the incumbents are
checked, so they were, and the claim holds — but for a sharper reason than
"nobody does it":

| checked | measured | what it carries |
| --- | --- | --- |
| **`MathNet.Numerics`** 5.0.0, published 2022-04-03, 74.7M downloads | the dominant third-party numerical library | probability distributions and descriptive statistics. **No hypothesis tests.** |
| **`Accord.Statistics`** 3.8.0, published **2017-10-19**, 8.3M downloads | `accord-net/framework`, 4.5k ★, **archived by its owner on 2020-11-19, read-only** | `Accord.Statistics.Testing` — the one .NET library that had the tests, on `netstandard1.4`, nine years unpublished |
| **ML.NET** | the first-party incumbent | prediction. A t-test exists in **Azure ML Studio (classic)**, a retired hosted product, and Mann-Whitney only in **Kusto/KQL** — neither is a library a .NET process can call. |
| **this repository** | `grep -rniE 'ttest\|mannwhitney\|kruskal\|shapiro\|anova\|pvalue' src/` | nothing. |

So the shape of the gap is not that the numbers are hard. It is that the
distributions ship in one library, the tests shipped in a library that stopped,
and nothing joins them. `MathNet.Numerics` has the incomplete beta a t-test
needs and no t-test; `Accord` had the t-test and no future.

## Scope

Ten families, one lot, one pull request. Ordered as #442 lists them:

| family | type | oracle call |
| --- | --- | --- |
| Student / Welch *t* | `TTest` | `ttest_ind`, `ttest_rel`, `ttest_1samp` |
| Mann-Whitney *U* | `MannWhitney` | `mannwhitneyu` |
| Wilcoxon signed-rank | `Wilcoxon` | `wilcoxon` |
| χ² | `ChiSquare` | `chisquare`, `chi2_contingency` |
| Fisher exact | `FisherExact` | `fisher_exact` |
| Kolmogorov-Smirnov | `KolmogorovSmirnov` | `ks_2samp` |
| one-way ANOVA | `OneWayAnova` | `f_oneway` |
| Kruskal-Wallis | `KruskalWallis` | `kruskal` |
| Shapiro-Wilk | `ShapiroWilk` | `shapiro` |
| Bonferroni / BH / BY | `MultipleComparisons` | `false_discovery_control`, and arithmetic |

**Out of scope, and why**, so the boundary is a decision rather than an omission:

- **`ks_1samp`.** scipy's one-sample KS takes a *callable CDF*. This package
  has no distributions namespace to pass one from, and inventing one to serve a
  single test is a second package's worth of surface. Two-sample only.
- **`Stats.Regression`, `Cluster`, `Preprocessing`, `Survival`.** Separate
  checkboxes on #442, separate specs.
- **`nan_policy`.** scipy's three-valued policy is a convenience for its array
  API, not part of any test's definition. A NaN in the input propagates to the
  statistic and the p-value; a caller who wants `omit` filters the array in one
  line. No enum, no parameter, and the remarks say so.

## Public surface — 19 types, corrected from the design's 18

The design section presented 18 types and 18 sample files. Both numbers were
wrong, in opposite directions, and the spec is where they get fixed:

- **19 types, not 18.** scipy's `method='auto'` on `mannwhitneyu`, `wilcoxon`
  and `ks_2samp` switches between the exact distribution and the asymptotic
  approximation, which *changes the number returned*. A parameter that decides
  the answer cannot be hidden, so `ExactMethod` joins the enums. `NanPolicy`
  leaves, per Scope above. Net: +1.
- **14 sample files, not 18.** `tools/check_sample_coverage.py:34` enforces
  `CONVERTED` packages, and [decision 0041](../../decisions/0041-one-sample-file-per-public-class.md)
  excludes an enum: it is demonstrated through the class whose parameter it is.
  Five enums, fourteen classes and records.

```text
Lodestar.Stats
├── TTest                 Independent(a, b, Alternative, Variance)
│                         Paired(a, b, Alternative)
│                         OneSample(x, populationMean, Alternative)
├── MannWhitney           Test(x, y, Alternative, Continuity, ExactMethod)
├── Wilcoxon              Paired(x, y, ZeroMethod, Alternative, Continuity, ExactMethod)
│                         OneSample(d, ZeroMethod, Alternative, Continuity, ExactMethod)
├── ChiSquare             GoodnessOfFit(observed, expected)
│                         Contingency(table, Continuity)
├── FisherExact           Test(table, Alternative)
├── KolmogorovSmirnov     TwoSample(a, b, Alternative, ExactMethod)
├── OneWayAnova           Test(params double[][] groups)
├── KruskalWallis         Test(params double[][] groups)
├── ShapiroWilk           Test(x)
├── MultipleComparisons   Bonferroni(p)
│                         BenjaminiHochberg(p)
│                         BenjaminiYekutieli(p)
├── TestResult            (double Statistic, double PValue)
├── TTestResult           (double Statistic, double PValue, double Df)
│                             .ConfidenceInterval(level = 0.95) -> (double Low, double High)
├── Chi2ContingencyResult (double Statistic, double PValue, int Dof, double[][] ExpectedFrequencies)
├── KsResult              (double Statistic, double PValue,
│                          double StatisticLocation, int StatisticSign)
└── enums                 Alternative { TwoSided, Less, Greater }
                          Variance { Equal, Welch }
                          Continuity { Applied, None }
                          ExactMethod { Auto, Exact, Asymptotic }
                          ZeroMethod { Wilcox, Pratt, ZSplit }
```

The four result shapes are exactly what scipy returns, measured rather than
assumed — `ttest_*` carry `df`, `chi2_contingency` carries `dof` and
`expected_freq`, `ks_2samp` carries `statistic_location` and `statistic_sign`,
and the remaining eight carry `statistic` and `pvalue` and nothing else. Twelve
result records where four suffice would be eight reference pages and eight
sample files bought with no information.

`TTestResult.ConfidenceInterval` is a **method returning a named tuple**, not a
record: scipy exposes it as a method too (it takes a confidence level), and a
`ConfidenceInterval` record would be a twentieth public type and a
fifteenth sample file to carry two doubles.

Static classes throughout, `Metrics`' shape — arrays in, numbers out. Nothing is
fitted, so there is nothing to hold between two calls.

## The numerical layer

`internal`, under `Internal/`, exactly as `Lodestar.Metrics` holds its own. Not
`Lodestar.Stats.Special` and not `Lodestar.Abstractions`: a public special-function
namespace is a parity promise per function for a need #442 does not state, and
`Abstractions` would buy a published floor between two packages for code one of
them uses. Publishing it later stays possible; unpublishing it would not.

| function | what needs it |
| --- | --- |
| `LogGamma` (Lanczos) | the two regularized functions below, and Fisher's hypergeometric terms |
| `RegularizedIncompleteBeta` | Student *t* and Fisher *F* tails — `TTest`, `OneWayAnova` |
| `RegularizedIncompleteGamma` | the χ² tail — `ChiSquare`, `KruskalWallis` |
| `Erfc` | the normal tail — the asymptotic branches of `MannWhitney`, `Wilcoxon`, `ShapiroWilk` |
| `KolmogorovCdf` | KS, asymptotic series and small-sample exact |
| exact rank distributions | rank sums (`MannWhitney`), signed ranks (`Wilcoxon`) — dynamic programming over the counts, not a series |
| hypergeometric enumeration | `FisherExact` |
| Royston AS R94 | `ShapiroWilk`'s coefficients and p-value |

Royston's AS R94 and the continued-fraction expansions are **published
algorithm descriptions**, which is what [ADR 0003](../../decisions/0003-provenance-and-licensing.md)
requires. No reference implementation is transcribed. Reading scipy to diagnose
one failing case is diagnosis and stays allowed.

## Oracle discipline

`scipy 1.18.0` and `numpy 2.5.1`, already installed in `.venv-oracles` —
verified to export every call in the Scope table, `false_discovery_control`
included, so **no new development dependency is added**. Bonferroni is the one
family scipy does not cover; it is `min(p × n, 1)`, a definition rather than an
implementation, and its corpus is generated from that definition — the shape
[#526](https://github.com/CyrilB1531/lodestar/issues/526) used to generate the
BK-tree corpus by brute force.

### Two rules this package needs and the existing corpora did not

**1. Every case carries its full argument set. No default is relied on.**
Measured signatures:

```text
ttest_ind(a, b, *, equal_var=True, alternative='two-sided', ...)
mannwhitneyu(x, y, use_continuity=True, alternative='two-sided', method='auto', ...)
wilcoxon(x, y=None, zero_method='wilcox', correction=False, method='auto', ...)
chi2_contingency(observed, correction=True, ...)
ks_2samp(data1, data2, alternative='two-sided', method='auto', ...)
fisher_exact(table, alternative=None, ...)
```

`equal_var=True` means the default is **Student, not Welch**. `correction=True`
means Yates is applied, but for 2×2 tables only. `method='auto'` flips between
exact and asymptotic on sample size and ties. `fisher_exact`'s `alternative=None`
is a default in transition. Each of these silently decides the number, so the
generator passes every argument explicitly and each corpus case records what was
passed. A scipy upgrade that moves a default then fails the *Oracles are
reproducible* job loudly, instead of moving a frozen number quietly.

**2. A p-value is compared relatively; a statistic absolutely.**

The repository's tolerance is `1e-9` **absolute** (`tests/Lodestar.Text.Tests/Oracles/OracleAsserts.cs:89`,
`tools/compare_oracles.py:121`). Measured on ordinary cases:

```text
ttest_ind, two well-separated normal samples   p = 7.85e-26
f_oneway, three separated groups               p = 2.38e-53
chi2_contingency, [[100,10],[10,100]]          p = 3.52e-33
shapiro, 200 exponential draws                 p = 6.79e-16
```

At `1e-9` absolute, an implementation that returns `0.0` for every one of those
passes green. The gate would prove nothing about the tail — which is precisely
where a hand-written incomplete beta goes wrong.

So `StatsOracleAsserts` compares p-values at `1e-9` **relative**
(`|expected − actual| ≤ 1e-9 · |expected|`) and statistics at `1e-9` absolute,
and the corpus deliberately includes tail cases below `1e-15`.

`tools/compare_oracles.py` is **not** changed. Its subject is corpus
reproducibility, not assertion strength, and weakening one exact ordering to
serve one package is the shape [decision 0079](../../decisions/0079-tied-textrank-scores-canonicalize-by-phrase-not-blas.md)
already refused.

### Corpora

One file per family, `tests/oracles/stats_<family>.json`, each carrying a
corpus-identity fact — case count and `scipy.__version__` — so an empty `cases`
array cannot pass as green, the shape
`MatchRatingApproachOracleTests` established on [#313](https://github.com/CyrilB1531/lodestar/issues/313).

Each corpus spans, deliberately: the balanced case; unequal sample sizes; ties
(which switch `method='auto'` off exact); a tail case below `1e-15`; and the
degenerate input each test refuses.

## Placement and wiring

`src/Lodestar.Stats`, **core tier**, `net10.0;netstandard2.0`, no external
dependency, version **0.1.0** in its own `Version.props` per
[decision 0012](../../decisions/0012-per-package-versioning.md). It creates **no
inter-package edge** — nothing depends on it and it depends on nothing — so it
releases on its own schedule, as `Lodestar.Metrics` and `Lodestar.Conformal` do.

The wiring, each item a gate that fails without it:

- `Lodestar.slnx`, `src/Directory.Packages.props`, the two test projects
  including the `netstandard2.0` mirror with its `NetStandardAssemblyGuardTests`
  and `ProjectReference`-pinned dependencies (`check_netstandard_guards.py`);
- `docs/wiki-map.json` — a `Lodestar.Stats` entry with `pages` and a `covered`
  row pointing at `docs/reference/stats/tests`;
- `tools/check_nuspec_dependencies.py` — `Lodestar.Stats` in `EXPECTED` with an
  empty dependency set, which is what makes the core-tier rule of
  [decision 0076](../../decisions/0076-a-core-package-carries-no-external-dependency.md)
  fail a build rather than a review;
- `tools/check_sample_coverage.py` — `Lodestar.Stats` appended to `CONVERTED`,
  and the fourteen `<Type>Sample.cs` files that satisfies;
- `docs/reference/stats/` — one page per type and one per public method,
  roughly **36 pages**, each with an executed `// =>` fence;
- `docs/guides/hypothesis-testing.md`;
- `docs/equivalence.md` — one row per scipy call, in the same commit as the
  function;
- `CHANGELOG.md`, and one ADR recording the core-tier placement of the
  numerical layer.

## Benchmarks

Unlike `Lodestar.Conformal`, this domain **does** have a named .NET incumbent, so
the constraint #442 states is satisfied literally rather than by recording an absence:
`Accord.Statistics` 3.8.0 is archived but still installable, and `bench/` already
references incumbents this way — `Fastenshtein`, `Quickenshtein`,
`F23.StringSimilarity`, `Raffinert.FuzzySharp`, `Microsoft.ML.Tokenizers`,
`Microsoft.ML`.

`bench/Lodestar.Stats.Benchmarks` measures `TTest.Independent`,
`MannWhitney.Test` and `ChiSquare.Contingency` against
`Accord.Statistics.Testing`, registered in `bench/bench-map.json`
(`tools/check_bench_map.py`). The second implementation is worth more than the
timing: where Accord and scipy disagree, the corpus says which one this package
follows, and the guide records it.

`bench/README.md` gets how to run it; every number goes to
`docs/guides/performance.md` with its machine, per this repository's split.

## Testing

Per family: the oracle replay, the degenerate inputs it refuses
(`ArgumentException` on an empty sample, on mismatched paired lengths, on a
contingency table with a zero marginal), and the `netstandard2.0` mirror running
the same sources.

The numerical layer is `internal` and is tested through `InternalsVisibleTo`
against values the corpus does not otherwise reach — `RegularizedIncompleteBeta`
near `x = a/(a+b)` where the continued fraction switches branch, `LogGamma` at
half-integers, `KolmogorovCdf` in both its series.

**Read the test count, not the colour**: a `--filter` matching nothing exits
zero.
