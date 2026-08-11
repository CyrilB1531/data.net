# #44 Extract the shared Romance Snowball framework — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Move the Snowball *scaffolding* — regions, RV, `Ends`, `Delete`, `Replace`, longest-suffix search — into one internal base shared by Spanish and Portuguese, leaving each language's step logic exactly as readable as it is today, and prove the move changed nothing.

**Architecture:** `RomanceSnowballWorker` holds the scaffolding; each language supplies its vowel set and its steps. French stays out because its RV rule is different. Portuguese passes its nasal expansion through the base constructor, because the regions must see the transformed word.

**Tech Stack:** C# (net10.0 + netstandard2.0), xunit, the four existing Snowball corpora.

**Spec:** `2026-08-04_0044_extract-the-shared-snowball-framework-from-the-romance-stemmers.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `refactor/44-shared-romance-framework`. Never commit to `main`.
- **Byte-identical corpora.** `snowball_es.json`, `snowball_pt.json`,
  `snowball_fr.json`, `snowball_en.json` all replay unchanged. This is the whole
  licence for the refactor.
- **Nothing about which suffix, in what order, or under which region condition
  moves into the base.** If a step ends up there, the extraction has crossed from
  scaffolding into algorithm and must be pulled back.
- **Do not put French in the base**, however close it looks.
- Both frameworks build; warnings are errors.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

build_all()    { dotnet build -c Release; }
test_romance() { dotnet test -c Release --filter "FullyQualifiedName~Snowball"; }
test_all()     { dotnet test -c Release; }
```

---

### Task 1: Separate scaffolding from algorithm, on paper, before moving anything

**Files:** none modified.

**Depends on:** nothing.
**Produces:** the line the extraction must not cross.

- [x] **Step 1: List every member of the three Romance stemmers**

```bash
grep -nE "private|protected|internal" src/DataNet.Text/Stemming/{Spanish,Portuguese,French}SnowballStemmer.cs
```

- [x] **Step 2: Classify each**

- **Scaffolding** — `Region`, RV, `InRv`/`InR1`/`InR2`, `Ends`, `Delete`,
  `Replace`, `LongestSuffix`, `LongestSuffixInRv`. Identical by construction.
- **Algorithm** — which suffixes, in what order, under which condition. Stays.

The test: could this member be written without knowing the language? If yes, it is
scaffolding.

- [x] **Step 3: Confirm French's RV really is different**

```bash
grep -n -A15 "Rv\|RV" src/DataNet.Text/Stemming/FrenchSnowballStemmer.cs | head -30
```

Expected: the `par`/`col`/`tap` prefix cases. **French stays out.** Forcing it in
would make the base carry a language-specific branch, which is how a shared
framework turns into a bucket of exceptions.

- [x] **Step 4: Record the baseline**

```bash
test_romance 2>&1 | tail -3
git rev-parse HEAD:tests/oracles
```

"Unchanged" needs a number recorded before the change.

---

### Task 2: The base

**Files:**

- Create: `src/DataNet.Text/Stemming/RomanceSnowballWorker.cs`

**Depends on:** Task 1.

- [x] **Step 1: Move the scaffolding, verbatim**

No improvements on the way past. A pure move is reviewable; a move plus a
refactor is not.

- [x] **Step 2: The constructor takes the word already transformed**

Portuguese expands nasals (`ã` → `a~`) **before** regions are computed. The base
must accept the transformed string rather than the original — get this wrong and
`geração` stems differently, which is a word-shaped wrong answer no reviewer would
catch by eye.

- [x] **Step 3: Each language supplies its vowel set and its steps**

---

### Task 3: Spanish, then Portuguese, one at a time

**Files:**

- Modify: `src/DataNet.Text/Stemming/SpanishSnowballStemmer.cs`
- Modify: `src/DataNet.Text/Stemming/PortugueseSnowballStemmer.cs`

**Depends on:** Task 2.

- [x] **Step 1: Spanish onto the base**

```bash
build_all && dotnet test -c Release --filter "FullyQualifiedName~SpanishSnowball" 2>&1 | tail -3
```

Expected: green, same count. Do not start Portuguese until this passes — two
languages in flight means a corpus failure has two candidate causes.

- [x] **Step 2: Portuguese onto the base, with the nasal expansion through the
      constructor**

```bash
dotnet test -c Release --filter "FullyQualifiedName~PortugueseSnowball" 2>&1 | tail -3
```

- [x] **Step 3: Sanity-check the specific case the transformation order breaks**

```bash
python3 -c "
import json; d=json.load(open('tests/oracles/snowball_pt.json'))
print([c for c in d['cases'] if c['input']=='geração'])
"
```

Compare against what the code now produces. If the base computed the regions on
the untransformed word, this is the case that moves.

- [x] **Step 4: Merge the two identical branches in Portuguese step 5**

`S1871`. The published description lists `gu` and `ci` separately, but both drop
the same single character. A genuine simplification, and it belongs here rather
than in a second pass over the same file.

- [x] **Step 5: Confirm the diff is a deletion**

```bash
git diff --stat main -- src/DataNet.Text/Stemming/
```

Expected shape: the two language files losing far more than the base gains — on
the order of 33 insertions against 192 deletions across the two.

---

### Task 4: Prove it is inert

**Depends on:** Task 3.
**Produces:** the licence for the whole change.

- [x] **Step 1: All four corpora**

```bash
build_all && test_romance 2>&1 | tail -3
```

Expected: every Snowball test green, at the counts recorded in Task 1.

- [x] **Step 2: The corpora themselves untouched**

```bash
git status --porcelain tests/oracles/
```

Expected: empty. This branch does not regenerate anything.

- [x] **Step 3: Whole suite and format**

```bash
test_all 2>&1 | tail -3
dotnet format --verify-no-changes
```

Expected: 164/164.

- [x] **Step 4: Read the duplication figure on the pushed branch**

That is the reason this branch exists, and it is the one number only SonarQube
Cloud can give. Expect a substantial drop; if it does not move, the extraction did
not reach the duplicated code.

---

### Task 5: The tail nobody enforces

**Depends on:** Task 4.

- [x] **Step 1: Check whether any suppression was left behind**

```bash
grep -n "pragma warning disable" src/DataNet.Text/Stemming/*.cs
```

`Delete` and `Replace` carry a `CA1845` suppression in the language files.
**Moving code does not move its `#pragma`** — the rule will reappear against
`RomanceSnowballWorker`, and the build stays green because nothing here runs the
analyzer.

If it is visible now, fix it here. If it surfaces on the dashboard after merge, it
is a follow-up — and worth stating in the pull request that it is a known
consequence rather than a surprise.

- [x] **Step 2: Commit**

```bash
git add src/DataNet.Text/Stemming/
git commit -m "Extract the Snowball framework shared by the Romance stemmers"
```
