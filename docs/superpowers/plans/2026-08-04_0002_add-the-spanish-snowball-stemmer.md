# #2 Spanish Snowball stemmer — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `SpanishSnowballStemmer.Stem(word)` reproduces `nltk.stem.snowball.SnowballStemmer("spanish")` on every word of a frozen corpus, as an original implementation of the published Snowball algorithm.

**Architecture:** One `public static class` beside the English and French stemmers, carrying its own RV/R1/R2 machinery — the third copy, accepted deliberately and handed to a follow-up issue. Step 0 (attached object pronouns) runs before any suffix stripping; accents are removed last. A new `generate_oracles.py` section freezes the reference values into `tests/oracles/snowball_es.json`, which the test suite replays.

**Tech Stack:** C# (net10.0 + netstandard2.0), xunit, Python 3 with `nltk` for oracle generation only.

**Spec:** `2026-08-04_0002_add-the-spanish-snowball-stemmer.md` (in `../specs/`).

## Global Constraints

- **Everything in English** — code comments, commit messages, PR body.
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `feat/2-spanish-snowball-stemmer`. Never commit to `main`.
- **ADR 0003 — provenance.** Write from the published Snowball description. Do not
  open nltk's `snowball.py` to *derive* the implementation. Reading it to
  *diagnose a specific failing case* is allowed and is what Task 4 does, but the
  code must be the algorithm's, not nltk's.
- `TreatWarningsAsErrors=true` repo-wide. Both `net10.0` and `netstandard2.0` build.
- **The oracle comes first.** Do not write the stemmer, then a corpus that agrees
  with it. Generate the corpus from nltk, then make the C# match it.
- Regenerating the corpora must add `snowball_es.json` and **touch nothing else**.
  Any other file moving is drift, and drift is a stop.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

# Oracle regeneration. The venv and a neutral working directory both matter:
# nltk resolves its data relative to cwd, and the generator's exit code is the
# only thing that says whether it worked — a green log is not enough.
regen() {
  . .venv-oracles/bin/activate
  python tools/generate_oracles.py
  echo "generator exit: $?"
}

