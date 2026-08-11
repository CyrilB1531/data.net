# #4 Italian Snowball stemmer — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `ItalianSnowballStemmer.Stem(word)` reproduces `nltk.stem.snowball.SnowballStemmer("italian")` on every word of a frozen corpus, built on the shared `RomanceSnowballWorker` rather than on a fourth copy of the region machinery.

**Architecture:** Three separable pieces, in this order. First the rule table: `RomanceSnowballWorker` gains a data-driven step 1, and French, Spanish and Portuguese are converted onto it with their corpora proving the conversion inert. Then Italian itself, contributing only its steps and vowel set. Then the documentation the earlier stemmers owe — ADR 0008 for the nltk divergence, and the `equivalence.md` rows #42 and #43 omitted.

**Tech Stack:** C# (net10.0 + netstandard2.0), xunit, Python 3 with `nltk` for oracle generation only.

**Spec:** `2026-08-04_0004_add-the-italian-snowball-stemmer.md` (in `../specs/`).

## Global Constraints

- **Everything in English** — code, ADR, `CONTRIBUTING.md`, commit messages, PR body.
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `feat/4-italian-snowball-stemmer`. Never commit to `main`.
- **ADR 0003 — provenance.** Original implementation from the published
  description. Reading nltk's `snowball.py` to diagnose a specific failing case is
  permitted and Task 3 does exactly that; deriving the implementation from it is
  not.
- **The rule-table conversion must be inert.** French, Spanish and Portuguese
  corpora replay byte-identically, before Italian is written. If the conversion
  and the new language are entangled in one diff, neither can be trusted.
- `TreatWarningsAsErrors=true`; both frameworks build.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

regen() {
  . .venv-oracles/bin/activate
  python tools/generate_oracles.py
  echo "generator exit: $?"
}

