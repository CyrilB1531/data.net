# 0075 — Double Metaphone ships, with `doublemetaphone` 1.2 as its oracle

**Status:** accepted · **Date:** 2026-09-02

## Context

[#314](https://github.com/CyrilB1531/lodestar/issues/314) exists because a survey claim turned
out to be false. `jellyfish` 1.2.1 — this repository's oracle for the other encoders — exports

```text
damerau_levenshtein_distance  hamming_distance  jaccard_similarity  jaro_similarity
jaro_winkler_similarity  levenshtein_distance  match_rating_codex  match_rating_comparison
metaphone  nysiis  soundex
```

and **no Double Metaphone**. It was in older releases and is not in this one, so "jellyfish has
it and we do not" was never the gap.

That turns an implementation lot into a decision. Reproducing Double Metaphone means adding a
development dependency to `tools/generate_oracles.py`, and every oracle library here is pinned by
hash: widening that surface changes how conformance is proven, not just what is convenient.
[Decision 0036](0036-a-member-may-ship-without-an-oracle-if-it-says-so.md) offers a third way —
ship without an oracle and say so — which had to be beaten rather than ignored.

**Read and measured on 2026-09-02.** Every figure below is what the named artefact produced that
day.

## The fear, and the measurement that removes it

Double Metaphone has no normative specification, only Lawrence Philips' original C++ and a crowd
of ports. The real risk was therefore not *"which library"* but *"which dialect"* — if the
candidates disagree, then picking one is picking a behaviour, and the corpus would freeze an
accident.

So they were compared against each other, over the **401 distinct words already in
`tests/oracles/phonetics.json`** — the repository's own phonetics corpus, real names and
generated strings both, rather than a word list chosen for this question.

One convention has to be normalised first: where a word has no alternate encoding, `metaphone`
and `phonetics` return `''` for the secondary while `doublemetaphone` repeats the primary. That is
presentation, not phonetics.

| comparison | primary code | both codes, normalised |
| --- | ---: | ---: |
| `metaphone` 0.6 vs `doublemetaphone` 1.2 | **0 / 401** | 16 / 401 |
| `phonetics` 1.0.5 vs `doublemetaphone` 1.2 | 3 / 401 | 4 / 401 |
| `metaphone` 0.6 vs `phonetics` 1.0.5 | 3 / 401 | 20 / 401 |

**The dialect fear does not survive.** Two independently written permissive implementations
produce the *same primary code on all 401 words*, and the remaining differences are defects with a
side rather than disagreements about the algorithm:

- **`metaphone` 0.6 emits a trailing space** in the secondary code of 16 words —
  `doublemetaphone("Cj")` is `('KJ', 'K')` where `metaphone` gives `('KJ', 'K ')`. Strip
  whitespace and the two agree on **401 / 401, both codes**.
- **`phonetics` 1.0.5 drops characters.** `Dmtpbf` → `TMTP` where both others give `TMTPF`;
  `lnvppqfv` → `LNFPFF` against `LNFPKFF`; `qappjvg` → `KPFK` against `KPJFK`. It also diverges on
  one secondary, `wpbczgtr` → `PTSKTR` against `PXKTR`.

A library that loses a character from a phonetic key is not a candidate for freezing behaviour.

## The candidates, and why the field narrows to one

| package | version | licence | last upload |
| --- | --- | --- | ---: |
| **`doublemetaphone`** | **1.2** | **Artistic 2.0** | **2025-05-11** |
| `abydos` | 0.5.0 | **GPLv3+** | 2020-01-11 |
| `metaphone` | 0.6 | BSD | 2016-08-24 |
| `fuzzy` | 1.2.2 | Artistic / MIT | 2017-10-16 |
| `phonetics` | 1.0.5 | MIT | 2018-03-23 |

`abydos` is **excluded on licence**. [ADR 0003](0003-provenance-and-licensing.md) refuses copyleft
even for generating test data — *"neither transcribed nor even used to generate test data (for
hygiene, though the GPL claims nothing over a program's outputs)"* — and that rule is what removed
`python-Levenshtein`. It applies here unchanged.

`doublemetaphone` 1.2 carries the **Artistic License 2.0**, verified from the `LICENSE` file in
its own wheel rather than from the PyPI classifier, which says only "Artistic License". 2.0 is a
free, GPL-compatible, non-copyleft licence — it is not the vague 1.0, and ADR 0003's hygiene rule
does not reach it. The posture is the one `rapidfuzz` and `jellyfish` already have: a development
dependency, run to produce JSON, never redistributed and never transcribed.

The remaining three are permissive and agree well enough to be usable, so the tiebreaker is
maintenance. `doublemetaphone` shipped **2025-05-11**; the newest of the others is **2018**. A
frozen corpus outlives the session that made it, and an oracle nobody has touched in eight years
is one nobody will fix.

It is also installable under this repository's own constraint: CI installs with
`--only-binary :all: --require-hashes`, and 1.2 publishes 89 wheels including
`cp312-manylinux_2_17_x86_64` — the interpreter and platform the oracle job runs on.

## Decision

**Double Metaphone ships**, and **`doublemetaphone` 1.2 is its oracle**, pinned by hash in
`tools/requirements.lock.txt` like every other oracle library.

The encoder then follows lot 1's shape — the stemmer and phonetics lots' shape — with the gates
[#313](https://github.com/CyrilB1531/lodestar/issues/313) names: a frozen corpus compared exactly
for strings, an entry in `docs/equivalence.md` in the same commit as the function, a reference page
under `docs/reference/text/phonetics/`, and a member reference from `samples/Lodestar.Sample`.

**The secondary code is normalised on the way in.** The corpus stores `''` for "no alternate",
which is the convention `jellyfish`, `metaphone` and `phonetics` share and the one the C# API will
expose; `doublemetaphone`'s repeated primary is unwrapped by the generator. That is a presentation
choice made once, in one place, and recorded here so the corpus is not read as disagreeing with
its own source.

## What was rejected

**Decision 0036's third option — ship without an oracle, say so, and name what would retire the
exception.** It is the right instrument when no reference exists, and that was the premise here
until the comparison ran. Two permissive implementations agreeing on 401 of 401 primaries is a
reference; declining to use it would be spending 0036's exception on a case that does not need it,
and 0036 is worth less every time it is spent loosely.

**`metaphone` 0.6, on the strength of agreeing with the winner.** It does agree — exactly, once
its trailing space is stripped — but that strip would be a workaround in the generator for a
defect upstream, and the package has not shipped since 2016.

**Dropping Double Metaphone and pointing callers at `docs/migration/`.** It is the encoder people
ask for by name, and the reason to drop it had been "no oracle exists". One does.

## Consequences

- `tools/requirements.lock.txt` gains `doublemetaphone==1.2` with its hashes, and
  `THIRD-PARTY-NOTICES.md` gains its attribution — ADR 0003 requires the notice to move at the
  same time as the dependency.
- The oracle surface grows by one library. That is the cost this decision is really about, and the
  measurement above is what pays for it.
- The corpus records `doublemetaphone` 1.2 in its metadata, so a future divergence is traceable to
  a version rather than to "the reference".
- If a later release of `jellyfish` restores Double Metaphone, this decision is worth revisiting:
  one fewer library would be strictly better, and the 401-word comparison above is the test that
  would say whether the two agree.
