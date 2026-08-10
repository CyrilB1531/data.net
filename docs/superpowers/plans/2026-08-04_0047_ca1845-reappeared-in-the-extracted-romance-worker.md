# #47 `CA1845` in the extracted Romance worker — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Clear the `CA1845` failing the quality gate on `main`, with the same justification the four language stemmers already carry — and record the general failure so the next extraction does not repeat it.

**Architecture:** One suppression in one file. The value of the branch is in the reasoning being copied verbatim rather than re-invented, and in the lesson being written where it will be read.

**Tech Stack:** Roslyn analyzers, C# (net10.0 + netstandard2.0).

**Spec:** `2026-08-04_0047_ca1845-reappeared-in-the-extracted-romance-worker.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `fix/romance-base-ca1845`. Never commit to `main`.
- **Do not "fix" the finding.** The span-based overload does not exist on
  `netstandard2.0`; applying the rule breaks that target.
- **No behaviour change.** Corpora untouched.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

build_all() { dotnet build -c Release; }
test_all()  { dotnet test -c Release; }
```

---

### Task 1: Confirm it is the suppression that was left behind, not a new defect

**Files:** none modified.

**Depends on:** nothing.

- [ ] **Step 1: Find the finding's exact site**

```bash
grep -n "Substring" src/DataNet.Text/Stemming/RomanceSnowballWorker.cs
```

Expected: `Replace`, and `Delete` beside it — the two members #45 moved.

- [ ] **Step 2: Find where the suppression still sits**

```bash
grep -n -B3 "CA1845" src/DataNet.Text/Stemming/*.cs
```

Expected: the `#pragma` still in the four language files, and **absent** from the
new one. The code moved; the suppression did not.

- [ ] **Step 3: Note that the build never complained**

```bash
build_all 2>&1 | grep -c "CA1845"
```

Expected: `0`. Nothing in the build runs the analyzer at this point, so only the
dashboard caught it. That is the reason this class of slip is invisible until
after merge.

---

### Task 2: Suppress, with the wording copied

**Files:**

- Modify: `src/DataNet.Text/Stemming/RomanceSnowballWorker.cs`

**Depends on:** Task 1.

- [ ] **Step 1: Copy the justification from a language stemmer verbatim**

```bash
grep -n -B4 -A1 "CA1845" src/DataNet.Text/Stemming/SpanishSnowballStemmer.cs
```

The span-based `string.Concat` overload is net-only; the `Substring` form is what
makes the file compile for `netstandard2.0`.

**Copy it.** A paraphrase invites a reader to wonder whether the five reasons
differ.

- [ ] **Step 2: Scope it to the two members, not the file**

A file-wide suppression would also hide a future, genuine `CA1845` elsewhere in
the worker.

- [ ] **Step 3: Build and test**

```bash
build_all && test_all 2>&1 | tail -3
dotnet format --verify-no-changes
```

Expected: both frameworks 0/0, 164/164, format clean.

---

### Task 3: Write the lesson where it will be read

**Files:**

- Modify: `CONTRIBUTING.md`

**Depends on:** Task 2.

- [ ] **Step 1: The general rule**

**When code carrying a justified suppression moves to a new file, the suppression
does not move with it.** Nothing enforces this. It has now happened once, on the
first extraction this repository performed.

A commit message is not where the next person doing an extraction looks.

- [ ] **Step 2: Note that the extraction itself worked**

Duplication on `main` went 5.9 % → 4.1 %. This is the tail of a change that did
what it was meant to, not evidence against it — say so in the pull request, or a
reader will draw the wrong conclusion from a fix that immediately follows a
refactor.

- [ ] **Step 3: Confirm the gate on `main` after merge**

```bash
# Read the dashboard, not the build.
```

The quality gate is the only thing that reported this and the only thing that can
confirm it is gone.

- [ ] **Step 4: Commit**

```bash
git add src/DataNet.Text/Stemming/RomanceSnowballWorker.cs CONTRIBUTING.md
git commit -m "Suppress CA1845 in the shared Romance worker"
```
