# 0194 — Move the project to Lodestar, implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development
> (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use
> checkbox (`- [ ]`) syntax for tracking.

**Spec:** [#194](https://github.com/CyrilB1531/data.net/issues/194) is the spec — it carries the
reasoning, the name, and the rule about what must not be renamed. This plan carries the order, the
version numbers, and the four carriers the issue does not name.

**Goal:** the four packages, the repository, the published metadata and the analysis identity all
say `Lodestar`, with no window in which `main` cannot be cloned and built.

## Decisions taken on 2026-08-16, before any file moved

- **Rename-only releases.** `Lodestar.Text`, `Lodestar.Embeddings`, `Lodestar.Fuzzy` publish
  **0.3.1**, `Lodestar.Metrics` publishes **0.1.1**. Same code, new id, on all four — that is the
  promise a consumer needs, and a patch number is what makes it.
- **`Lodestar.Metrics 0.1.1` is cut from the tag `DataNet.Metrics/v0.1.0`, not from `main`.** `main`
  carries six clustering metrics that 0.1.0 does not, and shipping new public API under a patch
  number would make the version lie. The clustering lot follows as `Lodestar.Metrics 0.2.0`.
- **`Lodestar.Text` publishes first, alone.** It depends on no other package, so its publication is
  what lets `Fuzzy`'s floor name an id that exists. No guard is suspended at any point: the job that
  forbids a `ProjectReference` under `src/` stays on throughout.

## Global Constraints

- Everything in English. No `feat:`/`fix:` prefix on commit messages; `Closes #194` in the body of
  the last pull request only — the earlier ones reference it without closing it.
- `dotnet build DataNet.slnx -c Release` treats warnings as errors on both target frameworks.
- **Rename what will be built next; leave what has already shipped.** The issue lists the four
  historical records that must not move. Task 7 checks that rule rather than trusting it.
- A `git mv` per file, never a `sed` over paths: 292 files carry the name, and the count grows with
  every lot landed before this one starts. Re-measure at the start.
- The reference gate, the snippet runner and the packaging gate all read the package ids. Each one
  refuses a half-rename, which is what makes this safe to do in lots.

---

## Task 1 — measure, and freeze the target

- [ ] **Step 1: Re-count what carries the name.** `git ls-files | grep -c DataNet` for file names,
  and the same over content per area. The issue said 282 files; it was 292 on 2026-08-16. Record
  today's number in the pull request, because it is the only way a reviewer can tell a complete
  sweep from a partial one.
- [ ] **Step 2: Confirm the ids are still free on nuget.org** — `Lodestar`, `Lodestar.Text`,
  `Lodestar.Embeddings`, `Lodestar.Fuzzy`, `Lodestar.Metrics`. The issue verified this; verify again
  the day the work starts, since nothing reserves them until the first publication.
- [ ] **Step 3: List the four carriers the issue does not name**, so they are not rediscovered
  under a red CI run: `.github/workflows/wiki.yml:53` (the literal package list),
  `wiki.yml:13` and `release.yml:16` (the `DataNet.*/v*` tag triggers), `docs/wiki-map.json`
  (package keys **and** covered namespaces), `samples/NuGet.config:35` (the
  `<package pattern="DataNet.*" />` source mapping).

## Task 2 — `Lodestar.Text` alone, and published

**Files:** `src/DataNet.Text/**` → `src/Lodestar.Text/**`, its two test projects, the solution, and
the identity carriers that name Text.

- [ ] **Step 1: Move the project and its mirrors**, `git mv` throughout: the package id, the
  assembly name, the root namespace, `Version.props`'s property name
  (`DataNetTextVersion` → `LodestarTextVersion`), and the `*.NetStandard.Tests` twin that links the
  same sources.
- [ ] **Step 2: Rewrite the namespaces in the sources that ship**, and in the tests, benchmarks,
  samples and documentation fences that use them — 87 documentation files carry
  `using DataNet.…` inside **executed** fences, so the snippet runner is the check.
- [ ] **Step 3: `docs/wiki-map.json`** — the `DataNet.Text` package key and the
  `DataNet.Text.Distances` covered namespace. The reference gate compares that string against the
  assembly, so a miss reports a missing entry rather than a rename.
- [ ] **Step 4: Set the version to 0.3.1** in `src/Lodestar.Text/Version.props`, and say in
  `CHANGELOG.md` that it is `DataNet.Text 0.3.0` under a new id, with no behaviour change.
- [ ] **Step 5: Verify.** Both frameworks, `dotnet format`, markdownlint, the three guards, the
  packaging gate with a fresh `pack` and an isolated `NUGET_PACKAGES` — deleting the cached
  extracted package first, because a repacked *same* version is not re-restored.
- [ ] **Step 6: Review, then the pull request.** Merge, tag `Lodestar.Text/v0.3.1`, and confirm the
  release workflow fired — its trigger is one of the four carriers, so this is the first test of
  Task 1's step 3.
- [ ] **Step 7: Confirm the package is on nuget.org** before Task 3 starts. Everything below
  depends on that id existing.

## Task 3 — the other three, and the floor

- [ ] **Step 1: Move `Embeddings`, `Fuzzy` and `Metrics`** the way Task 2 moved `Text`.
- [ ] **Step 2: Point the floor at `Lodestar.Text 0.3.1`** in `src/Directory.Packages.props`, and
  check `tools/check_version_floor.py` still reads it — it names the packages.
- [ ] **Step 3: The remaining carriers** — the wiki workflow's literal package list, both tag
  triggers, the sample's source mapping, and the `Version.props` property names.
- [ ] **Step 4: Versions 0.3.1, 0.3.1 and — for Metrics — leave `main` at 0.2.0.** `main`'s Metrics
  is not 0.1.0 and must not claim to be; its 0.1.1 is Task 5's business.
- [ ] **Step 5: Verify, review, pull request, tag** `Lodestar.Embeddings/v0.3.1` and
  `Lodestar.Fuzzy/v0.3.1`.

## Task 4 — the repository, and the analysis identity

- [ ] **Step 1: Rename the repository** `data.net` → `lodestar`. The wiki follows with no edit; the
  248 absolute links keep resolving through GitHub's redirect. **Never create a repository named
  `data.net` under this account afterwards** — it breaks every redirect at once.
- [ ] **Step 2: `RepositoryUrl` and `PackageProjectUrl`** in `Directory.Build.props`, which are
  stamped into every package.
- [x] **Step 3: SonarCloud — nothing to do, measured.** The key stays `CyrilB1531_data.net` (it is
  an internal identifier, and changing it would start the analysis history over), and the binding
  followed the rename on its own: GitHub keeps the repository id `1322216727` across it, and
  SonarCloud binds to that. Verified on PR #198, the first of the renamed repository:
  `SonarCloud Code Analysis` decorated it, key unchanged, no action taken.

- [x] **Step 4: nuget.org Trusted Publishing — the policy does NOT follow the rename**, and this
  step is here because the plan did not have it. The publish at 06:29 succeeded, the rename
  happened, and the publish at 07:27 failed:
  `Token exchange failed (HTTP 401) … No matching trust policy owned by user 'CyrilB1531' was
  found.` nuget.org's tooltip says the permanent id is used for validation, which is what misled
  this plan — the policy has to be **deleted and recreated** against the renamed repository,
  because its edit form exposes only the policy name, the workflow file and the environment.
  Recreated, the same run succeeded on re-attempt.

  The rule for whoever renames next: **the id survives, the policy does not follow it.** Publish
  one cheap package first, and find out before four tags are waiting.

## Task 4b — what publishing four tags at once broke

- [ ] **The wiki lost two archives, and nothing failed.** `wiki.yml` declares
  `concurrency: group: wiki`, and GitHub keeps at most one *pending* run per group —
  `cancel-in-progress: false` protects a running job, not a queued one. Four tags pushed together
  left `Fuzzy 0.3.1` and `Metrics 0.2.0` with no frozen pages, while every run reported success or
  cancellation. Recorded as [#199](https://github.com/CyrilB1531/lodestar/issues/199) and fixed
  there: any run now archives every released version the wiki does not hold, each from its own tag.

## Task 5 — `Lodestar.Metrics 0.1.1`, from the tag

- [ ] **Step 1: Branch from `DataNet.Metrics/v0.1.0`**, apply the rename to that content, and
  nothing else. This is the one release in the sequence that is not cut from `main`, and the reason
  is in the decisions above.
- [ ] **Step 2: Publish and tag** `Lodestar.Metrics/v0.1.1`.
- [ ] **Step 3: `main` then ships `Lodestar.Metrics 0.2.0`** with the clustering lot, which is the
  version it already declares.

## Task 6 — retire the old ids

- [ ] **Step 1: Deprecate the four `DataNet.*` packages on nuget.org**, each pointing at its
  replacement. Deprecation, not unlisting: deprecation surfaces a message in the IDE, unlisting is
  silent.
- [ ] **Step 2: Request the `Lodestar.*` prefix reservation**, which is the thing that stops this
  recurring in the other direction.

## Task 7 — prove the history was left alone

- [ ] **Step 1: Check the four records the issue protects** are untouched:
  `docs/superpowers/specs/` and `plans/`, `CHANGELOG.md`'s 0.1.0–0.3.0 entries, `docs/decisions/`
  where an ADR names a past decision, and any archived `docs/reference/` page. A `git diff` over
  those paths for the whole branch should be empty except where an entry describes work done *after*
  the rename.
- [ ] **Step 2: Read the wiki as a reader**, not as a tree: the channels, the banner naming the
  released version, and one archived page if a tag has produced one by then.

---

## What this plan does not do

The `Lodestar` root package id stays unpublished. It is reserved by the prefix request rather than
by an empty package, and nothing in the four needs it.
