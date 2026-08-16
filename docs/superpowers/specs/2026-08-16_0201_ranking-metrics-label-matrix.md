# 0201 — Ranking metrics, lot 2: the multilabel family over a boolean label matrix

**Issue:** [#201](https://github.com/CyrilB1531/lodestar/issues/201) ·
**Status:** proposed · **Date:** 2026-08-16

## Problem

[#173](https://github.com/CyrilB1531/lodestar/issues/173) names six metrics and its first lot
shipped four — `Ndcg`, `Dcg`, `TopKAccuracy` and `ReciprocalRank`, which all score **one ordered
list**. The other three take a **boolean label matrix**: one row per sample, one column per label,
several labels true at once. Measured by grep over `src/`, nothing names label ranking average
precision, coverage error or label ranking loss.

They share none of lot 1's tie handling and none of its input shape, which is why they were split
off. This lot is what lets #173 close.

## Everything below was measured, not transcribed

`scikit-learn` 1.9.0 in `.venv-oracles`, run from a neutral directory on 2026-08-16. The worked case
is `y_true = [[1, 0, 0], [0, 0, 1]]` against `y_score = [[0.75, 0.5, 1.0], [1.0, 0.2, 0.1]]`.

| case | LRAP | coverage | ranking loss |
| --- | --- | --- | --- |
| the worked case | `0.41666666666666663` | `2.5` | `0.75` |
| a row with **every** label true | `1.0` | `3.0` | `0.0` |
| a row with **no** label true | `1.0` | `0.0` | `0.0` |
| an all-false row beside a scoring one | `1.0` | `0.5` | `0.0` |
| every score equal, 2 of 3 relevant | `0.6666666666666666` | `3.0` | `1.0` |
| negative scores | `0.3333333333333333` | `3.0` | `1.0` |
| **one** label column | `1.0` | `ValueError` | `ValueError` |
| `sample_weight` summing to zero | `nan` | `ZeroDivisionError` | `ZeroDivisionError` |
| a negative `sample_weight` | `-0.33333333333333337` | `5.0` | `2.0` |
| a `NaN` in `y_score` | `ValueError` | `ValueError` | `ValueError` |
| a float `y_true` of `0.5` | `ValueError` | `ValueError` | `ValueError` |
| an integer `y_true` of `2` | `1.0` | — | — |
| 1-D input | `ValueError: binary format is not supported` | same | same |

Three of those decide the design, and one of them retires a question the issue asked.

**The tie order is not observable.** Permuting which of two tied columns carries the true label
leaves all three values unchanged: `lrap` ranks with `rankdata(…, "max")`, `label_ranking_loss`
groups with `np.unique` and `bincount`, and `coverage_error`'s own source says the max rank "has
been assigned to all tied values". Lot 1's stabilised descending order — and the introsort trap it
was built to survive — is irrelevant here.

**The three disagree about a single label column.** `lrap` accepts it and returns `1.0`; the other
two refuse it as "binary format is not supported". That is a divergence inside the reference, not a
gap in it.

**A float `y_true` is refused where an integer is accepted.** `0.5` raises
"continuous-multioutput format is not supported" while `2` scores. The boundary is scikit-learn's
type inference, and D1 makes it disappear rather than reproducing it.

## Decisions

### D1 — `yTrue` is a `ReadOnlySpan<bool>`

These three metrics are defined over a boolean label matrix, and the type says so. Lot 1's
`ReciprocalRank` takes a `ReadOnlySpan<double>` and reads "non-zero is relevant", which is the right
contract for a *relevance judgement* that may also be graded; here nothing is graded, and a `double`
would invite the question of what `0.5` means. Measured, that question is exactly the one
scikit-learn answers with an error message about `continuous-multioutput`, so a `bool` removes a
whole class of refusal instead of reproducing it.

The cost is one more shape in the package and a conversion for a caller holding `double`. It is
paid once, in a namespace that already carries two shapes.

### D2 — the rows arrive row-major with a `labelCount`, as everywhere else

```csharp
public static double Score(
    ReadOnlySpan<bool> yTrue, ReadOnlySpan<double> yScore,
    int labelCount, ReadOnlySpan<double> sampleWeight = default)
```

The shape lot 1, the regression metrics and `Silhouette` all take — a span cannot carry two
dimensions, and [`decisions/0021`](../../decisions/0021-multioutput-is-a-method-not-an-enum.md)
already ruled that a second dimension is a count rather than an overload.

### D3 — the names are spelled out

`LabelRankingAveragePrecision`, `CoverageError`, `LabelRankingLoss`. The package keeps an acronym
when the acronym *is* the metric's name — `Dcg`, `Ndcg`, `R2`, `F1`, `RocAuc`, `VMeasure` — and
spells the rest out, as `NormalizedMutualInformation` does where NMI is equally common. LRAP is not
as established as NDCG, and `LabelRankingLoss` keeps the "label" that separates it from a ranking
loss in general.

### D4 — `sampleWeight` comes in, and its degenerate cases diverge as the reference's do

All three Python functions take it and the classification metrics here already do. Two measured
behaviours travel with it, and both are reproduced rather than smoothed:

- **a weight vector summing to zero** raises for `CoverageError` and `LabelRankingLoss` with
  `numpy.average`'s "Weights sum to zero, can't be normalized." — the sentence the regression lot
  already reproduces — and returns `NaN` for `LabelRankingAveragePrecision`;
- **a negative weight** is accepted by all three and takes the result outside its natural range:
  `-0.33` for a metric documented in `[0, 1]`, `2.0` for a loss. That is the regression lot's rule
  too, where `[-1, -2, -3]` still scores.

### D5 — the single-column divergence is reproduced, not normalised

`LabelRankingAveragePrecision.Score` accepts `labelCount == 1`; the other two refuse it with
scikit-learn's sentence. Making the three agree would be inventing a divergence rather than
reproducing one, which
[`decisions/0007`](../../decisions/0007-metaphone-scope.md) and the stop-word
provenance decision have both already refused to do. The reference pages say which is which, beside
the number.

### D6 — no `Ranking.Descending`, and the corpus proves it

Measured above: the tie order is unobservable in all three. `Internal/LabelRanking.cs` computes the
**max rank** of each column — every member of a tied group takes the group's worst rank — and the
relevant/irrelevant split, and it never sorts for order. A fixture wider than 16 columns goes into
the corpus anyway, because that is precisely the width at which lot 1's assumption broke, and a
claim of indifference is worth a case rather than a sentence.

### D7 — the relevant/irrelevant split is shaped for #210 as well

[#210](https://github.com/CyrilB1531/lodestar/issues/210) brings `average_precision_score`, whose
multilabel form takes this same boolean matrix. `Internal/LabelRanking.cs` exposes the split and the
max-rank as internals that a fourth metric can call, so #210 extends them instead of writing a
second copy. It does **not** implement average precision here: that is #210's lot, and widening this
branch to reach it is what `CONTRIBUTING.md` forbids.

## What lands with the code

- Three types under `src/Lodestar.Metrics/`, one `Internal/LabelRanking.cs`.
- One frozen corpus, `tests/oracles/label_ranking.json`, carrying all three metrics per fixture —
  they take the same two inputs, so a separate file per metric would triple the fixtures to say the
  same thing. Generated by `tools/generate_oracles.py` from a neutral directory, read on the
  generator's own exit code.
- Facts for what a corpus cannot state: the single-column divergence, the zero-sum weight
  divergence, the negative weight leaving the range, and the tie indifference asserted **as an
  indifference** — two permutations compared to each other rather than two numbers compared to a
  frozen one.
- Three type pages and three member pages under `docs/reference/metrics/ranking/`, the index
  extended, `covered` already naming the directory.
- One `docs/equivalence.md` row per function, in the same commit as the function.
- The three types exercised from `samples/Lodestar.Sample/Lot5Metrics.cs`, and a `CHANGELOG.md`
  entry under the unreleased `Lodestar.Metrics`.
- A code review of the diff before the pull request exists, as lot 1 did — the gates check
  declarations and replay a corpus; none of them reads the arithmetic, and on lot 1 a review found
  eight defects the corpus could not see.

## Risks

**The corpus can agree with a wrong implementation on easy rows.** Lot 1's lesson exactly: its
fixtures were 4 and 6 columns wide and could not see that `Array.Sort` stops being stable at 17. The
mitigation is the same shape — fixtures chosen to separate implementations rather than to exercise
them, and a fact that counts how many of them actually do.

**`coverage_error` is not bounded by the label count.** A row with nothing relevant contributes `0`,
so the mean can sit below `1` — measured, `0.5` on two rows. An implementation that treated the
empty row as "all labels covered" would return `2.0` on the same input and look plausible.

**The pull request closes two issues.** `Closes #201` and #173, whose six metrics are then all
present. Lot 1's body tried to say the opposite of that and closed #173 by accident, because
GitHub's keyword parser ignores negation — so the body carries the keyword deliberately this time,
once, next to each number.
