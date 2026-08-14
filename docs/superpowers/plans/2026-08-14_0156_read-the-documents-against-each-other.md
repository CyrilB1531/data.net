# #156 — Reading the prose documents against each other Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for
> tracking.

**Goal:** No contradiction, no paraphrase, nothing filed in the wrong document, every pointer resolving —
and documents a reader can actually read — across the 56 tracked Markdown files outside `docs/superpowers/`.

**Architecture:** Four defect classes, ordered by causality rather than visibility: misplacement causes
paraphrase, paraphrase becomes contradiction, and staleness hides in all three. The tasks follow that order,
so the later readings work on a corpus whose sections already sit where they belong.

**Tech Stack:** Markdown, `markdownlint-cli2`, a throwaway candidate finder in the scratch directory.

**Spec:** `docs/superpowers/specs/2026-08-14_0156_read-the-documents-against-each-other.md`

## Global Constraints

- Branch `docs/156-read-the-documents-against-each-other`, based on `main` at `6ed56de`. Do not push, do not
  open a pull request without asking.
- **Prose only.** No source file, no test, no corpus. `git status --porcelain src/ tests/ bench/*.cs` stays
  empty, and the test suite is not re-run per task — there is nothing for it to measure. The final task runs
  it once as a backstop.
- **Never rewrite an ADR's body.** An ADR is a dated record. Where one is contradicted by a later decision it
  gains a `> **#NNN update:**` block naming what went stale, the convention ADR 0022 already uses. The
  original paragraph stays standing.
- **Duplicate verbatim, or point. Never paraphrase.** A verbatim copy diffs; two wordings of one rule drift.
- **Take no ADR number and write no ADR.**
- Every ` ```csharp ` fence in `README.md` and `docs/guides/` is extracted and compiled — run
  `python3 tools/extract_doc_snippets.py` after touching either.
- `npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"`
  after every task. Note the glob **does not cover** `.github/instructions/` or `THIRD-PARTY-NOTICES.md`;
  both are in this lot's reading scope and neither is linted, which is #155's subject, not this one's.
- English everywhere. Commit messages carry no `feat:`/`fix:` prefix and no process prefix.

## The corpus, and what the finder already said

56 tracked Markdown files outside `docs/superpowers/`, 8 704 lines, 6 486 of prose. Two verbatim pairings and
nothing else:

- the four-line `python`/`python3` passage in `CONTRIBUTING.md` and `CLAUDE.md` — **stays as it is**, D1;
- **31 identical table rows** in `bench/README.md` and `docs/guides/performance.md` — Task 1's subject.

The finder lives in the scratch directory and is re-run in the final task, not trusted from the start: six
pull requests merged into these files in the last day.

---

### Task 1: Put the misplaced sections where they belong

**Files:** `bench/README.md`, `docs/guides/performance.md`, `README.md`, `CLAUDE.md`, and whatever else the
reading finds.

**Depends on:** nothing. First because misplacement is what produces the paraphrases the later tasks would
otherwise reconcile one by one.

- [ ] **Step 1: The benchmark numbers leave `bench/README.md`**

The 31 shared rows are measurements. `bench/README.md`'s subject is **how to measure** — the harness, the
corpus, the commands; `docs/guides/performance.md`'s is **what was measured**. Move any row that is a
result, leave everything about running them, and put one link where the table was.

Check first whether the two copies still agree: if `bench/README.md` holds a number the guide does not, that
number is a finding — say what it is and where it came from rather than deleting it.

- [ ] **Step 2: Publish the map, verbatim, in `README.md` and `CLAUDE.md`**

Three columns — the document, where its content comes from, what it is for. The spec's D1b table is the
content; add the source column. It is the same table in both files, **word for word**: this lot's own rule
applied to its own output.

`docs/guides/performance.md`'s source is a benchmark run on a named machine. `docs/decisions/README.md`'s is
the ADRs' own `**Status:**` lines. `THIRD-PARTY-NOTICES.md`'s is the packages' licences. Those are what tell
a reader to correct the source rather than the document.

