# 0173 — Ranking metrics, lot 1: implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development
> (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use
> checkbox (`- [ ]`) syntax for tracking.

**Spec:** [`2026-08-16_0173_ranking-metrics.md`](../specs/2026-08-16_0173_ranking-metrics.md) ·
**Issue:** [#173](https://github.com/CyrilB1531/lodestar/issues/173) ·
**Branch:** `feat/173-ranking-metrics-ordered-list`

**Goal:** `Lodestar.Metrics` scores an ordered list of documents — `Ndcg`, `Dcg`, `TopKAccuracy` at
scikit-learn parity, and `ReciprocalRank`, which has no reference and says so.

## Global Constraints

- English everywhere; no `feat:`/`fix:` prefix; `Closes #173` in the pull-request body only.
- Warnings are errors on both target frameworks, with SonarAnalyzer running in the build.
- Comment budgets: two lines inline, eight of prose in XML documentation. The `long-comment:` marker
  is for length the *material* imposes, not for room ([#187](https://github.com/CyrilB1531/lodestar/issues/187)).
- Oracles generated from a neutral directory, reading **the generator's own exit code**.
- Every new public type exercised from `samples/Lodestar.Sample` — the packaging gate.
- A member page per method under `docs/reference/metrics/ranking/`, with its type page and index,
  `covered` extended in the same commit ([#189](https://github.com/CyrilB1531/lodestar/issues/189)).
- **A code review before the pull request exists** — the gates check declarations and replay a
  corpus; none of them reads the arithmetic ([#172](https://github.com/CyrilB1531/lodestar/issues/172)'s lesson).

---

## Task 1 — the frozen corpus, ties in three shapes

- [x] **Step 1: Fixtures that separate the implementations, not just exercise them.** Ties are where
  a plausible implementation passes by agreeing on easy cases, so the corpus holds: every score
  tied, two tied among distinct scores, a tie spanning the `k` boundary, a perfectly ordered list, a
  reversed one, all-zero relevance, and a `k` past the label count.
- [x] **Step 2: `generate_ranking` in `tools/generate_oracles.py`**, emitting for each fixture:
  `dcg`, `dcg_ignore_ties`, `dcg_log_e`, `ndcg`, `ndcg_ignore_ties`, `ndcg_at_k`, and for the
  classification-shaped fixtures `top_k_accuracy` at two values of `k`, normalized and not.
- [x] **Step 3: Generate from `/tmp` with `PYTHONSAFEPATH=1`**, read the generator's exit code, and
  check the tied cases actually differ between `ndcg` and `ndcg_ignore_ties` — a corpus where they
  agree everywhere would prove nothing about the hard half.

## Task 2 — the three metrics with an oracle

- [x] **Step 1: `Internal/Ranking.cs`** — the discounted gain of one row, the ideal gain, and the
  tie-averaging. The gains are **linear**: `Σ relevance / log(rank + 1)`, measured
  `4.7618595071429155` where the exponential form gives `9.392789260714373`.
- [x] **Step 2: The tie rule.** scikit-learn averages the discounted gain over the permutations of
  equal scores, which is a mean of the tied group's gains times the sum of their discounts — not a
  permutation enumeration. Implement the closed form, and let the corpus judge the arithmetic.
- [x] **Step 3: `Dcg` with `logBase`, `Ndcg` without**, mirroring the reference's own surface, both
  with `k` and `ignoreTies`.
- [x] **Step 4: `TopKAccuracy`**, with `normalize` — `false` returns a count, measured `3.0`.
- [x] **Step 5: Clear the analyzers before the first green build**, not after.

## Task 3 — `ReciprocalRank`, and the ADR that admits it

- [x] **Step 1: Write the ADR first**, because it is what makes the code admissible: the parity rule
  set aside, what replaces it (a pinned definition and hand-written tests), and **what would retire
  the exception** — a reference implementation worth freezing. It is about the rule, not about this
  metric.
- [x] **Step 2: `ReciprocalRank.Score`** — the reciprocal of the rank of the first relevant
  document, averaged over queries; a query with no relevant document contributes `0`.
- [x] **Step 3: Tests that pin the definition**, one per choice the definition makes, so a future
  change to it fails rather than drifts.

## Task 4 — the tests

- [x] **Step 1: Replay the corpus**, one theory row per fixture so a failure names it.
- [x] **Step 2: Facts for what a corpus cannot say** — the single-document refusal with
  scikit-learn's sentence, `k` past the label count, all-zero relevance, and the `ignoreTies`
  difference asserted as a *difference* rather than as two numbers.
- [x] **Step 3: Read the count, not the colour.**

## Task 5 — the documents and the gates

- [x] **Step 1: Reference pages** — index, type pages, member pages, `covered` extended. The linear
  gain and the tie averaging go on the pages **next to the number**, because a reader comparing
  against a paper will find the other value.
- [x] **Step 2: An `equivalence.md` row per function**, including the row that says MRR has no
  counterpart and why.
- [x] **Step 3: The sample, the changelog, and the whole-repository battery** — both frameworks,
  format, markdownlint, the four guards each on its own exit code, the packaging gate with a purged
  package cache, and the snippets **executed**.

## Task 6 — review, then the pull request

- [ ] **Step 1: Ask for a code review of the diff.** What it is for here: whether the closed-form
  tie averaging is the same function scikit-learn computes, and whether the pages say what the code
  does rather than what the spec hoped.
- [ ] **Step 2: Act on it, then open the pull request and assign it.** `gh pr edit` fails silently
  on this repository; assignment and body edits go through `gh api`.

---

## What this plan does not do

Lot 2 — `Lrap`, `CoverageError`, `RankingLoss` — which take a boolean label matrix and share none of
the tie handling. Its own branch, after this one, tracked as
[#201](https://github.com/CyrilB1531/lodestar/issues/201). **[#173](https://github.com/CyrilB1531/lodestar/issues/173)
names all six metrics, so this branch does not close it**; #201 is what carries the other three, and
the pull-request body says so rather than `Closes #173`.
