# #5 German Snowball stemmer — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `GermanSnowballStemmer.Stem(word)` reproduces `nltk.stem.snowball.SnowballStemmer("german")` on every word of a frozen corpus, without re-copying the region machinery that #44, #47 and #48 spent three branches consolidating.

**Architecture:** Two halves with a hard barrier between them. First `RomanceSnowballWorker` splits into a language-neutral `SnowballWorkerBase` (R1/R2, suffix primitives, rule table) and a Romance layer holding RV and everything built on it — proven inert by replaying all four Romance corpora. Only then is German written, deriving from the base.

**Tech Stack:** C# (net10.0 + netstandard2.0), xunit, Python 3 with `nltk` for oracle generation only.

**Spec:** `2026-08-04_0005_add-the-german-snowball-stemmer.md` (in `../specs/`).

## Global Constraints

- **Everything in English** — code comments, commit messages, PR body.
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `feat/5-german-snowball-stemmer`. Never commit to `main`.
- **ADR 0003 — provenance.** Original implementation from the published
  description.
- **The split lands green before German is written.** Task 1 finishes with all
  four Romance corpora replaying unchanged. Do not begin Task 3 until it does.
- **German does not derive from `RomanceSnowballWorker`.** If it needs something
  that lives there, either the member belongs in the base — move it — or German
  needs its own, and the difference is real. Do not widen the Romance class.
- `TreatWarningsAsErrors=true`; both frameworks build.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

regen() {
  . .venv-oracles/bin/activate
  python tools/generate_oracles.py
  echo "generator exit: $?"
}

