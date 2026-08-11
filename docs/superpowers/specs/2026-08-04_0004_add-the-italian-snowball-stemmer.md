# Design — #4: the Italian Snowball stemmer

**Date:** 2026-08-04 · **Issue:** #4 · **Branch:** `feat/4-italian-snowball-stemmer` ·
**Checkout:** `<repo>`

## Problem

Fifth Snowball language, third Romance one. Unlike #2 and #3, this one starts
**after** the shared framework exists, so it is the first real test of whether
that framework was extracted along the right seam.

## Where the repository stands

- #2 (Spanish, 22:38), #3 (Portuguese, 22:44), #44 (extraction, 22:51) and #47
  (a CA1845 suppression lost in the extraction, 23:01) are merged.
- `RomanceSnowballWorker` now holds the region machinery that French, Spanish and
  Portuguese used to carry three times over.
- **`equivalence.md` has no Spanish or Portuguese rows.** #42 and #43 both omitted
  them, which is a documented repository rule broken twice in a row.

## Decisions

### D1 — Italian is built on `RomanceSnowballWorker` from the start

It contributes its steps and its vowel set; it does not bring another copy of the
region code. If something in the framework does not fit Italian, the framework is
what changes — that is what #44 was for, and a fourth private copy would answer
the question the wrong way.

### D2 — Acute accents fold to grave, first

`perché` and `perchè` must stem alike. The fold happens before anything else, so
every later step sees one form.

### D3 — `u` after `q`, and `u`/`i` between vowels, are upper-cased

So the regions treat them as **consonants**. Lower-cased again at the end. Same
device as Portuguese's nasal expansion in #3 — a temporary re-spelling whose only
purpose is to make region computation correct — and worth recognising as the same
device rather than as an Italian curiosity.

### D4 — Step 0 restores an `e` after an infinitive

`mandarci` → `mandare`, not `mandar`. This is where Italian and Spanish diverge
most visibly: Spanish step 0 **deletes** the attached pronoun outright, Italian
deletes it and puts the infinitive's `e` back. Copying the Spanish step 0 gives
plausible output and wrong stems.

### D5 — Where the published description and nltk disagree, **nltk wins**

The published description gives, for step 2:

> `enza enze` → replace with `ente` if in R2

nltk replaces with `te`, not `ente`. So `esistenza` → `esist` + `te` = `esistte`,
which step 3a trims to `esistt` — where the spec reading gives `esistent`.

**The corpora are frozen from nltk and `equivalence.md` names it as the
reference**, so the implementation follows nltk. This is the same call already
made for jellyfish in ADR 0005, and it gets its own record: **ADR 0008**.

Two things about this divergence are worth stating because they generalise:

- It hides narrowly. The mismatch only appears when the suffix falls **inside
  R2**. `potenza`, `pazienza`, `partenza` and `presenza` all fail that condition,
  fall through to step 3a, and agree under either reading. **A smaller corpus
  would have passed and shipped the wrong rule.**
- When two of 96 cases fail, the corpus says *something* is wrong but not what.
  Reading nltk's source to find out is diagnosis, and is permitted under ADR 0003;
  deriving the implementation from it is not.

### D6 — Express the Romance step 1 as a rule table

Three languages now write the same shape — suffix, condition, replacement — as
three hand-rolled `if` chains. Italian would be the fourth. A table in
`RomanceSnowballWorker`, driven by data, is what makes the four readable against
their published descriptions.

This lands **in this branch** rather than as a follow-up, because writing the
fourth chain and then removing all four is more churn than doing it once. It is
verified inert: the Spanish and Portuguese corpora must replay unchanged after the
conversion.

### D7 — Fix the `equivalence.md` omission here

The Spanish and Portuguese rows that #42 and #43 forgot are added in this branch.
The rule is that a row lands with the function, not at the end of the project; two
consecutive misses means the rule needs restating in `CONTRIBUTING.md`, not just
applying.

## Out of scope

- German (#5). It shares almost nothing with the Romance algorithms and needs the
  base split first.
- Italian stop words.

## What "done" means

`snowball_it.json` replayed at 100 %; Spanish, Portuguese and French corpora
unchanged after the rule-table conversion; ADR 0008 written; `equivalence.md`
carrying Spanish, Portuguese and Italian.
