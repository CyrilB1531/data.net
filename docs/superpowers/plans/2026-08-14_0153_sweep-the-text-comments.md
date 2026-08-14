# #153 — Sweeping DataNet.Text's comments Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or
> superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for
> tracking.

**Goal:** Bring `src/DataNet.Text`'s 50 over-budget comment blocks inside the rule, citing the ADRs this
package already has and keeping the provenance the stemmers' comments carry.

**Architecture:** One task per area, because the zone is flat and an area shares what its claims are checked
against. Each block gets one of three outcomes — cite what answers it, run it once and cite the output, or
cut it as the opinion it is — and every block that loses a fact keeps a line naming where the fact went.

**Tech Stack:** C# XML documentation and inline comments, Markdown, `tools/check_comment_length.py`,
`tools/count_cited_claims.py`, and the reference libraries in `.venv-oracles`.

**Spec:** `docs/superpowers/specs/2026-08-14_0153_sweep-the-text-comments.md`

## Global Constraints

- Branch `docs/153-sweep-text-comments`, based on `main` at `b81eac5`. Do not push, do not open a pull
  request without asking.
- **No behaviour changes.** Comments, `docs/equivalence.md`, `docs/guides/migrating-from-rapidfuzz.md` and
  `docs/guides/vectorization.md` only. The suite is **3 147 passing, 0 failed** across eight assemblies
  before and after every task, and no byte of `tests/oracles/` moves.
- **Every `dotnet` invocation goes through `./.dotnet-guarded`**, never bare `dotnet`. It blocks with no
  deadline; let it wait.
- `dotnet build` gives no analyzer diagnostics without `--no-incremental`. Warnings are errors.
- Budgets: **two lines** inline, **eight lines of prose** in XML documentation. `long-comment: <reason>` as
  the first line where a block earns it. **The reason above a `#pragma` is already exempt** — the counter
  stopped charging it in #151, so do not "fix" one.
- **Write no ADR and take no ADR number.** A block holding a real undocumented divergence is **reported**;
  it becomes an issue, as #160 did.
- **Open every pointer before writing it.** #152 found a suppression citing decision 0013 for a claim 0013
  never makes.
- **One fact, one home.** Grep `docs/equivalence.md`, the guides and `docs/decisions/` before moving
  anything, and cite what exists rather than restating it.
- **`nltk` imports only from a neutral directory.** Any check against it runs `cd /tmp` first with
  `PYTHONSAFEPATH=1`, or it fails with `Blocked import of regex from current working directory`.
- `dotnet format DataNet.slnx --verify-no-changes` runs **once**, in the final task.
- English everywhere. Commit messages carry no `feat:`/`fix:` prefix and no process prefix.

## How to triage one block

1. **Read what it claims** — it names a reference (rapidfuzz, jellyfish, textdistance, difflib, nltk,
   scikit-learn) or asserts something about this code.
2. **Ask what would check it.** An ADR is the cheap tier *in this package*: `0004` Myers backlog, `0005`
   Hamming against jellyfish, `0006` Ratcliff `autojunk`, `0007` Metaphone scope, `0008` Italian `-enza`
   against nltk. A corpus in `tests/oracles/` is the next. Cite the file and the case.
3. **If it is executable and nothing frozen answers it**, run it once against the library in
   `.venv-oracles` and cite the output, or add the corpus case where the answer deserves freezing.
4. **If nothing reasonable checks it**, it is an opinion: cut it or rewrite it as one. Do not reformat it
   into a shorter unverifiable claim.
5. **Then fit the budget.** What survives and does not fit goes to `docs/equivalence.md` or the matching
   guide, and the block keeps **one line naming where it went**.

**In `Stemming/` and `Phonetics/`, step 4 does not apply the same way.** A comment tracing a rule to the
published algorithm description is provenance evidence under ADR 0003, not an opinion. It shortens, keeps
its reference to the description, and never becomes "matches nltk".

## Per-task shape

Written once; every task below follows it.

1. **List your blocks**: `python3 tools/check_comment_length.py | grep '<your prefix>'`.
2. **Triage and edit** by the five rules above.
3. **Verify**: `./.dotnet-guarded dotnet build DataNet.slnx -c Release --no-incremental` (0 warnings), then
   `./.dotnet-guarded dotnet test DataNet.slnx -c Release` — **3 147 passing** — and
   `git status --porcelain tests/oracles/` empty.
4. **Confirm** the same `grep` now prints nothing for your files. If Markdown changed, run markdownlint over
   the documented glob and `python3 tools/extract_doc_snippets.py`.