- [ ] **Step 3: Read every document for a section whose subject is another's**

Walk the corpus with the map in hand. A section belongs elsewhere when a reader looking for it would open
the other file first. Move it, leave a link, and **list every move in the report** — this is the task whose
diff a reviewer cannot verify without that list.

- [ ] **Step 4: Verify and commit**

```bash
cd <repo>
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
python3 tools/extract_doc_snippets.py
git add -A '*.md' && git commit -m "Move each section to the document whose subject it is"
```

---

### Task 2: `docs/decisions/README.md`, and the links that should point at it

**Files:** create `docs/decisions/README.md`; modify `README.md`, `CLAUDE.md`, `CONTRIBUTING.md`.

**Depends on:** Task 1 for the map.

- [ ] **Step 1: Build the index from the ADRs themselves**

Thirty-three files, each carrying `**Status:** … · **Date:** …` on its third line. The index lists number,
title, status, date, and the relationships: what each supersedes, updates, or is updated by.

Read them; do not hand-copy titles. Today only two relationships are stated — `0013` says "superseded in
part by 0014", and `0004` carries a sentence (`single-word and blocked shipped`) where a status belongs.
**Others exist in the bodies**: ADR 0022 carries a `> **#119 and #120 update:**` block, and ADR 0004 was
revised on 2026-08-05. Read for those rather than trusting the status lines.

- [ ] **Step 2: `0004`'s status becomes a status**

Its sentence moves into the body, where it can say what shipped and when. The status line reads what the
other thirty-two read, or a word that means something different and is defined in the index.

- [ ] **Step 3: Point the links at the index**

The root `README.md` **does not link to `docs/decisions/` at all** — add the link, to the index.
`CLAUDE.md` and `CONTRIBUTING.md` link to the bare directory in four places; those become links to the
index, which is what a reader wants when they follow "see the decisions".

Links to a *specific* ADR stay as they are — they are pointers to a document, not to a listing.

- [ ] **Step 4: Fix the count that is already wrong**

`CLAUDE.md:172` says "nineteen ADRs so far". There are **thirty-three**. Replace the count with the link
rather than a fresher number: a count in prose is a claim that goes stale every time a lot lands, and this
one already has.

- [ ] **Step 5: Verify and commit**

```bash
cd <repo>
ls docs/decisions/*.md | wc -l          # the index must list every one of them
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
git add -A '*.md' && git commit -m "Give the decisions an index, and point the links at it"
```

---

### Task 3: The paraphrases

**Files:** `CONTRIBUTING.md`, `CLAUDE.md`, `docs/decisions/0012`, `0015`, `0019`, and whatever the reading
adds.

**Depends on:** Tasks 1-2.

- [ ] **Step 1: The five the issue names, measured**

| instruction | files |
| --- | ---: |
| the `python`/`python3` split | 2 — **verbatim, leave it** |
| the oracle generator's neutral directory | 2 — paraphrased |
| `Blocked import of regex from current working directory` | 2 — paraphrased |
| `DataNetUseProjectRefs` | 3, including ADR 0012 |
| "warnings are errors" | 5, including ADRs 0015 and 0019 |

For each: one document states it, the others point. Which one states it comes from the map — a process rule
belongs to `CONTRIBUTING.md`, a session trap to `CLAUDE.md`, a decision to its ADR.

**An ADR is not edited to remove a paraphrase.** An ADR stating the rule it decided is the ADR doing its job;
what changes is the *other* document, which points at the ADR instead of restating it.

- [ ] **Step 2: Find the ones nobody has named**

The finder reports verbatim repetition only, and found two. Paraphrase has no mechanical signal: read for
it, concentrating where two documents have the same subject — `CONTRIBUTING.md` and `CLAUDE.md`, the guides
and `docs/equivalence.md`, `tools/README.md` and `CONTRIBUTING.md`'s tooling sections.

- [ ] **Step 3: Verify and commit**

```bash
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
git add -A '*.md' && git commit -m "State each rule once, and point at it from everywhere else"
```

---

### Task 4: The statements that were true

**Files:** the whole corpus.

