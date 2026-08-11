# #73 Compile the guides' C# — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A renamed method fails CI on the pages that document it — every ` ```csharp ` fence in `README.md` and `docs/guides/` compiled against the packed packages, with the Markdown remaining the single copy.

**Architecture:** `tools/extract_doc_snippets.py` reads the fences and emits one method per fence into a git-ignored `Generated/` tree, rebuilt every run. `samples/DataNet.DocSnippets` compiles them against `./artifacts`, with `SnippetContext.cs` supplying the symbols the prose introduces without showing.

**Tech Stack:** Python 3, .NET console project, NuGet local feed, GitHub Actions.

**Spec:** `2026-08-06_0073_nothing-compiles-the-c-sharp-snippets-in-docs-guides.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `docs/73-compile-guide-snippets`. Never commit to `main`.
- **The Markdown is the single copy.** No second copy of any snippet, anywhere.
- **Compile against the packed packages**, never the projects. A snippet that only
  compiles through a `ProjectReference` is not one a reader can run.
- `Generated/` is git-ignored and rebuilt on every run. Never hand-edited.
- **Fix the guides where they do not compile.** Do not weaken the extractor to
  accept broken prose.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net
SCRATCH=/tmp/snip73

pack_feed() {
  rm -rf ./artifacts "$SCRATCH/pack"
  NUGET_PACKAGES="$SCRATCH/pack" bash -c '
    for p in src/DataNet.Text src/DataNet.Embeddings src/DataNet.Fuzzy; do
      dotnet pack "$p" -c Release -o ./artifacts || exit 1
    done'
}

build_snippets() {
  python3 tools/extract_doc_snippets.py || return 1
  rm -rf "$SCRATCH/snip"
  NUGET_PACKAGES="$SCRATCH/snip" dotnet build samples/DataNet.DocSnippets -c Release
}
```

---

### Task 1: Settle the shape, and record why the other two lose

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the argument, because the issue left the form open and the wrong
choice is plausible.

- [x] **Step 1: Count what is at stake**

```bash
grep -rc '```csharp' README.md docs/guides/*.md
```

- [x] **Step 2: Write down why a `docs-samples` project loses**

It holds a **second copy** of each snippet. Nothing forces the two to agree, so it
converts "documentation that lies" into "documentation that lies while a project
compiles nearby".

- [x] **Step 3: Why marker-based inclusion loses**

It needs a sync tool **and** a drift check, and moves the text a reader edits out
of the file they are reading.

- [x] **Step 4: Extraction, chosen because drift becomes impossible**

Not merely detected. And it adds no syntax to files that are plain Markdown today.

---

### Task 2: The extractor

**Files:**

- Create: `tools/extract_doc_snippets.py`
- Modify: `.gitignore`

**Depends on:** Task 1.

- [x] **Step 1: Read every ` ```csharp ` fence in `README.md` and `docs/guides/`**

- [x] **Step 2: One method per fence**

This is what lets `vectorization.md` declare `cv` twice on the same page without
colliding. A guide is prose; re-introducing a variable further down is normal.

- [x] **Step 3: Hoist `using` lines to the compilation unit**

So a fence inherits what a reader would already have in scope from earlier on the
page.

- [x] **Step 4: Support an opt-out with a reason**

`<!-- docs-compile: skip - reason -->` on the line above the fence. The reason has
to be one a reviewer can disagree with — same bar as an analyzer suppression.

- [x] **Step 5: `Generated/` is git-ignored**

Rebuilt on every run; never hand-edited. A generated tree in git is a second copy
by another name.

---

### Task 3: The project that compiles them

**Files:**

- Create: `samples/DataNet.DocSnippets/DataNet.DocSnippets.csproj`
- Create: `samples/DataNet.DocSnippets/SnippetContext.cs`

**Depends on:** Task 2.

- [x] **Step 1: Reference the packed packages, through `samples/NuGet.config`**

- [x] **Step 2: `SnippetContext.cs` for what the prose introduces without showing**

Guides legitimately say "given a corpus…" with no declaration. Write that context
once, by hand, rather than forcing every page into a compilable program.

- [x] **Step 3: Build**

```bash
pack_feed && build_snippets
```

- [x] **Step 4: Fix the guides where they fail — do not weaken the extractor**

Expected: several fences will not compile. Each is a real defect in the
documentation, which is the whole point of the exercise.

---

### Task 4: Prove it catches what it is for

**Depends on:** Task 3.

- [x] **Step 1: Rename a public method and confirm the snippet build fails**

```bash
# Temporarily rename a method used in docs/guides/quickstart.md, then:
pack_feed && build_snippets; echo "exit: $?"
```

Expected: non-zero, naming the generated file and — through it — the page. Revert.

A gate never seen to fail is not known to work.

- [x] **Step 2: Confirm the opt-out works and is visible**

```bash
grep -rn "docs-compile: skip" README.md docs/guides/
```

Each with a reason.

---

### Task 5: CI and the definition of done

**Files:**

- Modify: `.github/workflows/ci.yml`, `CONTRIBUTING.md`
- Modify: `docs/guides/quickstart.md`, `vectorization.md`, `embeddings.md`

**Depends on:** Task 4.

- [x] **Step 1: A `Guide snippets compile` job — pack, extract, build**

Same `NUGET_PACKAGES` separation as the sample job, and for the same reason
(ADR 0009): otherwise it judges the published packages.

- [x] **Step 2: Add it to `CONTRIBUTING.md`'s definition of done**

A gate not listed where contributors read is a gate they discover by failing.

- [x] **Step 3: Answer the SonarQube findings on the extractor**

A new Python file will raise some. Fix or suppress with a reason, before the pull
request rather than after — a green build is not a clean Sonar.

- [x] **Step 4: Full gate**

```bash
dotnet build DataNet.slnx -c Release && dotnet test DataNet.slnx -c Release 2>&1 | tail -3
pack_feed && build_snippets
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
```

- [x] **Step 5: Commit**

```bash
git commit -m "Compile the guides' C#, from the guides themselves"
git commit -m "Take the four SonarQube findings on the extractor"
```