5. **Commit**, naming what moved and where, and any claim found false.

---

### Task 1: `Distances/` — 15 blocks

**Files:** `DamerauLevenshtein.cs` (1), `Hamming.cs` (1), `Indel.cs` (1), `Jaro.cs` (2), `JaroWinkler.cs` (1),
`Lcs.cs` (1), `Levenshtein.cs` (3), `Myers.cs` (3), `Osa.cs` (1), `RatcliffObershelp.cs` (1).

**Depends on:** nothing. First because it is the area with the most ADRs already pointing at it.

- [ ] **Step 1: Three of these have their decision already written**

`Hamming.cs:9` is ADR 0005's subject — the divergence from jellyfish on unequal lengths.
`RatcliffObershelp.cs:6` is ADR 0006's — `autojunk`. `Myers.cs:12` and `:109` are ADR 0004's — the
bit-parallel backlog and what the blocked path does. **Open each ADR and check it says what you are about to
cite**, then cite section and file rather than retelling it.

- [ ] **Step 2: The type headers are the budget's subject here**

Nine of the fifteen are 11-18 line headers on `Distance`/`Similarity` types. They typically restate the
algorithm, which the published description and the oracle corpus both already carry. Keep what a caller
needs — what it computes, what it costs, which Python function it matches — and cite the rest.

- [ ] **Step 3-5: Verify, confirm, commit** per the per-task shape.

```bash
git commit -m "Sweep the distances' comments onto the decisions that already hold them"
```

---

### Task 2: `Vectorization/` — 15 blocks

**Files:** `StopWords.cs` (2, one of 26 lines), `StopWordSet.cs` (1 of 20), `StopWords.Snowball.cs` (2),
`CsrMatrix.cs` (2), `CountVectorizer.cs` (1), `CountVectorizer.Persistence.cs` (2),
`TfidfVectorizer.Persistence.cs` (2), `HashingVectorizer.Persistence.cs` (1), `TextAnalyzer.cs` (1),
`TfidfTransformer.cs` (1).

**Depends on:** Task 1 for the guide sections it may create.

- [ ] **Step 1: The two longest are about the stop-word lists**

`StopWords.cs:3` (26 lines) and `StopWordSet.cs:7` (20) carry where the lists come from and how they differ
from scikit-learn's and nltk's. That provenance is the same class as the stemmers': the lists are fetched by
`tools/fetch_stopwords.py` against a pinned SHA-256, which is a citation stronger than any prose — **cite
the fetcher and the pinned hash** rather than describing the source.

- [ ] **Step 2: The `.Persistence.cs` trio says the same thing three times**

`CountVectorizer`, `TfidfVectorizer` and `HashingVectorizer` each open their persistence partial with a
10-12 line block about the artifact format. If they agree, one of them keeps the explanation and the other
two cite it; if they disagree, that is a finding for the report.

- [ ] **Step 3: `CsrMatrix.cs` claims a layout**

Its two blocks describe the compressed-sparse-row invariant, which is checkable against the type's own
tests. Cite the test class.

- [ ] **Step 4-5: Verify, confirm, commit** per the per-task shape.

```bash
git commit -m "Sweep the vectorizers' comments, and cite the fetcher that pins the stop-word lists"
```

---

### Task 3: `Stemming/` and `Phonetics/` — 12 blocks

**Files:** `Stemming/` — `SpanishSnowballStemmer.cs` (2), `ItalianSnowballStemmer.cs` (2),
`PortugueseSnowballStemmer.cs` (1), `PorterStemmer.cs` (1), and the rest (2).
`Phonetics/` — `Nysiis.cs` (2), `Soundex.cs` (1), `Metaphone.cs` (1).

**Depends on:** Tasks 1-2.

- [ ] **Step 1: Read ADR 0003 before touching anything in these two directories**

`docs/decisions/0003-provenance-and-licensing.md`. These are original implementations written from the
**published algorithm description**, never transcribed from a GPL reference, and the comments tracing a rule
to its step in that description are what evidences it. **A block here shortens; it does not disappear**, and
its citation is the published description — the Snowball page, the original paper — not a reference
implementation.

Two decisions already cover part of this: ADR 0007 for Metaphone's scope, ADR 0008 for the Italian `-enza`
divergence from nltk. Open both before citing them.

- [ ] **Step 2: The suppression reasons here are already exempt — leave them**

