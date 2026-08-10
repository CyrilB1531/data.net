# Design — #13: stop-word lists beyond English

**Date:** 2026-08-05 · **Issue:** #13 · **Branch:** `feat/13-multilingual-stop-words` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

`StopWords.English` ships scikit-learn's 318-word list. Six languages now have a
Snowball stemmer, and five of them have no stop-word list, so a French or German
user of `CountVectorizer` has to supply their own.

This looks like a data-entry task. It is a **licensing** task, and getting the
order wrong is how a redistribution problem ends up compiled into a shipped
assembly.

## Order of work: licence first, decision second, lists last

The three boxes of the issue, deliberately in that order. Any other order means
choosing a source and then looking for permission to use it.

## Decisions

### D1 — The obvious source is the one that cannot be used

`nltk.corpus.stopwords` is the natural candidate: it is the parity reference for
the stemmers. It is also unusable.

`nltk_data` classifies its `stopwords` package under **"Unclarified, Unknown,
Ambiguous, or Citation-Only"** — no `license` attribute in `index.xml`, and
`LICENSE-OVERVIEW.md` states the repository-wide Apache-2.0 governs *the
repository*, not the individual data packages. The Apache-2.0 recorded for `nltk`
in `THIRD-PARTY-NOTICES.md` covers the code executed to generate oracles; it does
not extend to a corpus that would be **redistributed inside our assembly**.

That distinction is the whole finding: running someone's code at development time
and shipping their data are different acts with different permissions.

### D2 — Snowball is both the clean source and the original one

Snowball publishes the same lists upstream under BSD-3-Clause (© 2001 Dr Martin
Porter, © 2002 Richard Boulton). PostgreSQL took its copy from there, and `nltk`
took its own from PostgreSQL.

So the licensed source is also upstream of the unlicensed one. That is a good
outcome and it is worth stating, because it means the divergence in D4 is not a
compromise.

### D3 — `StopWords.English` is not touched

Snowball's English list has 174 words; scikit-learn's has 318. `stop_words="english"`
is what a migrating user wrote in Python, and matching it is the point of this
library. Swapping in Snowball's list would break that parity to gain consistency
with five lists nobody is migrating from.

### D4 — The divergence from nltk is owned out loud

For French, German, Portuguese and Spanish this library **deliberately does not
match nltk** — the opposite of the call made in ADR 0008 for the Italian stemmer,
which is exactly why it needs saying rather than assuming a reader will infer the
rule.

| Language | Here (Snowball) | `nltk` | Only here | Only in `nltk` |
| --- | ---: | ---: | ---: | ---: |
| French | 154 | 157 | 13 | 16 |
| German | 231 | 232 | 4 | 5 |
| Italian | 279 | 279 | 0 | 0 |
| Portuguese | 203 | 207 | 0 | 4 |
| Spanish | 308 | 313 | 2 | 7 |

Recorded in three places a reader might land: `docs/equivalence.md`, the
vectorization guide, and the XML doc on `StopWords` itself.

A caller who needs exactly nltk's behaviour still has a clean route —
`StopWords` accepts any `IReadOnlyCollection<string>`, so the corpus can be
supplied at the call site.

### D5 — The lists are generated and verified, never typed

`tools/fetch_stopwords.py` downloads the five files, checks each against a
**pinned SHA-256**, and emits `StopWords.Snowball.cs`. A CI job replays it with
`--check`.

Two reasons, both concrete: 1,175 words typed by hand is an unreviewable diff, and
a silent upstream edit would otherwise change the shipped library with nobody
noticing. A hash mismatch is a decision to record — read the diff, update the pin,
adjust the counts in the tests — not something to regenerate quietly.

### D6 — This is shipped source, not a test fixture

The lists are compiled into the assembly, so their provenance is a *shipping
guarantee*. `NOTICE` and `THIRD-PARTY-NOTICES.md` gain the Snowball attribution,
and the verification job is separate from the oracle jobs for that reason.

## Out of scope

- Any stop-word list for a language without a stemmer here.
- Changing `StopWords.English`.

## What "done" means

Five new lists generated from a pinned source; ADR 0010 recording the licence
finding; the divergence table in `equivalence.md`, the guide and the XML doc;
`fetch_stopwords.py --check` green in CI; attribution in `NOTICE` and
`THIRD-PARTY-NOTICES.md`.
