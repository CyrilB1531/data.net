# 0172 — Clustering metrics, implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development
> (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use
> checkbox (`- [ ]`) syntax for tracking.

**Spec:** [`2026-08-15_0172_clustering-metrics.md`](../specs/2026-08-15_0172_clustering-metrics.md) ·
**Issue:** [#172](https://github.com/CyrilB1531/data.net/issues/172) ·
**Branch:** `feat/172-clustering-metrics-agreement` (lot 1), a second branch for lot 2

**Goal:** `DataNet.Metrics` scores a clustering against a reference partition at scikit-learn
parity — five agreement metrics first, silhouette second.

**Written after lot 1 was implemented, not before.** The session went from the spec straight to the
code on a "go", and the plan is the artefact that was skipped. What Tasks 1–4 record is therefore
what *was* done, in the order it was done, with the traps that were hit; Tasks 5–7 are ahead of the
work and are a plan in the ordinary sense. The distinction is marked per task rather than hidden.

## Global Constraints

- Everything in English — code, comments, documents, commit messages, pull-request bodies.
- Commit messages carry no `feat:`/`fix:` prefix; the pull request body carries `Closes #172`.
- `dotnet build DataNet.slnx -c Release` treats warnings as errors, on both target frameworks, with
  SonarAnalyzer running in the build.
- Comment budgets: two lines inline, eight lines of prose in XML documentation. Past either, the
  block's first line carries `long-comment:` **and a reason the material imposes** — see
  [#187](https://github.com/CyrilB1531/data.net/issues/187): the marker is not a budget to spend.
- Oracles are generated from a neutral working directory, and **the generator's own exit code is
  read**, never a pipeline's.
- Every new public type must be exercised from `samples/DataNet.Sample`, by a member reference —
  the packaging gate of ADR 0009.
- A reference page per member, with its type page and the namespace index, per
  [#189](https://github.com/CyrilB1531/data.net/issues/189). `covered` gains the directory in the
  same commit, so the gate enforces the pages from the first one.

---

## Task 1 — the frozen corpus *(done)*

- [x] **Step 1: Measure the degenerate cases before deciding anything.** Twelve partitions through
  `adjusted_rand_score`, `normalized_mutual_info_score` and
  `homogeneity_completeness_v_measure` on scikit-learn 1.9.0. The table is in the spec; the two
  answers nobody predicts are that an empty input and a single sample both score `1`.
- [x] **Step 2: `generate_clustering_agreement` in `tools/generate_oracles.py`**, registered as
  `clustering_agreement.json`, twelve cases including the four degenerate ones.
- [x] **Step 3: Generate from `/tmp` with `PYTHONSAFEPATH=1`, and read the generator's exit code.**
  `exit=0`, `clustering_agreement.json: 12 cases`.

## Task 2 — the five metrics *(done)*

- [x] **Step 1: `Internal/Contingency.cs`** — a sparse contingency table, its row and column totals,
  `MutualInformation()` and `Entropy()`. Sparse because a dense table is `n²` when every sample is
  its own cluster, which is a case the reference scores rather than refuses.
- [x] **Step 2: Follow scikit-learn's arithmetic, not the textbook's.** `log(nij) - log(n)` rather
  than `log(nij / n)`, and the outer term as `-log(ai·bj) + log(n) + log(n)`. Written the obvious
  way the two disagree in the last places, and `1e-9` would catch it.
- [x] **Step 3: `AdjustedRand`, `NormalizedMutualInformation`, `Homogeneity`, `Completeness`,
  `VMeasure`**, plus `Internal/Cluster.cs` for the half homogeneity and completeness share.
- [x] **Step 4: Clear what the build's analyzers say** — S3453, S2234 (twice), S1244, S3267 and a
  collection-expression error, all before a first green build.

## Task 3 — the tests *(done)*

- [x] **Step 1: Replay the corpus**, one theory row per case so a failure names it.
- [x] **Step 2: Five facts for what the corpus cannot say** — the empty input, label renaming, the
  mirror between homogeneity and completeness, the split-everything case where homogeneity says `1`
  and adjusted Rand says `0`, and the length refusal.
- [x] **Step 3: Read the count**, not the colour: 17 tests, first run green.

## Task 4 — the documents *(done)*

- [x] **Step 1: The reference pages** — `docs/reference/metrics/clustering.md`, five type pages,
  five member pages, and `covered` gaining the directory in the same commit.
- [x] **Step 2: A `docs/equivalence.md` row per function**, including the two rows that record what
  is *not* reproduced: `average_method` and `beta`.
- [x] **Step 3: Run the snippets, not only compile them.** This is where the gate earned its keep:
  `vmeasure-score.md` promised `0.8000…` where the code produces `0.8132898335036762`. Corrected to
  `0.8132…`; 78 snippets run, 0 skipped.

## Task 5 — the sample, the changelog, the review and the verification *(done)*

- [x] **Step 1: Exercise all five types from `samples/DataNet.Sample`.** A new public type that no
  lot references fails the packaging gate, and the gate needs a fresh `pack` **and** an isolated
  `NUGET_PACKAGES` — with the trap this session hit: repacking the *same* version does not refresh
  an already-restored package, so the isolated cache directory for that package is deleted first.
- [x] **Step 2: `CHANGELOG.md`** — an `Unreleased` entry under `DataNet.Metrics`.
- [x] **Step 3: Verify the repository.** Both frameworks, `dotnet format --verify-no-changes`,
  markdownlint, `check_comment_length.py --report` (reporting the marker count, per #187),
  `check_machine_paths.py`, `check_version_floor.py`, and the oracle drift job — which is flaky, so
  a red one is re-run before it is believed.
- [x] **Step 4: Ask for a code review of the diff, before the pull request exists.** Neither the
  gates nor CI is a review: they check that the declarations match the assembly, that the snippets
  run and that the corpus replays, and none of them reads the arithmetic. What a review is for here
  is the part no gate can see — that `Contingency`'s sparse keying is right for a labelling with
  more than `2^31` distinct labels, that `MutualInformation`'s grouping still matches the reference
  it cites, and that the five member pages say what the code does rather than what the spec hoped.
- [x] **Step 5: Act on what it says**, then commit, push, open the pull request and assign it.
  `gh pr edit` fails silently on this repository; assignment and body edits go through `gh api`.
  The review returned four findings and **two of them were not on this list**: `Completeness.Score`
  reversed the labellings before validating, so a length mismatch reported the two lengths under
  each other's names; and `The_map_declares_both_directories` was never extended to the clustering
  directory, so dropping it from `wiki-map.json` would have disabled that gate in silence. Both
  fixed here. It also re-derived the arithmetic independently — 4000 random labellings against
  scikit-learn 1.9.0, worst deviation 1.25e-15.

## Task 6 — silhouette *(lot 2, done but for the review and the pull request)*

- [x] **Step 1: Two entry points and no metric zoo** — a precomputed distance matrix, and feature
  vectors with the euclidean distance. Measured: the two paths give the identical `double`
  (`0.9738594604105609` on the spec's fixture), so they are one computation with two entries. Not
  two overloads, though: the signatures collide and the analyzers refuse the `double[,]` that would
  have separated them, so the precomputed path is `ScoreFromDistances`. The spec records it.
- [x] **Step 2: `Silhouette.PerSample` beside `Score`**, the score being the mean of the samples.
- [x] **Step 3: Reproduce the refusal.** Fewer than two labels, or as many labels as samples, is
  `ValueError` there and `ArgumentException` here, carrying scikit-learn's own sentence.
- [x] **Step 4: A cluster of one sample contributes `0.0`** for that sample — measured, and worth a
  fact of its own because it is the case a reader would expect to divide by zero.
- [x] **Step 5: Corpus, pages, equivalence row, sample usage**, as Tasks 1–5.

## Task 7 — close the loop

- [x] **Step 1: `docs/equivalence.md`'s "not shipped yet" row for silhouette is removed** when lot 2
  lands, not left to rot. Replaced by three rows: the two entry points and `silhouette_samples`.
- [x] **Step 2: The spec's status becomes `implemented`**, with what the implementation added to it
  — including the two example values the executed snippets caught as wrong.

---

## What this plan does not do

`DataNet.Metrics` has two more clustering families waiting — the pair-counting relatives of the Rand
index (Fowlkes-Mallows, adjusted mutual information) and the internal indices that need the feature
matrix (Calinski-Harabasz, Davies-Bouldin). They are
[#191](https://github.com/CyrilB1531/data.net/issues/191) and
[#192](https://github.com/CyrilB1531/data.net/issues/192), filed on 2026-08-15 because this
sentence claimed they existed before they did. #192 waits on lot 2: it inherits whatever surface
`Silhouette` settles. Silhouette is in scope here only because #172 names it.
