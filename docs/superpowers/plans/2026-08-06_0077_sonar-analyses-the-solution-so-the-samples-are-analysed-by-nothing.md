# #77 Analyse the samples — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Bring `samples/` into SonarCloud's view — the one area the packaging gate forces new code into and no analyser reads — **without** putting it in the solution, which would destroy what the samples exist to prove.

**Architecture:** The sample builds are added to the existing `begin` → `build` → `end` window, after a pack. The change is demonstrated by deliberately introducing findings on the branch, because a build step that analyses nothing looks exactly like one that finds nothing.

**Tech Stack:** GitHub Actions, `dotnet-sonarscanner`, NuGet local feed.

**Spec:** `2026-08-06_0077_sonar-analyses-the-solution-so-the-samples-are-analysed-by-nothing.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `chore/77-sonar-analyses-the-samples`. Never commit to `main`.
- **Neither sample joins `DataNet.slnx`.** Inside the solution, `ProjectReference`
  resolution satisfies the package references and the packaging gate stops proving
  anything (ADR 0009). This is the constraint the whole change is balanced on.
- The sample builds need a `pack` first, and an isolated `NUGET_PACKAGES`.
- **Prove by mutation.** Observing no new findings is not evidence.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

parse() { python3 -c "import yaml; yaml.safe_load(open('.github/workflows/sonarcloud.yml'))" && echo OK; }
in_solution() { grep -c "samples" DataNet.slnx || echo "0 — correct"; }
```

---

### Task 1: Establish that the samples are invisible

**Files:** none modified.

**Depends on:** nothing.

- [x] **Step 1: Confirm the scanner's window is the solution**

```bash
grep -n -A6 "begin\|dotnet build" .github/workflows/sonarcloud.yml
```

The scanner reads whatever MSBuild compiles between `begin` and `end`. That is
`DataNet.slnx`, and both samples are outside it.

- [x] **Step 2: Confirm no finding has ever been reported there**

Check the dashboard by file path. Expected: nothing under `samples/`.

- [x] **Step 3: Note why this compounds**

Issue #72's packaging gate requires every new exported type to be reachable from
`samples/DataNet.Sample`. **New code lands there on every feature branch** — in
the one area no analyser reads.

---

### Task 2: Add the sample builds to the scanner window

**Files:**

- Modify: `.github/workflows/sonarcloud.yml`

**Depends on:** Task 1.

- [x] **Step 1: Pack, then build both samples, between `begin` and `end`**

With an isolated `NUGET_PACKAGES`, or the build judges the published packages
rather than the working tree (ADR 0009).

- [x] **Step 2: Do not add them to the solution**

```bash
in_solution
```

Expected: `0`. This is the line that must not move.

- [x] **Step 3: Record the exclusions and their reasons in the workflow**

`DocSnippets/Generated/` is regenerated from the Markdown on every run and is
already excluded. Any further exclusion is written where it takes effect, with
why.

- [x] **Step 4: Parse check**

```bash
parse
```

---

### Task 3: Prove it by mutation, on the branch

**Files:** temporary edits, reverted.

**Depends on:** Task 2.
**Produces:** the difference between a build step and a gate.

- [x] **Step 1: Introduce a finding in each sample project**

One in `samples/DataNet.Sample/*.cs`, one in
`samples/DataNet.DocSnippets/SnippetContext.cs`. Commented-out code (`S125`) is a
reliable choice.

- [x] **Step 2: Push, and read SonarCloud**

Expected: both appear, attributed to the right files.

**A build step that analyses nothing looks exactly like one that finds nothing.**
This step is what distinguishes them.

- [x] **Step 3: Revert, and confirm they disappear**

- [x] **Step 4: Confirm the packaging gate still works**

```bash
# Remove a member call from a Lot*.cs, pack, run the sample.
```

Expected: still fails on an unreachable public type. The gate and the analysis
must both hold, and this change is only acceptable if neither weakened the other.

---

### Task 4: Triage the first pass, with a count

**Depends on:** Task 3.

- [x] **Step 1: Read the first analysis of `samples/`**

This area has never been analysed, so whatever appears is a backlog rather than a
regression.

- [x] **Step 2: Fix or triage each, and report the number**

A count is what lets a reader judge whether the area was in reasonable shape. "Some
findings were addressed" is not reviewable.

- [x] **Step 3: Full gate**

```bash
dotnet build DataNet.slnx -c Release && dotnet test DataNet.slnx -c Release 2>&1 | tail -3
in_solution
parse
```

- [x] **Step 4: Commit**

```bash
git add .github/workflows/sonarcloud.yml
git commit -m "Analyse the samples, which the solution build never reached"
```
