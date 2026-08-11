# Design — #5: the German Snowball stemmer

**Date:** 2026-08-04 · **Issue:** #5 · **Branch:** `feat/5-german-snowball-stemmer` ·
**Checkout:** `<repo>`

## Problem

Sixth and last language of the #2–#5 batch, and the first non-Romance one. Every
abstraction built so far was extracted from Romance languages and validated
against Romance corpora. German is what tells us whether `RomanceSnowballWorker`
was named honestly or whether "Romance" was doing no work in that name.

The answer is that the name is honest, and that is a problem to solve before
writing a line of German.

## Where the repository stands

Italian merged at 23:17. `RomanceSnowballWorker` holds the region machinery, the
suffix primitives and the rule table introduced in #4, and drives French, Spanish,
Portuguese and Italian. Duplication across the stemming folder has gone from
roughly 12 % to 2.7 % over #44, #47 and #48.

## Decisions

### D1 — Split the worker before writing German, not after

German shares almost nothing with the Romance algorithms:

- **No RV region at all.** RV is the axis half of `RomanceSnowballWorker` is built
  on.
- **R1 is floored** so that the region before it holds at least three letters — a
  constraint the Romance languages do not have.
- Several conditions turn on the **preceding letter**, not on a region.

So `RomanceSnowballWorker` splits in two:

| | holds |
| --- | --- |
| `SnowballWorkerBase` | R1/R2, the suffix primitives, the rule table — language-neutral |
| `RomanceSnowballWorker` | RV and everything built on it (`InRv`, `LongestSuffixInRv`, `StripAmente`) |

German derives from the base.

**Without the split, German re-copies the primitives** and pushes duplication
straight back up — undoing exactly what #44, #47 and #48 achieved. Doing it
afterwards means writing the duplication first and trusting a later branch to
remove it, which is how the three copies in #2 happened.

### D2 — The split must be proven inert before German exists

All four Romance corpora replay unchanged, as a separate step, before any German
code is written. Moving members between a base and a derived class is the kind of
change that looks obviously safe and occasionally is not — a region helper that
silently reads a field initialised in the wrong constructor produces subtly
different regions, and only the corpora would notice.

### D3 — Three conditions that are easy to state and easy to get subtly wrong

These are the parts of the published description that a careful reader still gets
wrong, so they are named here and tested directly:

- A bare final `s` is removed **only after a valid s-ending**, and that letter
  **need not itself be in R1**. Attaching the region condition to the wrong
  character is the natural misreading.
- `st` needs a valid st-ending with **at least three letters before it** — which is
  precisely what keeps `ist` intact.
- `ig`, `ik` and `isch` are **never** removed straight after an `e`.

### D4 — The rest of the algorithm, unremarkable but ordered

`ß` → `ss` and `u`/`y` between vowels upper-cased on entry; R1 and R2 with R1
floored at 3; step 1 (`em ern er`, `e en es`, `s`), step 2 (`en er est`, `st`),
step 3 for derived suffixes (`end ung ig ik isch lich heit keit`); then strip the
umlaut from `a o u` and lower-case `U`/`Y` again.

### D5 — Same oracle discipline as the other five

A frozen corpus from `nltk.stem.snowball.SnowballStemmer("german")`, replayed by
the suite. If German passes on the first run, that is a fact worth recording
rather than a reason to shorten the corpus: the Romance languages each hid a
divergence that only the corpus found.

## Out of scope

- German stop words.
- Any further refactor of the base once the split is done. It exists to let German
  in, not to be improved speculatively.

## What "done" means

`snowball_de.json` replayed at 100 %; all four Romance corpora unchanged by the
split; the German row added to `equivalence.md`; Snowball coverage reaching six
languages and 758 frozen reference words.
