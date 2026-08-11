# #6 Warnings-as-errors repository-wide, plus a CI lint job — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** One declaration of `TreatWarningsAsErrors` covering every project in the repository, and a `lint` CI job running `dotnet format` and markdownlint — both green on the tree they land in.

**Architecture:** The property moves from three `.csproj` files to the root `Directory.Build.props`, so `src`, `tests`, `bench` and anything added later inherit it. A `lint` job is added to `ci.yml` with a `.markdownlint.json` disabling MD013 and scoping MD024 to siblings. Both checks fail against the current tree; fixing what they find is part of this change.

**Tech Stack:** MSBuild `Directory.Build.props`, GitHub Actions, `dotnet format`, markdownlint-cli2.

**Spec:** `2026-08-04_0006_enforce-warnings-as-errors-repository-wide-add-a-ci-lint-job.md` (in `../specs/`).

## Global Constraints

- **Everything in English** — commit messages, PR body.
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `chore/6-warnings-as-errors-and-lint`. Never commit to `main`.
- **The gate lands green.** If either check is red at the end, the change is not
  done — do not disable the rule that found it without a reason written in
  `.markdownlint.json`.
- **No behavioural change to any library.** The only `src/` edits permitted are
  whitespace fixes `dotnet format` produces. If a warning-turned-error demands a
  code change, stop and report it — that is a separate concern.
- Stay out of #7's lane: no `#pragma warning disable S…` in this diff.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

