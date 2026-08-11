# #9 CONTRIBUTING guide — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Write down the conventions the repository already follows — and only those — so a contributor does not have to infer them from diffs, with every quoted job name, link and property verified against the tree rather than recalled.

**Architecture:** One new `CONTRIBUTING.md`, a pointer from `README.md`, and the file added to the markdownlint glob in `ci.yml` so it is inside the gate it documents.

**Tech Stack:** Markdown, GitHub Actions, markdownlint-cli2.

**Spec:** `2026-08-04_0009_add-a-contributing-guide.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `docs/9-contributing-guide`. Never commit to `main`.
- **Documentation only.** No source, test, workflow-logic or configuration change
  beyond adding the file to the lint glob.
- **No forward references.** #1 (netstandard2.0), #8 (changelog) and #10
  (benchmark suite) are open. Nothing in this file may describe them.
- Every factual claim is checked against the tree in Task 4 before the PR. This
  document will be quoted back at people; a wrong job name in it is a wrong
  required check later.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

mdl() { npx --yes markdownlint-cli2 "**/*.md" "#node_modules"; }
```

---

### Task 1: Collect the conventions from the evidence

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the content, sourced rather than remembered.

- [x] **Step 1: Branch naming, from what has actually been used**

```bash
git log --all --format="%D" | grep -oE "(feat|fix|perf|docs|chore|refactor|test|ci|release)/[a-z0-9.-]+" | sort -u
```

Document the prefixes in use. Do not invent a taxonomy the history does not
support.

- [x] **Step 2: The CI job names, exactly**

```bash
grep -n "name:" .github/workflows/ci.yml
```

Required status checks are configured **by name**. A paraphrase here becomes a
required check that never matches.

- [x] **Step 3: Confirm warnings-as-errors is where you think it is**

```bash
grep -rn "TreatWarningsAsErrors" --include='*.props' --include='*.csproj' .
```

- [x] **Step 4: Collect the existing suppressions and their shape**

```bash
grep -rn "pragma warning disable" src --include='*.cs' | head -20
```

The guide describes the convention these follow; read them first.

---

### Task 2: Write the guide

**Files:**

- Create: `CONTRIBUTING.md`

**Depends on:** Task 1.

- [x] **Step 1: Branching model**

GitHub flow, `main` always releasable, `<type>/<short-kebab-summary>` optionally
prefixed with the issue number, one concern per branch, `Closes #n` in the pull
request. Include the four-command example — a contributor copies that, not the
prose around it.

- [x] **Step 2: Review with a single maintainer**

The part that needs the reasoning, not just the rule: **GitHub does not let anyone
approve their own pull request**, so requiring an approving review would block
every pull request with nobody able to unblock it. Protection rests on required
status checks instead, and "require approvals" stays off until a second maintainer
joins.

Say plainly that self-merging after green checks is the expected flow here, not a
shortcut. Otherwise the next person to read the settings will "fix" them.

- [x] **Step 3: Definition of done, as runnable checks**

Build clean under repository-wide warnings-as-errors; tests pass; a new algorithm
replays an oracle corpus; `dotnet format` and markdownlint clean; public API
carries XML documentation naming the Python function it matches.

- [x] **Step 4: Oracle validation, with the neutral-directory note**

The procedure — generator section, regenerate, commit the JSON, replay with a
`1e-9` tolerance — and the trap, **with its exact error text**:

```text
ImportError: Blocked import of regex from current working directory for security reasons
```

Say which directory works and which do not: from `/tmp` with the virtualenv inside
the repository, yes; from the repository root or from `~`, no. A reader searching
for their error message must land here.

Add the determinism requirement: fixed seed, no wall-clock timestamps, no
unordered iteration.

- [x] **Step 5: Analyzer suppressions, with the reason they live in the source**

`#pragma warning disable` with a written justification. Then **why**: SonarLint
ignores `.editorconfig` entirely, and `sonarlint.rules` is application-scope so VS
Code silently drops it from a workspace file. Without that paragraph the rule
reads as arbitrary and someone will helpfully move the suppressions to a config
file that has no effect.

State the bar: a justification a reviewer can disagree with. "Too noisy" is not
one.

- [x] **Step 6: Licensing and provenance**

Never transcribe GPL-licensed code — implement from the published algorithm
description, which is *why* the stemmers and phonetic encoders are original
implementations. Never commit model weights. Point at ADR 0003 rather than
restating it.

---

### Task 3: Wire it in

**Files:**

- Modify: `.github/workflows/ci.yml`
- Modify: `README.md`

**Depends on:** Task 2.

- [x] **Step 1: Add `CONTRIBUTING.md` to the markdownlint glob**

The file is about to be the most-edited document here. Adjacent to the gate is not
inside it.

- [x] **Step 2: Link it from `README.md`**

One sentence, where a contributor looks: the conventions, the definition of done,
the oracle procedure and the suppression policy.

---

### Task 4: Verify every claim, then gate

**Depends on:** Task 3.

This task is the reason the guide is trustworthy. Do not shorten it.

- [x] **Step 1: Every link target resolves**

```bash
grep -oE "\]\(([^)]+)\)" CONTRIBUTING.md | sed -E 's/\]\((.*)\)/\1/' | grep -v "^http" | while read -r p; do
  [ -e "${p%%#*}" ] || echo "BROKEN: $p"
done
```

Expected: no output.

- [x] **Step 2: Every quoted CI job name exists verbatim**

```bash
grep -oE '`[A-Z][^`]+`' CONTRIBUTING.md | tr -d '`' | while read -r n; do
  grep -qF "$n" .github/workflows/ci.yml && echo "OK: $n"
done
```

Check by eye that each job name quoted appears. This is the claim most likely to
be subtly wrong and most expensive when it is.

- [x] **Step 3: No forward reference slipped in**

```bash
grep -niE "netstandard|changelog|benchmark" CONTRIBUTING.md
```

Expected: nothing describing #1, #8 or #10 as existing. If a hit is a legitimate
mention of something already in the tree, keep it; otherwise remove it.

- [x] **Step 4: Lint**

```bash
mdl
```

Expected: 0 issues across 25 files — the new file included, which is what proves
Task 3 Step 1 took effect.

- [x] **Step 5: Commit**

```bash
git add CONTRIBUTING.md README.md .github/workflows/ci.yml
git commit -m "Add a CONTRIBUTING guide"
```
