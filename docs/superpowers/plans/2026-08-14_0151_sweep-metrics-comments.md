# Sweep `DataNet.Metrics`' comments — implementation plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring `src/DataNet.Metrics` inside the comment budgets, and give every claim there a pointer to
what would check it.

**Architecture:** Four tasks, grouped by file so a member's inline comments and its XML documentation move
together — they usually say the same thing twice, and fixing them apart duplicates the work. Each task ends
with its files clean under `tools/check_comment_length.py`.

**Tech Stack:** C#, the guard from #150, the oracle corpora under `tests/oracles/`.

**Spec:** `docs/superpowers/specs/2026-08-14_0134_claims-and-comment-discipline.md`

**Issue:** [#151](https://github.com/CyrilB1531/data.net/issues/151) · **Part of:** [#134](https://github.com/CyrilB1531/data.net/issues/134) ·
**Branch:** `docs/151-sweep-metrics-comments`, off `main` at `8b1e0d1` (already created)

## What the zone holds, measured after the suppression scoping

| | blocks | prose lines |
| --- | ---: | ---: |
| XML documentation, past 8 | 34 | 605 |
| inline, past 2 | 40 | 256 |
| **total** | **74** | **861** |

Across 25 files. Separately, **162 comment lines name a reference library and 0 cite anything** — the worst
ratio in the tree, and why this zone goes first: the oracle corpora make citing them nearly free.

## Global Constraints

- **Relocate, do not delete.** These 861 lines hold measured scikit-learn parity arguments —
  `BalancedAccuracy`'s `<remarks>` cites `balanced_accuracy_score([1,1], [1,1], adjusted=True)` returning
  `nan`. That class of prose is what makes a divergence reviewable. **An ADR is where it goes when it
  outgrows its budget; the bin is not.**
- **A wrong claim in a comment is a defect** here, and this lot is about claims. For every sentence you
  keep, ask what would check it and whether you ran it. Your report carries one line per claim you cite.
- **Every public member keeps its XML documentation**, naming the scikit-learn function it matches —
  `CLAUDE.md` requires it. Trimming a `<remarks>` never means deleting a `<summary>`, a `<param>` or an
  `<exception>`; those do not spend the budget anyway.
- **The machine is shared.** Every `dotnet` command goes through `../data.net/.dotnet-guarded`.
- Warnings are errors repository-wide. Two target frameworks; the suite runs twice.
- Everything in English. Commit messages carry no `feat:`/`fix:` prefix.
- Per task the gate is:

  ```bash
  ../data.net/.dotnet-guarded dotnet build DataNet.slnx -c Release
  ../data.net/.dotnet-guarded dotnet test DataNet.slnx -c Release
  python3 tools/check_comment_length.py | grep '^src/DataNet.Metrics/'   # your files gone
  ```

  **Read the test count, not the colour.**

## The triage, which is the whole of the work

Every block gets one of four outcomes. Decide by asking what the prose is *for*, not by counting lines.

1. **It paraphrases the code below it.** Cut it. This is the cheapest and commonest outcome for inline
   blocks, and the rule's first sentence — a comment says why, never what.
2. **It is the reason for something, and fits.** Trim to the budget while keeping the reason. Most 3-to-5
   line inline blocks are one sentence padded into three.
3. **It is a measured argument that outgrows the budget.** Move it to `docs/decisions/` and cite the ADR
   from one line. `docs/decisions/` already holds 22; a new one needs the shape the others use.
4. **It genuinely needs the room where it is.** Mark it `long-comment:` with a reason a reviewer would
   accept. **This should be rare** — if a task marks more than two or three, that is a signal the prose
   wanted an ADR instead, and the report should say so.

Alongside, for every comment naming scikit-learn, numpy or another reference: **cite what checks it** — the
corpus file and case, or the command. Where nothing does, it is an opinion: say so plainly or cut it. The
zone's 162-and-0 is the number this lot moves.

---

### Task 1: The two heaviest internals

**Files:** `src/DataNet.Metrics/Internal/MultiClassRoc.cs` (4 doc / 7 inline),
`src/DataNet.Metrics/Internal/WeightedPercentile.cs` (3 doc / 7 inline)

Twenty-one blocks, the densest pair in the zone. `WeightedPercentile` is also where #92's epsilon
divergence lives, so its prose carries measured reasoning that must survive relocation.

- [ ] **Step 1: Read what the guard says about these two files**

```bash
python3 tools/check_comment_length.py | grep -E 'MultiClassRoc|WeightedPercentile'
```

- [ ] **Step 2: Triage each block, writing the outcome down before editing**

For each, in your report: file, line, current prose count, chosen outcome, and one line of why. Doing this
before editing is what stops the sweep becoming "shorten until the guard is quiet".

- [ ] **Step 3: Apply, one file at a time**

Cut, trim, relocate or mark. Where you relocate, write the ADR in the same commit as the citation that
replaces it — an ADR referenced by nothing is worse than the comment it replaced.

- [ ] **Step 4: Cite the claims**

```bash
grep -nE "scikit-learn|sklearn|numpy" src/DataNet.Metrics/Internal/MultiClassRoc.cs src/DataNet.Metrics/Internal/WeightedPercentile.cs
```

For each, find the corpus case that checks it under `tests/oracles/` and cite it by file and case, or run
the check once and cite the command. Where nothing checks it, say so in the comment.

- [ ] **Step 5: Gate and commit**

```bash
../data.net/.dotnet-guarded dotnet build DataNet.slnx -c Release
../data.net/.dotnet-guarded dotnet test DataNet.slnx -c Release
python3 tools/check_comment_length.py | grep -E 'MultiClassRoc|WeightedPercentile'   # empty
```

The suite must be green and **unchanged in count** — this task edits no code.

---

### Task 2: The regression metrics

**Files:** `R2.cs` (2/4), `ExplainedVariance.cs` (1/4), and the remaining regression metrics the guard
names — `MeanSquaredLogError.cs`, `RootMeanSquaredLogError.cs`, `PinballLoss.cs`,
`MeanAbsolutePercentageError.cs`, `RootMeanSquaredError.cs`, `MedianAbsoluteError.cs`

Get the exact list with:

```bash
python3 tools/check_comment_length.py | grep '^src/DataNet.Metrics/' | grep -vE 'Internal/|Accuracy|Confusion|Cohen|Balanced|Classification|RocAuc|Averaging|Prf'
```

These share one shape: `R2.cs:8`'s 18-line `<remarks>` explains that two knobs answer two different
undefined cases, and the same argument recurs across the family. **If the same explanation appears in three
files, it is an ADR** — write it once and cite it three times, which is the outcome this task most likely
wants.

Steps 1 to 5 as in Task 1: read the guard's list, triage in writing, apply, cite the claims, gate.

---

### Task 3: The classification metrics

**Files:** `ConfusionMatrix.cs` (2/3), `BalancedAccuracy.cs` (1/3), `CohenKappa.cs` (1/3),
`Internal/Prf.cs` (1/3), `Accuracy.cs`, `ClassificationReport.cs`, `Averaging.cs`, `RocAuc.cs`,
`Internal/BinaryRoc.cs`

Exact list:

```bash
python3 tools/check_comment_length.py | grep -E 'ConfusionMatrix|BalancedAccuracy|CohenKappa|Prf|Accuracy|ClassificationReport|Averaging|RocAuc|BinaryRoc'
```

**`BalancedAccuracy.cs:11` is the zone's showcase**: 34 lines of three `<para>` blocks, each a measured
scikit-learn behaviour with a citation. It is the clearest ADR candidate in the package — and the clearest
thing not to delete.

Steps as in Task 1.

---

### Task 4: The remainder, and the zone's gate

**Files:** everything the guard still names under `src/DataNet.Metrics/` — `Internal/Outputs.cs` (3/0),
`Internal/CompensatedSum.cs` (2/0), `Internal/Inputs.cs` (2/0), and whatever Tasks 1 to 3 left.

```bash
python3 tools/check_comment_length.py | grep '^src/DataNet.Metrics/'
```

- [ ] **Step 1: Clear the remainder**, by the same triage.

- [ ] **Step 2: The zone's own gate**

```bash
python3 tools/check_comment_length.py | grep '^src/DataNet.Metrics/'   # no output at all
grep -rnE "scikit-learn|sklearn|numpy" src/DataNet.Metrics --include=*.cs | grep -vcE "measured|\.json|oracle|corpus|ADR |docs/decisions|#[0-9]"
```

The first must be empty. The second is the count of reference-naming lines still citing nothing; it started
at 162 and **the report must state where it ended**, with the reason for any that remain.

- [ ] **Step 3: The branch's single lint pass**

```bash
../data.net/.dotnet-guarded dotnet build DataNet.slnx -c Release
../data.net/.dotnet-guarded dotnet test DataNet.slnx -c Release
../data.net/.dotnet-guarded dotnet format DataNet.slnx --verify-no-changes
npx markdownlint-cli2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
python3 tools/check_version_floor.py && python3 tools/check_machine_paths.py
.venv-oracles/bin/python -m pytest tools/tests -q
```

`dotnet format` is 1 min 37 warm against over 4 min 40 cold — build first, and give it a generous timeout.

- [ ] **Step 4: Update the CHANGELOG**

Under `## [Unreleased]` → `### DataNet.Metrics`, in `#### Changed`. Say what moved and where, not that
comments were tidied: a caller reading the changelog cares that reasoning is now in `docs/decisions/` and
findable, not that a line count fell.

---

## Self-review

**Spec coverage.** #134's triage has three tiers for claims and this plan carries all three, plus the
fourth outcome for blocks (the marker) that the spec's *Design* defines. The spec's warning — relocate,
never delete — is a global constraint here rather than a footnote, because 861 lines of measured argument
is exactly what a hurried sweep destroys.

**Placeholders.** Tasks 2 and 3 name their files by a command rather than a fixed list, deliberately: the
guard's output is the authority and a hand-copied list goes stale the moment Task 1 commits. Every task
carries the command that produces its own scope.

**Type consistency.** No types. The one name that must match across tasks is the guard's invocation, which
is `python3 tools/check_comment_length.py` throughout, filtered per task.

**What a reviewer should push on.** Task 4's second gate counts reference-naming lines that cite nothing
and asks for the ending number — but it does not require zero. Some of the 162 will be claims nothing
reasonably checks, and forcing a citation there would produce fake ones. The honest bar is that each
remaining line is deliberate and the report says why; a reviewer should test that the report actually
does, rather than accepting a number.
