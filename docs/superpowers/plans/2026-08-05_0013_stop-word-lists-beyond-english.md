# #13 Multilingual stop-word lists — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Ship `StopWords.French`, `.German`, `.Italian`, `.Portuguese` and `.Spanish` from a source this project is actually permitted to redistribute, with the divergence from `nltk` measured and recorded rather than discovered by a user.

**Architecture:** Licence check first, decision recorded second, lists generated last. `tools/fetch_stopwords.py` downloads the five Snowball files, verifies each against a pinned SHA-256 and emits `StopWords.Snowball.cs`; a CI job replays it with `--check`. `StopWords.English` is untouched.

**Tech Stack:** Python 3 standard library only (no dependency), C# partial class, GitHub Actions.

**Spec:** `2026-08-05_0013_stop-word-lists-beyond-english.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `feat/13-multilingual-stop-words`. Never commit to `main`.
- **Do not use `nltk.corpus.stopwords` as a source.** Not as a starting point, not
  "to compare and then retype". Task 1 establishes why; the constraint holds from
  the first line of code.
- **The generated file is never hand-edited.** If a word looks wrong, the pin or
  the generator is wrong.
- Both frameworks build; warnings are errors.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

build_all() { dotnet build -c Release; }
test_sw()   { dotnet test -c Release --filter "FullyQualifiedName~StopWords"; }
test_all()  { dotnet test -c Release; }
gen()       { python3 tools/fetch_stopwords.py; }
check()     { python3 tools/fetch_stopwords.py --check; }
```

---

### Task 1: The licence check, before choosing a source

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the finding that decides everything else.

This task can end the branch. Do it first.

- [ ] **Step 1: Read what `nltk_data` actually says about the `stopwords` package**

Check `index.xml` for a `license` attribute on that package, and read
`LICENSE-OVERVIEW.md`.

Expected finding: the package sits under **"Unclarified, Unknown, Ambiguous, or
Citation-Only"**, with no `license` attribute, and the overview says the
repository-wide Apache-2.0 governs *the repository*, not the individual data
packages.

- [ ] **Step 2: Check what the existing attribution actually covers**

```bash
grep -n -A5 "nltk" THIRD-PARTY-NOTICES.md
```

The Apache-2.0 recorded there covers the **code executed to generate oracles**. It
does not extend to a corpus compiled into the shipped assembly. Running someone's
code at development time and redistributing their data are different acts.

- [ ] **Step 3: Find the upstream source and its licence**

Snowball publishes the same lists under BSD-3-Clause (© 2001 Dr Martin Porter,
© 2002 Richard Boulton). Confirm the URLs resolve and record them — they become
the pinned inputs in Task 3.

- [ ] **Step 4: Measure the divergence, per language**

Download both, compare as sets, and record the four numbers per language: count
here, count in nltk, only-here, only-in-nltk. Do this **now**, not after the code
works — the numbers go in the ADR, the equivalence table, the guide and the tests,
and recomputing them later invites four copies that disagree.

---

### Task 2: Record the decision before writing the code

**Files:**

- Create: `docs/decisions/0010-stop-word-list-provenance.md`

**Depends on:** Task 1.

- [ ] **Step 1: ADR 0010**

The finding, the source chosen, and — explicitly — that this **reverses the call
made in ADR 0008**. There, nltk won against the published description; here,
Snowball wins against nltk. The rule is not "always follow nltk", it is "follow
the reference the corpus is frozen from, unless redistribution forbids it". Say
that, or the next contributor will apply the wrong precedent.

- [ ] **Step 2: The per-language divergence table, from Task 1's numbers**

- [ ] **Step 3: Why `StopWords.English` is excluded**

Snowball's English list is 174 words; scikit-learn's is 318. `stop_words="english"`
parity is the product; consistency across five lists is not.

---

### Task 3: The generator

**Files:**

- Create: `tools/fetch_stopwords.py`
- Create: `src/DataNet.Text/Vectorization/StopWords.Snowball.cs` (generated)
- Modify: `tools/README.md`

**Depends on:** Task 2.

- [ ] **Step 1: Download, verify, then emit — in that order**

Each file checked against a **pinned SHA-256 before use**. This is shipped source,
not a test fixture: a silent upstream edit would change the library.

Standard library only. This script has no business pulling a dependency.

- [ ] **Step 2: A `--check` mode that verifies the committed file is current**

Regenerates in memory and compares. This is what CI runs.

- [ ] **Step 3: Generate, and read the diff**

```bash
gen
git diff --stat src/DataNet.Text/Vectorization/StopWords.Snowball.cs
```

- [ ] **Step 4: Document the failure mode in `tools/README.md`**

A hash mismatch means Snowball edited the list upstream: read the diff, update the
pin, adjust the counts in `StopWordsTests`, record it. **Do not regenerate
quietly.** And state that the nltk corpus is not a permitted source here, with a
pointer to ADR 0010 — the script is where someone will be tempted.

---

### Task 4: Expose the lists

**Files:**

- Modify: `src/DataNet.Text/Vectorization/StopWords.cs`
- Modify: `tests/DataNet.Text.Tests/Vectorization/StopWordsTests.cs`

**Depends on:** Task 3.

- [ ] **Step 1: Five properties on the existing partial class**

`French`, `German`, `Italian`, `Portuguese`, `Spanish` — one per language that
already has a Snowball stemmer.

- [ ] **Step 2: XML doc naming the divergence at the point of use**

On the class and on each property: these are Snowball's lists, not nltk's, and
they differ. A reader comparing output to a Python script needs this where they
are looking, not only in an ADR.

- [ ] **Step 3: Tests asserting the counts from Task 1**

```bash
test_sw 2>&1 | tail -3
```

The counts are the tripwire: if the pin ever moves, these fail and force the
decision.

---

### Task 5: Attribution, documentation and CI

**Files:**

- Modify: `NOTICE`, `THIRD-PARTY-NOTICES.md`
- Modify: `docs/equivalence.md`, `docs/guides/vectorization.md`, `README.md`,
  `CHANGELOG.md`
- Modify: `.github/workflows/ci.yml`, `.github/workflows/sonarcloud.yml`

**Depends on:** Task 4.

- [ ] **Step 1: Attribution, because this ships**

Snowball, BSD-3-Clause, both copyright holders, in `NOTICE` **and**
`THIRD-PARTY-NOTICES.md`.

- [ ] **Step 2: The divergence in the three places a reader lands**

`equivalence.md` (a row per language, marked *not identical*), the vectorization
guide, and the changelog. All from Task 1's numbers — do not recompute per
document.

- [ ] **Step 3: The CI job**

`python tools/fetch_stopwords.py --check`, in its own job. No dependency to
install. Separate from the oracle jobs because this guards a *shipping* guarantee,
not a test fixture.

- [ ] **Step 4: Exclude `tools/` from SonarCloud coverage**

The generator is development tooling with no coverage to report; leaving it in
drags the coverage figure down for a file that is never executed by the suite.
Separate commit.

- [ ] **Step 5: Full gate**

```bash
build_all && test_all 2>&1 | tail -3
check
dotnet format --verify-no-changes
npx --yes markdownlint-cli2 "**/*.md" "#node_modules"
```

Expected: clean on both frameworks; `--check` green; format and markdownlint
clean.

- [ ] **Step 6: Commit, in the order the work was done**

```bash
git commit -m "Record where multilingual stop-word lists may come from"
git commit -m "Add Snowball stop-word lists for the five other stemmer languages"
git commit -m "Exclude tools/ from SonarCloud coverage"
```