build_all() { dotnet build -c Release; }
test_all()  { dotnet test -c Release; }
fmt()       { dotnet format --verify-no-changes; }
mdl()       { npx --yes markdownlint-cli2 "**/*.md" "#node_modules"; }
```

---

### Task 1: Move the property, and prove it is a move

**Files:**

- Modify: `Directory.Build.props`
- Modify: `src/DataNet.Text/DataNet.Text.csproj`
- Modify: `src/DataNet.Embeddings/DataNet.Embeddings.csproj`
- Modify: `src/DataNet.Fuzzy/DataNet.Fuzzy.csproj`

**Depends on:** nothing.
**Produces:** one declaration covering every project.

- [x] **Step 1: Find every current declaration**

```bash
grep -rn "TreatWarningsAsErrors" --include='*.csproj' --include='*.props' .
```

Expected: three hits, all under `src/`.

- [x] **Step 2: Add it to the root `Directory.Build.props`**

```xml
<!-- Warnings are errors everywhere: src, tests and bench alike. -->
<TreatWarningsAsErrors>true</TreatWarningsAsErrors>
```

- [x] **Step 3: Remove the three per-project declarations**

This is what makes it a move. Leaving them is not harmless: it creates three
places that could later disagree with the root, and the disagreement would be
invisible.

- [x] **Step 4: Confirm exactly one declaration remains**

```bash
grep -rn "TreatWarningsAsErrors" --include='*.csproj' --include='*.props' . | wc -l
```

Expected: `1`.

- [x] **Step 5: Build everything, including the projects that were never covered**

```bash
build_all
```

Expected: 0 warnings, 0 errors. If `tests/` or `bench/` now fails, read each
finding: a whitespace or unused-using warning is in scope; anything demanding a
behavioural change is not, and stops the task.

---

### Task 2: Measure what the two new checks find

**Files:** none modified.

**Depends on:** Task 1.
**Produces:** the size of Task 4, known before it starts.

- [x] **Step 1: `dotnet format`**

```bash
dotnet format --verify-no-changes 2>&1 | tail -20
```

Record the count and the files. Expect it to be concentrated rather than spread —
formatting drift usually comes from one or two files edited outside an IDE.

- [x] **Step 2: markdownlint, before any configuration**

```bash
npx --yes markdownlint-cli2 "**/*.md" "#node_modules" 2>&1 | tail -30
npx --yes markdownlint-cli2 "**/*.md" "#node_modules" 2>&1 | grep -oE "MD[0-9]+" | sort | uniq -c | sort -rn
```

Record the per-rule breakdown. It is the evidence for Task 3's decisions: a rule
disabled without knowing how often it fires is a rule disabled on a hunch.

---

### Task 3: `.markdownlint.json`, two rules and two reasons

**Files:**

- Create: `.markdownlint.json`

**Depends on:** Task 2.

- [x] **Step 1: Write the configuration**

```json
{
  "MD013": false,
  "MD024": { "siblings_only": true }
}
```

- [x] **Step 2: Justify both against Task 2's breakdown**

- **MD013** — the tree already hard-wraps prose consistently; the rule would
  re-litigate a decision already made everywhere.
- **MD024** — the ADRs and the changelog-to-be legitimately repeat "Context" and
  version headings across sections; `siblings_only` keeps the useful half of the
  rule.

Disable nothing else. Every remaining finding gets fixed in Task 4.

---

### Task 4: Make both checks pass

**Files:**

- Modify: every `.md` markdownlint flags (`README.md`, `docs/equivalence.md`,
  `docs/guides/*.md`, `docs/migration/*.md`)
- Modify: `src/DataNet.Text/Stemming/EnglishSnowballStemmer.cs`

**Depends on:** Task 3.

- [x] **Step 1: Apply the mechanical markdown fixes**

```bash
npx --yes markdownlint-cli2 --fix "**/*.md" "#node_modules"
git diff --stat
```

The bulk is table-pipe spacing and underscore emphasis. Read the diff anyway —
`--fix` is reliable, but this is documentation and a reviewer will assume it was
read.

- [x] **Step 2: Fix by hand what `--fix` cannot**

The unlabelled code fence in `README.md` needs a language. It holds the repository
tree, so `text`.

- [x] **Step 3: Fix the content error found on the way past**

That same fence names `DataNet.sln`; the solution file is `DataNet.slnx`.

```bash
ls *.slnx *.sln 2>/dev/null
```

Correct it, and put it in the PR description. A content fix inside a formatting
sweep is exactly the kind of change that should be called out rather than left for
a reviewer to spot in 150 lines of pipe alignment.

- [x] **Step 4: `dotnet format`**

```bash
dotnet format
git diff --stat
```

- [x] **Step 5: Both clean**

```bash
fmt && mdl
```

Expected: no output from either.

---

### Task 5: The CI job

**Files:**

- Modify: `.github/workflows/ci.yml`

**Depends on:** Task 4.

- [x] **Step 1: Add the `lint` job**

Running both checks. Same Markdown glob used locally in Task 4, so what passes on
a laptop passes in CI — a glob that differs between the two is a gate that fails
only on other people's machines.

- [x] **Step 2: Confirm the job name matches anything that references it**

```bash
grep -n "name:" .github/workflows/ci.yml
grep -rn "lint" CONTRIBUTING.md README.md 2>/dev/null
```

Required status checks are configured by name; a job renamed later without
updating what quotes it fails open.

---

### Task 6: Full gate

**Depends on:** Task 5.

- [x] **Step 1: Everything, from clean**

```bash
dotnet clean -c Release && build_all && test_all 2>&1 | tail -3
fmt && mdl
```

Expected: 0 warnings, 0 errors; 158/158; format clean; markdownlint 0 issues
across 24 files.

- [x] **Step 2: Confirm no library behaviour moved**

```bash
git diff main --stat -- src | tail -3
git diff main -- src | grep -E "^[+-]" | grep -vE "^[+-]{3}" | grep -vE "^\s*[+-]\s*$" | head
```

The only `src/` changes should be whitespace and the three removed
`TreatWarningsAsErrors` lines. Anything else means the task drifted.

- [x] **Step 3: Commit**

```bash
git add -A
git commit -m "Enforce warnings-as-errors repository-wide, add a CI lint job"
```
