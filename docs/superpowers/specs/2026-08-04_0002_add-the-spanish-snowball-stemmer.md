# Design — #2: the Spanish Snowball stemmer

**Date:** 2026-08-04 · **Issue:** #2 · **Branch:** `feat/2-spanish-snowball-stemmer` ·
**Checkout:** `<repo>`

## Problem

`DataNet.Text` ships three stemmers: Porter, Snowball English and Snowball
French. Spanish is the largest missing language by speaker count and the natural
next one — but it is also the first Romance language after French, which means it
is the first that will show whether the existing shape generalises or whether
each language is going to be its own island.

## Constraints this change inherits

- **ADR 0003, the provenance rule.** This must be an *original implementation of
  the published Snowball algorithm*, never a transcription of nltk's Python or of
  anyone's C#. The published description is a specification; the code is not a
  source.
- **Parity is proven, not asserted.** A frozen corpus from
  `nltk.stem.snowball.SnowballStemmer("spanish")` is replayed by the test suite.
  A stemmer with no oracle is not done.
- Both `net10.0` and `netstandard2.0` must build, warnings-as-errors.

## Decisions

### D1 — Standalone implementation, no premature framework

English and French each carry their own region machinery. Spanish will make three
copies of it, and that is accepted **for this branch**. Extracting a shared
Romance framework is a refactor with its own risk profile — it must be done with
at least two Romance corpora already green so the extraction can be proven inert.
Doing it here would mean changing the reference and the abstraction in one diff.

This is a deliberate debt, and it is written down so the follow-up is opened
rather than remembered.

### D2 — Region model: RV, R1, R2, and steps 0, 1, 2a, 2b, 3

The published algorithm's own structure, kept intact. Deviating from its step
numbering would make the source unreadable against the specification it claims to
implement.

### D3 — Step 0 runs first and removes attached object pronouns

Spanish attaches object pronouns to the verb (`dámelo`, `haciéndola`). They must
be removed **before any suffix stripping**, or step 1 matches against a word that
does not exist as a lexical form.

Two sub-cases matter and are easy to conflate:

- After `iéndo`, `ándo`, `ár`, `ér`, `ír` the deletion is followed by **dropping
  the accent**, so later steps still match the unaccented form.
- After `yendo` the rule applies **only when the stem ends in `uyendo`** — that
  is, when a `u` precedes. This is a condition on the *word ending*, not on a
  fixed character offset, and reading it the second way is the single most likely
  bug in the whole file. It is called out here so the plan can test it directly.

### D4 — Accents are stripped only at the very end

Unlike the eventual Portuguese behaviour, Spanish finishes by removing acute
accents. It must be *last*: every earlier step is defined over the accented form,
so stripping early silently changes which suffixes match.

### D5 — Step 1 matches the longest suffix across all groups at once

Not group by group. The groups overlap — `amente` must win over `mente`, `idades`
over `idad` — so a per-group scan returns the wrong answer depending on group
order, and the order is not part of the specification.

### D6 — CA1845 is suppressed with a reason, as in English and French

The span-based `string.Concat` overload is net-only; the `Substring` form is what
makes the file compile for `netstandard2.0`. Same suppression, same wording, same
justification as the two existing stemmers — a reader comparing them should find
them identical.

## Out of scope

- Extracting the shared Romance framework (the follow-up D1 creates).
- Portuguese, Italian, German — one language per branch, one issue per PR.
- Spanish stop words. Stemming and stop-word lists are separate concerns with
  separate provenance questions.

## What "done" means

`snowball_es.json` committed and replayed at 100 %, no existing oracle moved,
both frameworks green, `dotnet format` clean.