**Depends on:** Tasks 1-3.

- [ ] **Step 1: Walk the recent lots, because they are the map of where staleness is**

Each of these changed behaviour or process, and each could have left a document behind:

| lot | what moved |
| --- | --- |
| #121, #149 | what `LoadBpe` accepts, and what `Decode` does with malformed bytes |
| #140 | the median's cost, and the `median_ae` rows in `performance.md` |
| #143, #145 | the BPE pre-tokenizer's `Split` behaviours |
| #150 | the comment rule every document now has to agree with |
| #151-#153 | prose moved *into* `docs/equivalence.md` and the guides |
| #160 | which occurrence of a repeated merge pair wins |

- [ ] **Step 2: Check every count in prose**

A count is a claim that goes stale on the next merge. `CLAUDE.md`'s "nineteen ADRs" was one and Task 2 fixes
it; find the others — counts of packages, of tests, of rules, of files — and either replace them with a
pointer or confirm them against the tree.

- [ ] **Step 3: Check every pointer resolves**

A named file exists, a named ADR says what it is cited for, a named command runs. #152 found a suppression
citing decision 0013 for a claim 0013 never makes, and #153 found a comment citing `performance.md` for a
number it does not contain — both in code, and the same class of defect lives here.

- [ ] **Step 4: Verify and commit**

```bash
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
python3 tools/extract_doc_snippets.py
git add -A '*.md' && git commit -m "Correct what was true when it was written"
```

---

### Task 5: Make them readable, without changing a claim

**Files:** the whole corpus.

**Depends on:** Tasks 1-4. Last, because a section moved in Task 1 or corrected in Task 4 would have to be
made readable twice.

- [ ] **Step 1: Apply the five criteria, one document at a time**

- a document **opens by saying what it is and who it is for**, in one or two sentences;
- a **heading answers a question a reader arrives with**, so they can skip to it;
- a **paragraph makes one point** — one carrying three becomes three;
- a **fact a reader must act on is not buried mid-paragraph**: it becomes a list item, a table row, or the
  first sentence;
- **no sentence needs re-reading to parse** — in practice, more than two subordinate clauses, or more than
  one aside between dashes.

This repository's prose fails the last two most often: the house style packs a measurement, its caveat and
its consequence into one sentence with two dashes. Splitting that into two sentences changes nothing and
costs the reader nothing to check.

- [ ] **Step 2: Never restate a claim**

A readability edit **splits, promotes, re-orders, or deletes a repetition**. It does not rephrase a fact —
rephrasing is how a style pass silently changes meaning, and it is exactly the paraphrase D1 forbids.

If a sentence cannot be made readable without restating it, leave it and say so in the report. That is a
finding about the sentence, not a licence.

- [ ] **Step 3: Commit these edits on their own**

```bash
git add -A '*.md' && git commit -m "Split what needed two sentences, and lift what a reader has to act on"
```

**One commit for readability, separate from every other task's.** A reviewer reads this diff knowing no
fact moved in it; mixing it with Task 4's corrections would destroy that guarantee.

- [ ] **Step 4: Verify**

```bash
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
python3 tools/extract_doc_snippets.py
```

---

### Task 6: The changelog becomes one sentence and two links

**Files:** `CHANGELOG.md`, `CONTRIBUTING.md`.

**Depends on:** Tasks 1-5, and on `main` carrying #122's merge — that lot inserts 26 lines into
`CHANGELOG.md`, and rewriting all 766 before it lands would turn a small insertion into a whole-file
conflict. **Rebase first, then start.**

**Why it belongs to this lot.** `CHANGELOG.md`'s subject is *what changed, per release*. The **why** lives in
the issue and the **how** in the commit; restating either here is the misplacement D1b describes, and 766
lines for 91 entries — median entry 78 characters — says most of the file is the restating.

- [ ] **Step 1: The shape**

```markdown
- The byte-level decode substitutes U+FFFD instead of throwing. ([#149](https://github.com/CyrilB1531/data.net/issues/149), [`5948a59`](https://github.com/CyrilB1531/data.net/commit/5948a59))
```

