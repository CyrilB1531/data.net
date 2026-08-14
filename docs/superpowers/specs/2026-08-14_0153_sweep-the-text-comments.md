# 0153 — Sweep DataNet.Text's comments, in a package that already has its ADRs

**Issue:** [#153](https://github.com/CyrilB1531/data.net/issues/153) · **Date:** 2026-08-14 ·
**Branch:** `docs/153-sweep-text-comments` · **Part of:** [#134](https://github.com/CyrilB1531/data.net/issues/134)

## Context

Third zone of #134's sweep, after `DataNet.Metrics` (#151) and `DataNet.Embeddings` (#152). Both counters
were corrected while those two ran, so the issue's own numbers are superseded:

| | issue | measured on `main` at `b81eac5` |
| --- | ---: | ---: |
| blocks past their budget | 70 | **50** |
| blocks naming a reference library | 76 lines | **62 blocks** |
| of those, citing something a reader can open | 2 | **4 (6%)** |

The twenty that disappeared are the reason above a `#pragma`, which #151's branch stopped counting: a
suppression is *required* to carry one, and `CLAUDE.md` demands a reason "a reviewer cannot disagree with",
which is a stricter test than brevity. Twelve of those twenty were in `Stemming/`. Predicting that drop, and
waiting for it, is why this lot did not start earlier.

The zone is **flat**: fifty blocks over six areas, and the worst file holds five.

| area | blocks | area | blocks |
| --- | ---: | --- | ---: |
| `Vectorization/` | 15 | `Persistence/` | 7 |
| `Distances/` | 15 | `Phonetics/` | 4 |
| `Stemming/` | 8 | `Text/` | 1 |

## Decisions

### D1 — the work is split by area, because the concentration that ordered #152 is not there

Issue #152 followed two files holding 37% of its zone. Here the worst file holds 5 of 50. Areas are the unit, and
they group by what a claim would be checked against — a distance against rapidfuzz or textdistance, a
stemmer against nltk, a vectorizer against scikit-learn — which is what makes a task's verification one
setup rather than six.

### D2 — the cheap tier here is an ADR, not a corpus

`docs/decisions/0004`-`0008` are all `DataNet.Text`: the Myers backlog, the Hamming divergence from
jellyfish, Ratcliff's `autojunk`, Metaphone's scope, and the Italian `-enza` divergence from nltk. A block
that outgrows its budget in this package usually has a decision to point at rather than one to write.

**The pointer is checked before it is written.** #152's fourth task found a suppression citing decision 0013
for a claim 0013 never makes; a citation nobody verified is worse than none, because it looks settled.

### D3 — the five references are all available, and one of them only imports from elsewhere

rapidfuzz 3.14.5, jellyfish, textdistance 4.6.2, difflib in the standard library, and nltk 3.10.1 are all in
`.venv-oracles`, so tier 2 — run it once and cite the output — is practicable for every area.

**nltk imports only from a neutral working directory.** Checked while writing this spec: it fails from the
repository root and succeeds from `/tmp`, which is the trap `CLAUDE.md` documents for the oracle generator.
Any verification against nltk — the stemmers and the Snowball stop-word lists — runs from `/tmp`.

### D4 — in `Stemming/` and `Phonetics/`, a comment is provenance evidence

[ADR 0003](../../decisions/0003-provenance-and-licensing.md) is the repository's oldest rule: the stemmers
and phonetic encoders are original implementations written from the **published algorithm description**,
never transcribed from a GPL reference. A comment explaining where a rule comes from is what shows that.

So in those two areas a block **shortens but does not disappear**, and the citation of choice is the
published description — the Snowball page for a stemmer, the original paper for an encoder — rather than a
reference implementation. A sweep that replaced "step 2b of the published algorithm" with "matches nltk"
would trade a provenance record for a claim about a library, which is the wrong direction twice over.

### D5 — where displaced prose goes, and no ADR is written

`docs/equivalence.md` for what says reproduced-or-refused with its divergence, and
`docs/guides/migrating-from-rapidfuzz.md` or `docs/guides/vectorization.md` for what answers a user's
question. Not `embeddings.md`, which is the other package's.

**No ADR is written and no number is taken**, as in #152: this lot moves findings, not decisions. A block
holding a real undocumented divergence becomes an issue — that is how #160 came out of the last zone.

### D6 — one fact, one home

Before moving a fact, grep `docs/equivalence.md`, the guides and `docs/decisions/` for it already being
there, and cite what exists. #156 audits these same documents for the paragraph that lives twice and gets
corrected once.

### D7 — the evidence is the pointers, and no behaviour changes

The counter printing nothing for `src/DataNet.Text/` is the issue's "done when" and proves only that the
prose got shorter. What proves nothing was destroyed is the list of every fact that moved with its new home,
and every block cut as an opinion, named individually so a reviewer can disagree with each.

**3 147 tests pass, unchanged**, and no byte of `tests/oracles/` moves.

## Documentation

`docs/equivalence.md`, `docs/guides/migrating-from-rapidfuzz.md`, `docs/guides/vectorization.md`. No ADR.

## Out of scope

`src/DataNet.Metrics` (#151, merged) and `src/DataNet.Embeddings` (#152, merged). `tests/`, `bench/`,
`samples/` and `tools/`, which are their own zones. The prose documents themselves (#156). The suppression
reasons this package carries, which the counter now exempts by decision of #151 — twelve of them in
`Stemming/` alone, and reopening that decision is not this lot's business.

## Risks

- **Cutting a provenance record.** D4 is the mitigation and the risk is specific to two areas. When in doubt
  in `Stemming/` or `Phonetics/`, the block shortens and keeps its reference to the published description.
- **Citing an ADR that does not say it.** Measured once already in #152. Every pointer written here is
  opened and read first.
- **A claim that was true of an older design.** This is the oldest package in the repository, and #63, #95,
  #97 and #100 have all edited it. A claim found false is fixed and named, not reformatted.