build_all() { dotnet build -c Release; }
test_es()   { dotnet test -c Release --filter "FullyQualifiedName~SpanishSnowball"; }
test_all()  { dotnet test -c Release; }
```

---

### Task 1: Freeze the oracle before writing any implementation

**Files:**

- Modify: `tools/generate_oracles.py`
- Create: `tests/oracles/snowball_es.json`

**Depends on:** nothing.
**Produces:** the definition of correct that every later task is measured against.

- [ ] **Step 1: Read how French does it**

```bash
grep -n "snowball_fr\|SnowballStemmer" tools/generate_oracles.py
```

The Spanish section is the French one with a different language and word list.
Reuse the existing corpus helper rather than adding a second one — a duplicate
definition is something Python accepts silently.

- [ ] **Step 2: Add the Spanish section**

`nltk.stem.snowball.SnowballStemmer("spanish")` over a word list chosen to
exercise every step, not just common words. It must include:

- attached-pronoun forms: `dámelo`, `haciéndola`, `construyendolo`
- the `amente` / `mente` overlap: `rápidamente`, `claramente`
- the `idades` / `idad` overlap: `universidades`, `ciudad`
- accented forms that must survive until the final step: `país`, `además`
- verb endings across steps 2a and 2b

- [ ] **Step 3: Generate, and read the exit code**

```bash
regen
git status --porcelain tests/oracles/
```

Expected: `snowball_es.json` added, **and nothing else listed**. If another corpus
moved, stop — the environment differs from the one that produced the committed
files, and every number in this branch would be suspect.

- [ ] **Step 4: Sanity-check the corpus by hand**

```bash
python -c "
import json; d=json.load(open('tests/oracles/snowball_es.json'))
print(d['metadata']); print(len(d['cases']))
print([c for c in d['cases'] if c['input']=='construyendolo'])
"
```

Record the case count. `construyendolo` is printed on purpose: it is the case D3
warns about, and knowing its expected value now means Task 4 cannot rationalise a
wrong answer later.

---

### Task 2: The stemmer

**Files:**

- Create: `src/DataNet.Text/Stemming/SpanishSnowballStemmer.cs`

**Depends on:** Task 1.
**Produces:** the implementation, not yet trusted.

- [ ] **Step 1: Regions**

`RV`, `R1`, `R2` per the published definitions. Spanish's RV rule is the one with
three branches on the first two letters — write it as three explicit branches, not
as a clever combined condition, because it is read far more often than it is run.

- [ ] **Step 2: Step 0 — attached object pronouns**

Only after one of the permitted preceding forms. Two sub-cases from D3:

- `iéndo`, `ándo`, `ár`, `ér`, `ír` → delete the pronoun **and drop the accent**.
- `yendo` → delete **only when the stem ends in `uyendo`**.

Write the second as a suffix test on the word, never as a character lookup at a
computed offset.

- [ ] **Step 3: Step 1 — longest suffix across all groups at once**

One combined longest-match, per D5. If the implementation has a loop over groups,
it is wrong.

- [ ] **Step 4: Steps 2a, 2b, 3**

Per the published description.

- [ ] **Step 5: Remove acute accents — last**

After every other step. Add a one-line comment saying why the position matters;
this is the kind of line a later refactor moves "for tidiness".

- [ ] **Step 6: It compiles on both targets**

```bash
build_all
```

CA1845 will fire, as it does in the English and French stemmers. Suppress it with
the same reason those files give — the span-based `string.Concat` overload is
net-only and the `Substring` form is what compiles for netstandard2.0. **Copy
their wording**; a reader comparing the three should find them identical.

---

### Task 3: Replay the corpus

**Files:**

- Create: `tests/DataNet.Text.Tests/Stemming/SpanishSnowballStemmerOracleTests.cs`

**Depends on:** Task 2.
**Produces:** the failure list that Task 4 works through.

- [ ] **Step 1: Write the replay test in the shape the French one uses**

Same loader, same assertion, same naming. Do not invent a second convention.

- [ ] **Step 2: Confirm the test actually runs**

```bash
test_es 2>&1 | tail -5
```

Expected: a non-zero test count. A filter that matches nothing reports success —
read the count, not the colour. If it says `0 tests`, fix the filter before
reading anything into the result.

- [ ] **Step 3: Record the failures**

```bash
test_es 2>&1 | grep -E "^\s+(Assert|Expected|Actual|Failed)" | head -40
```

---

### Task 4: Work the failures down to zero

**Files:**

- Modify: `src/DataNet.Text/Stemming/SpanishSnowballStemmer.cs`

**Depends on:** Task 3.
**Produces:** 127/127, or whatever Task 1 Step 4 recorded.

- [ ] **Step 1: Fix, one failing case at a time**

For each failure, name the step responsible before editing. A fix that makes a
case pass without identifying which step was wrong is a fix that will break
another case later.

- [ ] **Step 2: The `uyendo` case, specifically**

If `construyendolo` stems to `construyendol` rather than `constru`, the step 0
`yendo` condition was read as "the characters `uy` sit at a fixed offset" instead
of "the stem ends in `uyendo`". This is the expected failure, it is expected to be
the *only* one, and it is exactly what an oracle exists to catch.

- [ ] **Step 3: When a case resists, read nltk's source for that case only**

Permitted, and different from deriving the implementation from it: the corpus says
*something* is wrong but not what. Diagnose, then fix against the published
description.

- [ ] **Step 4: Green**

```bash
test_es 2>&1 | tail -3
```

Expected: 127/127 (the count from Task 1).

---

### Task 5: Full gate

**Depends on:** Task 4.

- [ ] **Step 1: Whole suite, both frameworks**

```bash
build_all && test_all 2>&1 | tail -3
dotnet format --verify-no-changes
```

Expected: 160/160 (158 + 2 new), 0 warnings, format clean.

- [ ] **Step 2: Prove the corpora did not drift**

```bash
regen && git status --porcelain tests/oracles/
```

Expected: only `snowball_es.json`, and it must reproduce byte-identically after
being committed. Determinism is a property this repository claims; check it here
rather than discover it broken three languages later.

- [ ] **Step 3: Commit**

```bash
git add src/DataNet.Text/Stemming/SpanishSnowballStemmer.cs \
        tests/DataNet.Text.Tests/Stemming/SpanishSnowballStemmerOracleTests.cs \
        tests/oracles/snowball_es.json tools/generate_oracles.py
git commit -m "Add the Spanish Snowball stemmer"
```

The CA1845 suppression is a second, separate commit — it is a different concern
from the algorithm and reviews better on its own:

```bash
git commit -am "Suppress CA1845 in the Spanish stemmer, as in English and French"
```

- [ ] **Step 4: Open the follow-up D1 promised**

"Extract the shared Snowball framework from the Romance stemmers." Three copies of
the region machinery now exist. With two Romance corpora green, the extraction can
be proven inert — which is precisely why it is a separate branch.