One sentence, the issue, the commit. Nothing else — no rationale, no measurement, no caveat. Those are in
the two things the line links to.

- [ ] **Step 2: Backfill only what git can prove**

**Measured: 0 of the 91 entries carry an issue reference or a commit sha today.** So this is reconstruction,
not compression, and it is bounded by what the repository can answer:

- find each entry's commit with `git log --oneline --all --grep=<distinctive phrase>` and by reading
  `git log --follow` over the file the entry describes;
- accept a link **only when one commit matches unambiguously**. Two candidates means no link.
- take the issue from that commit's message (`Closes #n`) or its pull request, never by guessing from the
  subject.

**Fabricate nothing.** An entry whose commit cannot be identified keeps its sentence and gets no links.

- [ ] **Step 3: Mark the boundary, so a missing link reads as a date rather than an oversight**

One line in the file, above the first release that predates the convention, saying that entries before it
were written when the repository had neither an issue per lot nor this shape. A reader then knows what the
absence means.

- [ ] **Step 4: The convention goes in `CONTRIBUTING.md`**

Where the process lives — not in `CHANGELOG.md`, which is not the place that explains how to fill it in.
One example, the shape, and the rule that an entry carries no reasoning.

- [ ] **Step 5: Verify and commit**

```bash
cd <repo>
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
python3 - <<'EOF'
import pathlib, re
lines = pathlib.Path("CHANGELOG.md").read_text().splitlines()
bullets = [l for l in lines if re.match(r"^\s*[-*] ", l)]
linked = [l for l in bullets if "issues/" in l or "commit/" in l]
print(f"{len(bullets)} entries, {len(linked)} with at least one link")
EOF
git add CHANGELOG.md CONTRIBUTING.md
git commit -m "Say what changed in one sentence, and link the issue and the commit"
```

Report the count: how many of the 91 got both links, how many one, how many none, and **why** for every
entry that got none.

---

### Task 7: Final verification

**Depends on:** Tasks 1-6.

- [ ] **Step 1: Re-run the finder on the final tree**

The corpus moved under this lot — six pull requests merged into it in the last day, and the lot itself moved
sections. Re-run the candidate finder in the scratch directory and confirm the only verbatim repetitions
left are ones this lot decided to keep.

- [ ] **Step 2: The gates**

```bash
cd <repo>
git status --porcelain src/ tests/ bench/*.cs      # empty: this lot touches no code
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
python3 tools/extract_doc_snippets.py && NUGET_PACKAGES=/tmp/156-nuget ./.dotnet-guarded dotnet build samples/DataNet.DocSnippets -c Release
python3 tools/check_machine_paths.py; python3 tools/check_version_floor.py
./.dotnet-guarded dotnet test DataNet.slnx -c Release   # backstop only: 3 155 passing, unchanged
```

The snippet build needs a fresh `pack` if the packages moved; if `artifacts/` is stale, pack first.

- [ ] **Step 3: Report**

The list of every section moved and where; every paraphrase resolved and which document now states it; every
stale statement corrected with what it said and what is true; every pointer fixed. Do not push, do not open
a pull request.

---

## Self-Review

**Spec coverage.** D1 → Task 3. D1b → Tasks 1 and 2 Step 3. D2 → Task 3 Step 1's rule for who states what.
D3 → Task 2. D4 → Task 4. D5's four content checks → Tasks 1, 3 and 4; its five form criteria → Task 5,
committed on its own. The changelog's shape → Task 6, added after the maintainer asked for it: one
sentence, the issue, the commit, and links backfilled only where git resolves them unambiguously.

**Placeholders.** Task 1 Step 3 and Task 3 Step 2 are readings with no enumerable input — that is the lot's
nature, and both say what to concentrate on and what the report must list. `<repo>` stands for a path that
must not be written into a committed file.

**Type consistency.** No code. The counts — 56 files, 8 704 lines, 33 ADRs, 31 shared rows, "nineteen" in
`CLAUDE.md` — were measured on `main` at `6ed56de` and are the numbers the tasks check against.