Twelve blocks in `Stemming/` stopped being counted when #151 exempted the reason above a `#pragma`. They are
not in your list and must not be "tidied" into it: `CONTRIBUTING.md` requires them, and `CLAUDE.md` demands
a reason a reviewer can disagree with, which two lines rarely meet.

- [ ] **Step 3: Anything you verify against nltk runs from `/tmp`**

```bash
cd /tmp && PYTHONSAFEPATH=1 <repo>/.venv-oracles/bin/python -c "..."
```

From the repository it fails with `Blocked import of regex from current working directory`.

- [ ] **Step 4-5: Verify, confirm, commit** per the per-task shape.

```bash
git commit -m "Sweep the stemmers and phonetic encoders, keeping what evidences their provenance"
```

---

### Task 4: `Persistence/`, `Text/` and whatever remains — 8 blocks

**Files:** `Persistence/FeatureVocabularyJson.cs` (5), `Persistence/ArtifactLoadOptions.cs` (1, 24 lines —
the longest block left in the zone), `Persistence/VectorizerOptionsJson.cs` (1), `Text/TextElement.cs` (1).

**Depends on:** Tasks 1-3.

- [ ] **Step 1: `FeatureVocabularyJson.cs` holds five of the eight**

It is the artifact format's own documentation. The format is checked by round-trip tests, which is the
citation; what a caller needs is the shape and the version field, not the reasoning behind each key.

- [ ] **Step 2: Confirm the zone is empty before you commit**

```bash
python3 tools/check_comment_length.py | grep '^src/DataNet.Text/'   # prints nothing
```

If it prints anything, it is yours — the earlier tasks each confirmed their own files.

- [ ] **Step 3-5: Verify, confirm, commit** per the per-task shape.

```bash
git commit -m "Sweep the text persistence and the last of the zone"
```

---

### Task 5: Final verification

**Depends on:** Tasks 1-4.

- [ ] **Step 1: The issue's "done when", and the claims counter**

```bash
cd <repo>
python3 tools/check_comment_length.py | grep '^src/DataNet.Text/'   # nothing
python3 tools/count_cited_claims.py src/DataNet.Text                # was 62 blocks, 4 cited (6%)
```

The second number is not a gate — the issue does not set a target — but it is the one that says whether the
sweep cited or merely shortened. Report it.

- [ ] **Step 2: Every gate**

```bash
git status --porcelain                                                                     # empty
./.dotnet-guarded dotnet build DataNet.slnx -c Release --no-incremental > /tmp/153-fv-b.log 2>&1; echo "build=$?"; grep -E "Avertissement\(s\)|Erreur\(s\)" /tmp/153-fv-b.log
./.dotnet-guarded dotnet format DataNet.slnx --verify-no-changes > /tmp/153-fv-f.log 2>&1;  echo "format=$?"
./.dotnet-guarded dotnet test DataNet.slnx -c Release > /tmp/153-fv-t.log 2>&1;             echo "test=$?"; grep -E "^Réussi!|^Échoué!" /tmp/153-fv-t.log
python3 tools/check_version_floor.py; python3 tools/check_machine_paths.py; echo "floor+paths=$?"
.venv-oracles/bin/python -m pytest tools/tests -q | tail -1
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md" > /dev/null; echo "markdownlint=$?"
python3 tools/extract_doc_snippets.py | tail -2
```

Then pack and build both samples under an isolated `NUGET_PACKAGES`, and regenerate the oracles from a
neutral directory to confirm no drift.

- [ ] **Step 3: The evidence**

Every fact that moved with its new home; every block cut as an opinion; every claim found false; every
block marked `long-comment:` with its reason; and **every ADR citation written, with confirmation it was
opened** — that last one is specific to this zone, where the ADR is the cheap tier.

- [ ] **Step 4: Stop and report.** Do not push, do not open a pull request.

---

## Self-Review

**Spec coverage.** D1 → the four area tasks. D2 → Task 1 Step 1, Task 3 Step 1, and the "open every pointer"
constraint. D3 → Task 3 Step 3 and the neutral-directory constraint. D4 → Task 3 Steps 1-2. D5 → the
Global Constraints and each task's step 5. D6 → the Global Constraints. D7 → Task 5 Step 3 and step 3 of
every task.

**Placeholders.** The five steps are shared rather than repeated; each task carries only what is specific —
which ADR already holds its subject, which blocks are provenance, which trio repeats itself. `<repo>` stands
for a path that must not be written into a committed file.

**Type consistency.** No code changes. The file names and block counts come from
`check_comment_length.py` on `main` at `b81eac5` and sum to 50: 15 + 15 + 12 + 8.
