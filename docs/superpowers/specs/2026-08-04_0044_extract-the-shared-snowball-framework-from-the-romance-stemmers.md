# Design — #44: extract the Snowball framework shared by the Romance stemmers

**Date:** 2026-08-04 · **Issue:** #44 · **Branch:** `refactor/44-shared-romance-framework` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

The quality gate fails on duplication, and each language makes it worse:

| PR | Duplication | Limit |
| --- | --- | --- |
| #42 Spanish | 3.79 % | ≤ 3.0 % |
| #43 Portuguese | **28 %** | ≤ 3.0 % |

This is not a measurement artefact. Three Romance stemmers genuinely repeat the
same machinery, because they implement the same published framework: `Region`, the
RV rule, `InRv`/`InR1`/`InR2`, `Ends`, `Delete`, `Replace`, `LongestSuffix`,
`LongestSuffixInRv`.

Italian (#4) is a fourth with the same structure, so leaving this takes duplication
higher again.

## The tension this has to resolve rather than ignore

The project's stated reason for keeping each stemmer self-contained is that a
faithful, readable one-to-one mapping with the published algorithm is what makes
divergences auditable — the same argument used to suppress `S3776` on these files.
Extracting shared code appears to cut against it.

**The resolution is that the shared part is not the algorithm.** `Region`, RV,
`Ends`, `Delete` are the Snowball *scaffolding*, identical across languages by
construction. Which suffixes are stripped, in what order, under which region
condition — the part where a faithful reading against the published description
actually matters — stays in each language's own file, untouched.

That distinction is the design. Get it wrong in either direction and the project
loses something real: readability, or the gate.

## Decisions

### D1 — An internal base holding the scaffolding; each language supplies its vowel set and its steps

`RomanceSnowballWorker`. Nothing about *which suffix* or *in what order* moves
into it.

### D2 — French deliberately stays out

Its RV rule carries the `par`/`col`/`tap` prefix cases and is **not** the shared
one. Forcing it into the base is the tempting mistake: it would make the base
carry a language-specific branch, which is precisely how a shared framework
becomes a place where every language's exceptions accumulate.

### D3 — Portuguese passes its nasal expansion *through* the base constructor

The regions must see the **already-transformed** word. Get that wrong and
`geração` stems differently — a silent, word-shaped wrong answer.

This is the concrete constraint the base's shape has to accommodate, and it is why
the base takes the string rather than computing the transformation itself.

### D4 — The corpora are the safety net, and the bar is byte-identical

`snowball_es.json`, `snowball_pt.json`, `snowball_fr.json` and `snowball_en.json`
must all replay **unchanged**. A refactor of oracle-validated code is only
acceptable if it is provably inert.

### D5 — Take the `S1871` finding in Portuguese step 5 while here

Two identical branches: the published description lists `gu` and `ci` separately,
but both drop the same single character. Merging them is a genuine simplification
and belongs with this change rather than in a separate pass over the same file.

## A known consequence, recorded because it will bite

**Moving code moves the code, not its suppressions.** `Delete` and `Replace` carry
a `CA1845` suppression in the four language files; extracting them leaves the
`#pragma` behind and the rule reappears against the new file. Nothing enforces
this.

## Out of scope

- Italian (#4) — it is built on the base from the start, in its own branch.
- French.

## What "done" means

The framework extracted; Spanish and Portuguese refactored onto it; **all four
corpora byte-identical**; duplication back under the gate threshold.
