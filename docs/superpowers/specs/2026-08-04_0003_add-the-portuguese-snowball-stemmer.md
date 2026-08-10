# Design — #3: the Portuguese Snowball stemmer

**Date:** 2026-08-04 · **Issue:** #3 · **Branch:** `feat/3-portuguese-snowball-stemmer` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

Fourth Snowball language, second Romance one after Spanish (#2, merged). Portuguese
looks close enough to Spanish that the temptation is to copy that file and edit
the suffix tables. Two properties make that wrong, and they are the reason this
issue is worth a design note rather than a checkbox.

## Where the repository stands

Spanish merged at 22:38 today. `EnglishSnowballStemmer`, `FrenchSnowballStemmer`
and `SpanishSnowballStemmer` each carry **their own copy** of the region
machinery — the debt #2 recorded deliberately. The extraction issue (#44) is open
and is *not* this branch.

`tools/generate_oracles.py` gained a shared corpus helper in #2. This branch was
started before #2 merged, which creates a specific hazard covered in D4.

## Decisions

### D1 — Nasal vowels are expanded for the duration of the run

`ã` → `a~` and `õ` → `o~` on entry, restored on exit.

The point is not orthography, it is region computation: the tilde must behave as a
**consonant** so it cannot let the vowel before it close a region early. Left as a
single character, `ã` counts as a vowel and R1/R2 land in the wrong place.

The visible consequence, and the one to test: `geração` stems to `geraçã`, not
`geraç`. The trailing `o` goes later, as a residual suffix — not as part of the
nasal.

### D2 — Acute accents are kept

Portuguese has **no final accent-stripping step**. This is a direct contradiction
of the Spanish stemmer, whose last act is to remove them, and it is the single
most likely thing to be carried over by mistake if the file is written by copying.

Test: `país` stems to itself.

### D3 — `ança` / `anças` belong to the delete-if-in-R2 group

Easy to omit because it looks like it should fall out of a more general rule. It
does not: without it, `esperança` falls through to the residual step and the
`ç` → `c` rule, giving `esperanc` instead of `esper`. Nothing about that output
looks wrong enough to notice by eye — only the corpus catches it.

### D4 — Rebase onto merged `main`, do not merge into it

This branch predates #2. `tools/generate_oracles.py` gained the shared
`_snowball_corpus` helper there; a straight merge produces **two definitions of
the same function**, which Python accepts silently — the second simply wins.

So: rebase onto current `main`, reuse the helper, and verify by counting that
exactly one definition exists in the file. This is a correctness check, not
tidiness.

### D5 — Step 2 must fall through to the next-longest candidate

When the longest matching suffix lies outside RV, the search does **not** stop —
it continues to the next-longest. `amáveis` needs `eis`; `áveis` also ends the
word but is outside RV, and abandoning the search there leaves the word unstemmed.

This is a property of the search, not of the suffix table, so no amount of table
editing fixes it.

## Out of scope

- Extracting the shared Romance framework — #44, still open, still deliberately
  separate.
- Portuguese stop words.

## What "done" means

`snowball_pt.json` committed and replayed at 100 %; `snowball_es.json` reproduces
**byte-identically** from `main` after regeneration; both frameworks green.
