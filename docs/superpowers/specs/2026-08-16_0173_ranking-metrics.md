# 0173 — Ranking metrics: the ordered list first, the multilabel family second

**Issue:** [#173](https://github.com/CyrilB1531/lodestar/issues/173) ·
**Status:** proposed · **Date:** 2026-08-16

## Problem

`Lodestar.Metrics` scores classification, regression and clustering. It scores no ranking: measured
by grep over `src/`, nothing names NDCG, DCG, top-k accuracy, coverage error or ranking loss.

## Everything below was measured, not transcribed

`scikit-learn` 1.9.0 in `.venv-oracles`, run from a neutral directory on 2026-08-16.

| case | measured |
| --- | --- |
| `dcg_score` on a perfectly ordered list | `4.761859507142915` |
| the same sum computed with **linear** gains by hand | `4.7618595071429155` |
| the same with **exponential** gains (`2^rel − 1`) | `9.392789260714373` |
| `ndcg_score` with every score tied | `0.8069136566720543` |
| the same with `ignore_ties=True` | `0.6138273133441086` |
| `ndcg_score` with `k` larger than the label count | `1.0` — not an error |
| `ndcg_score` on all-zero relevance | `0.0` |
| `ndcg_score` on a single document | `ValueError: Computing NDCG is only meaningful when there is more than 1 document` |
| `top_k_accuracy_score(k=2, normalize=False)` | `3.0` — a count, not a fraction |
| a function matching `reciprocal` in `sklearn.metrics` | **none** |

Two of those decide the design. **The gains are linear**, where much of the literature and many
implementations use `2^rel − 1` — a reader who checks our number against a paper will find a
different one, and the reference page has to say so. **Ties are averaged over permutations** by
default, which is a 30% difference on the measured case and the kind of behaviour a reader takes
for a bug.

## Decisions

### D1 — two lots, the ordered list first

`Ndcg`, `Dcg`, `TopKAccuracy` and `ReciprocalRank` score one ordered list of documents. `Lrap`,
`CoverageError` and `RankingLoss` take a boolean label matrix and are a coherent multilabel family.
The first lot carries the tie handling and the `k` parameter, which is where the design work is;
the second depends on none of it and is tracked as
[#201](https://github.com/CyrilB1531/lodestar/issues/201), so splitting the issue does not lose
half of it.

### D2 — 2-D inputs arrive row-major with a count

```csharp
double Ndcg.Score(ReadOnlySpan<double> yTrue, ReadOnlySpan<double> yScore, int labelCount)
```

One row per query, `labelCount` values each, as the regression metrics already take 2-D targets
([0021](../../decisions/0021-multioutput-is-a-method-not-an-enum.md)). There is no 2-D overload
because a span cannot carry one.

### D3 — the default averages ties, and `ignoreTies` is offered

Reproducing only `ignore_ties=True` would be simpler and would silently disagree with scikit-learn
by 30% on tied scores. Reproducing only the default would deny a caller the cheaper path on data
with no ties. Both ship, the default matching scikit-learn's.

### D4 — linear gains, and `logBase` on `Dcg` alone

`Dcg.Score` takes `logBase` (default 2) because `dcg_score` does; `Ndcg.Score` does not, because
`ndcg_score` does not. The gain form is not a parameter on either side: it is linear, and the
reference page says so next to the number a paper would give instead.

### D5 — the refusals are scikit-learn's, with its sentences

A single document raises, carrying `Computing NDCG is only meaningful when there is more than 1
document`. A `k` past the label count does **not** raise — measured — and neither does all-zero
relevance, which scores `0`.

### D6 — `ReciprocalRank` ships without an oracle, and says so everywhere

scikit-learn has no MRR, so this is the first member of the package whose correctness does not rest
on a frozen corpus. It ships anyway, and the cost of that is paid in three places:

- **an ADR** recording that the parity rule is being set aside here, what replaces it, and what
  would retire the exception (a reference implementation worth freezing);
- **hand-written tests** that pin the definition rather than replay a corpus: the reciprocal of the
  rank of the first relevant document, averaged over queries, with a query holding no relevant
  document contributing `0`;
- **a warning on its reference page and its equivalence row**, so a reader learns from the
  documentation — not from a surprise — that this one number is not verified against a reference.

The definition above is a choice among variants and is pinned by those tests, which is the whole
point of writing it down.

## What lands with the code

A frozen corpus for the five oracle-backed functions at `1e-9`; an `equivalence.md` row per
function, including one that says MRR has no counterpart and why; member pages under
`docs/reference/metrics/ranking/` with their type pages and index, `covered` extended in the same
commit; and the sample exercising every new public type.

## Risks

- **The tie-averaging is subtle to implement and easy to get plausibly wrong.** The corpus must hold
  tied scores in more than one shape — all tied, two tied among distinct, ties spanning the `k`
  boundary — or the implementation will pass by agreeing on the easy cases.
- **`ReciprocalRank` sets a precedent.** The ADR has to be about the rule, not about this metric, or
  the next unproven member will cite it as a habit rather than as an exception.