build_all()  { dotnet build -c Release; }
test_romance() { dotnet test -c Release --filter "FullyQualifiedName~Snowball"; }
test_it()    { dotnet test -c Release --filter "FullyQualifiedName~ItalianSnowball"; }
test_all()   { dotnet test -c Release; }
```

---

### Task 1: Express the Romance step 1 as a rule table

**Files:**

- Modify: `src/DataNet.Text/Stemming/RomanceSnowballWorker.cs`
- Modify: `src/DataNet.Text/Stemming/SpanishSnowballStemmer.cs`
- Modify: `src/DataNet.Text/Stemming/PortugueseSnowballStemmer.cs`

**Depends on:** nothing.
**Produces:** the shape Italian will plug into — and, more importantly, three
existing languages proving it is faithful.

Do this **before** writing any Italian. A table validated by three green corpora
is a foundation; a table validated by nothing while a fourth language is being
debugged is a second variable.

- [x] **Step 1: Read the three step-1 implementations side by side**

```bash
grep -n "Step1\|step 1" src/DataNet.Text/Stemming/{French,Spanish,Portuguese}SnowballStemmer.cs
```

They are the same shape three times: a suffix, a region condition, a replacement
or a deletion, longest match first.

- [x] **Step 2: Add the table to `RomanceSnowballWorker`**

A rule is `(suffix, region, action)`. The worker takes an ordered set and applies
longest-match-across-all-groups, which is the semantics #2 established in its D5 —
per-group scanning gives the wrong answer when the groups overlap.

- [x] **Step 3: Convert Spanish and Portuguese onto it**

One language at a time, running that language's corpus after each.

- [x] **Step 4: Prove the conversion changed nothing**

```bash
test_romance 2>&1 | tail -3
```

Expected: every existing Snowball test green, same counts as before the branch.
This is the whole justification for doing the refactor here; if it is not clean,
revert and write Italian as a fourth chain instead.

- [x] **Step 5: Handle S3267 if it fires**

The table-driven loop may trip S3267 (loop should be simplified with LINQ). Suppress
it in the worker with a reason: the loop carries an early exit on longest match and
a LINQ rewrite would either lose that or allocate per call, in a method on the hot
path of every stem. Reason in the source, per `CONTRIBUTING.md`.

---

### Task 2: Freeze the Italian oracle

**Files:**

- Modify: `tools/generate_oracles.py`
- Create: `tests/oracles/snowball_it.json`

**Depends on:** Task 1.
**Produces:** the definition of correct — and, per D5, the only thing that will
reveal the divergence between the published description and nltk.

- [x] **Step 1: Add the Italian section**

`nltk.stem.snowball.SnowballStemmer("italian")`. The word list **must include
`enza`/`enze` words whose suffix falls inside R2**, or the divergence D5 describes
goes undetected and the wrong rule ships:

- inside R2 — `esistenza`, `sussistenza`
- outside R2, which agree under either reading — `potenza`, `pazienza`,
  `partenza`, `presenza`
- accent folding: `perché`, `perchè`
- `u` after `q`: `qualunque`, `quando`
- step 0 with infinitive restoration: `mandarci`, `parlarmi`

The four "agreeing" words are in the corpus on purpose. They document that the
divergence is narrow, which is the reason it survived being written from the
prose.

- [x] **Step 2: Generate, read the exit code, check for drift**

```bash
regen
git status --porcelain tests/oracles/
```

Expected: `snowball_it.json` added, nothing else moved.

- [x] **Step 3: Record what nltk says about `esistenza`**

```bash
python -c "
import json; d=json.load(open('tests/oracles/snowball_it.json'))
print('cases:', len(d['cases']))
print([c for c in d['cases'] if c['input'] in ('esistenza','potenza','perché','mandarci')])
"
```

Write down the value for `esistenza`. Task 3 will produce a different one, and the
temptation will be to assume the corpus is wrong.

---

### Task 3: The stemmer

**Files:**

- Create: `src/DataNet.Text/Stemming/ItalianSnowballStemmer.cs`

**Depends on:** Task 2.

- [x] **Step 1: Fold acute accents to grave, first**

Before regions, before anything.

- [x] **Step 2: Upper-case `u` after `q`, and `u`/`i` between vowels**

So the regions treat them as consonants. Lower-case them again as the last act.
Comment that this is the same device Portuguese uses for nasals — a temporary
re-spelling to make region computation correct.

- [x] **Step 3: Step 0 — attached pronouns, restoring the infinitive `e`**

`mandarci` → `mandare`. **Not** the Spanish behaviour. If this file was written
with the Spanish one open, this is the line that will be wrong.

- [x] **Step 4: Steps 1, 2, 3a, 3b through the rule table**

3b replaces `ch` with `c` and `gh` with `g` inside RV.

- [x] **Step 5: Run the corpus and expect exactly two failures**

```bash
test_it 2>&1 | grep -E "Expected|Actual" | head -20
```

Expected: `esistenza` and one more `enza`-in-R2 word, and **nothing else**. If
other cases fail, fix those first — they are ordinary bugs and they will confuse
the diagnosis in Step 6.

- [x] **Step 6: Diagnose the two, by reading nltk's source for that rule**

The published description says `enza`/`enze` → `ente` if in R2. Find what nltk
actually does:

```bash
python -c "
import inspect, nltk.stem.snowball as s
src = inspect.getsource(s.ItalianStemmer)
i = src.find('enza')
print(src[i-200:i+400])
"
```

Expected: a `suffix_replace(word, suffix, \"te\")` — `te`, not `ente`.

Implement nltk's behaviour. This is a deliberate divergence from the published
prose, and Task 5 records it.

- [x] **Step 7: Green**

```bash
test_it 2>&1 | tail -3
```

Expected: 96/96.

---

### Task 4: Replay test

**Files:**

- Create: `tests/DataNet.Text.Tests/Stemming/ItalianSnowballStemmerOracleTests.cs`

**Depends on:** Task 3.

- [x] **Step 1: Same shape as the Spanish and Portuguese replay tests**
- [x] **Step 2: Confirm the test count is non-zero before reading the colour**

---

### Task 5: The documentation this branch owes

**Files:**

- Create: `docs/decisions/0008-italian-enza-nltk-divergence.md`
- Modify: `docs/equivalence.md`
- Modify: `CONTRIBUTING.md`

**Depends on:** Task 4.
**Produces:** the record, plus the repair of a rule that has now been missed twice.

- [x] **Step 1: ADR 0008**

The published rule, what nltk does instead, the resulting stems under both
readings, and **why nltk wins**: the corpora are frozen from it and
`equivalence.md` names it as the reference. Cite ADR 0005 as the precedent — the
same call, made for jellyfish.

Include the observation that the divergence only shows when the suffix is inside
R2, and that four common words agree under either reading. That is the part a
future reader needs in order to trust the corpus over their own reading of the
prose.

- [x] **Step 2: `equivalence.md` — three rows, not one**

Italian, **and the Spanish and Portuguese rows #42 and #43 omitted**. The Italian
row names the divergence and links ADR 0008.

- [x] **Step 3: `CONTRIBUTING.md`**

The rule was broken twice in a row, so restate it where the next contributor will
read it: an `equivalence.md` row lands in the same commit as the function it
describes, never afterwards.

- [x] **Step 4: Documentation self-check**

Anything counting or enumerating languages, ADRs or corpora goes stale silently.
Re-read `README.md`, `docs/equivalence.md` and `CONTRIBUTING.md` for counts and
"see X" references before the PR.

---

### Task 6: Full gate

**Depends on:** Task 5.

- [x] **Step 1: Everything**

```bash
build_all && test_all 2>&1 | tail -3
dotnet format --verify-no-changes
npx --yes markdownlint-cli2 "**/*.md" "#node_modules"
```

Expected: 166/166, 0 warnings, format clean, markdownlint 0 issues across 26 files.

- [x] **Step 2: No drift**

```bash
regen && git status --porcelain tests/oracles/
```

Expected: `snowball_it.json` only.

- [x] **Step 3: Commit as three concerns, in the order they were done**

```bash
git commit -m "Add the Italian Snowball stemmer"
git commit -m "Express the Romance step 1 as a rule table"
git commit -m "Suppress S3267 in the shared Romance worker"
```