build_all()    { dotnet build -c Release; }
test_romance() { dotnet test -c Release --filter "FullyQualifiedName~Snowball"; }
test_de()      { dotnet test -c Release --filter "FullyQualifiedName~GermanSnowball"; }
test_all()     { dotnet test -c Release; }
```

---

### Task 1: Split the worker

**Files:**

- Create: `src/DataNet.Text/Stemming/SnowballWorkerBase.cs`
- Modify: `src/DataNet.Text/Stemming/RomanceSnowballWorker.cs`

**Depends on:** nothing.
**Produces:** a base class German can derive from, and four green corpora proving
the move changed nothing.

- [ ] **Step 1: Record the baseline before touching anything**

```bash
test_romance 2>&1 | tail -3
```

Write down the count. "Unchanged" is only meaningful against a number recorded
before the change.

- [ ] **Step 2: Sort every member of `RomanceSnowballWorker` into one of two piles**

```bash
grep -nE "protected|private|internal" src/DataNet.Text/Stemming/RomanceSnowballWorker.cs
```

- **Base** — R1/R2 computation, the suffix primitives (longest match, ends-with,
  replace), the rule table from #48.
- **Romance** — RV and everything that reads it: `InRv`, `LongestSuffixInRv`,
  `StripAmente`.

The test for a member is whether it mentions RV, directly or through a helper. If
it does, it stays.

- [ ] **Step 3: Move the base pile into `SnowballWorkerBase`**

`RomanceSnowballWorker` derives from it. No behaviour edited in this step — a pure
move. Resist the urge to improve anything on the way past.

- [ ] **Step 4: Prove the split is inert**

```bash
build_all && test_romance 2>&1 | tail -3
```

Expected: the exact count from Step 1, all green. French, Spanish, Portuguese and
Italian all replay.

If a corpus moves, the likely cause is a region helper reading state initialised
in the wrong constructor — the failure mode D2 names. Do not proceed to Task 2
with a red corpus.

---

### Task 2: Freeze the German oracle

**Files:**

- Modify: `tools/generate_oracles.py`
- Create: `tests/oracles/snowball_de.json`

**Depends on:** Task 1.

- [ ] **Step 1: Add the German section**

`nltk.stem.snowball.SnowballStemmer("german")`. The word list must exercise the
three conditions from D3 head-on, because they are the ones that pass a casual
reading:

- the bare final `s`: words ending in a valid s-ending, and words that are not
- `st` with fewer than three preceding letters — **`ist` must survive**
- `ig`/`ik`/`isch` directly after an `e`, which must **not** be removed
- `ß`: `heißen`, `weiß`
- umlauts that get stripped at the end: `häuser`, `bücher`
- `u`/`y` between vowels

- [ ] **Step 2: Generate, read the exit code, check for drift**

```bash
regen
git status --porcelain tests/oracles/
```

Expected: `snowball_de.json` added, nothing else moved.

- [ ] **Step 3: Record the count and the sentinel answers**

```bash
python -c "
import json; d=json.load(open('tests/oracles/snowball_de.json'))
print('cases:', len(d['cases']))
print([c for c in d['cases'] if c['input'] in ('ist','weiß','häuser')])
"
```

---

### Task 3: The stemmer

**Files:**

- Create: `src/DataNet.Text/Stemming/GermanSnowballStemmer.cs`

**Depends on:** Task 2.

- [ ] **Step 1: Entry re-spelling**

`ß` → `ss`; upper-case `u` and `y` between vowels. Same device as Italian's `qu`
handling and Portuguese's nasals.

- [ ] **Step 2: R1 and R2, with R1 floored at 3**

The floor is the German-specific part and belongs in this file, not in the base —
the base computes R1/R2, German constrains where R1 may start.

- [ ] **Step 3: Step 1 — `em ern er`, `e en es`, and the bare `s`**

The `s` rule per D3: only after a valid s-ending, and **that letter need not be in
R1**. Write the condition so this is visible; a reader who sees a region test on
the wrong character will "fix" it.

- [ ] **Step 4: Step 2 — `en er est`, and `st`**

`st` requires a valid st-ending with at least three letters before it. Add a
comment naming `ist` as the word this protects.

- [ ] **Step 5: Step 3 — derived suffixes**

`end ung ig ik isch lich heit keit`. `ig`, `ik` and `isch` are **never** removed
straight after an `e`.

- [ ] **Step 6: Exit — strip umlauts, restore case**

Remove the umlaut from `a o u`, lower-case `U`/`Y`.

- [ ] **Step 7: Both targets compile**

```bash
build_all
```

---

### Task 4: Replay the corpus

**Files:**

- Create: `tests/DataNet.Text.Tests/Stemming/GermanSnowballStemmerOracleTests.cs`

**Depends on:** Task 3.

- [ ] **Step 1: Same shape as the other five replay tests**

- [ ] **Step 2: Confirm the test count is non-zero, then read the result**

```bash
test_de 2>&1 | tail -5
```

Expected: 88/88.

If it passes on the first run, **do not treat that as a reason to trust the
implementation more than the corpus warrants** — check that the corpus actually
contains the D3 cases before concluding anything:

```bash
python -c "
import json; d=json.load(open('tests/oracles/snowball_de.json'))
print([c['input'] for c in d['cases']][:20])
"
```

Each of the five Romance languages hid a divergence the corpus found. German
passing cleanly is a fact about German, not evidence that a smaller corpus would
have done.

---

### Task 5: Documentation and full gate

**Files:**

- Modify: `docs/equivalence.md`

**Depends on:** Task 4.

- [ ] **Step 1: The German row, in the same commit as the code**

The rule #48 had to restate in `CONTRIBUTING.md`. Six languages now: English,
French, Spanish, Portuguese, Italian, German.

- [ ] **Step 2: Re-read anything that counts**

`README.md` and `docs/equivalence.md` both state language counts and a total word
count. Recompute the total rather than incrementing it:

```bash
python -c "
import json,glob
t=0
for f in sorted(glob.glob('tests/oracles/snowball_*.json'))+['tests/oracles/porter.json']:
    try: n=len(json.load(open(f))['cases']); t+=n; print(f, n)
    except FileNotFoundError: pass
print('total', t)
"
```

Expected: 758 across the Snowball corpora. Use the number this prints, not the
number a previous document claims.

- [ ] **Step 3: Everything**

```bash
build_all && test_all 2>&1 | tail -3
dotnet format --verify-no-changes
npx --yes markdownlint-cli2 "**/*.md" "#node_modules"
```

Expected: 168/168, 0 warnings, format clean, markdownlint 0 issues.

- [ ] **Step 4: No drift**

```bash
regen && git status --porcelain tests/oracles/
```

Expected: `snowball_de.json` only.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "Add the German Snowball stemmer"
```
