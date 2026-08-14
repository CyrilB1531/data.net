# 0152 — Sweep DataNet.Embeddings' comments against the rule that now counts them

**Issue:** [#152](https://github.com/CyrilB1531/data.net/issues/152) · **Date:** 2026-08-14 ·
**Branch:** `docs/152-sweep-embeddings-comments` · **Part of:** [#134](https://github.com/CyrilB1531/data.net/issues/134)

## Context

`tools/check_comment_length.py` shipped with #150 and counts what `CONTRIBUTING.md`'s *Claims in comments*
now states: two lines for an inline comment, eight lines of prose for XML documentation, and a
`long-comment:` marker with a reason past either. On `main` at `b8d8109` it reports **124 over-budget blocks
in `src/DataNet.Embeddings`**, the largest zone in the tree.

The distribution decides the order of work: two files carry 37% of them.

| file | blocks | file | blocks |
| --- | ---: | --- | ---: |
| `Persistence/TokenizerJsonLoader.cs` | **26** | `Persistence/SentencePieceModelLoader.cs` | 5 |
| `Tokenization/BpeTokenizer.cs` | **20** | `Tokenization/BpeVocabulary.cs` | 4 |
| `Tokenization/SentencePieceTokenizer.cs` | 8 | `Tokenization/AddedToken.cs` | 4 |
| `Tokenization/BpePreTokenizer.cs` | 8 | `Persistence/VocabTxtLoader.cs` | 4 |
| `Tokenization/WordPieceTokenizer.cs` | 7 | `Tokenization/EncodingOptions.cs` | 3 |
| `Persistence/BpeFilesLoader.cs` | 6 | seventeen files | 1-2 each |
| `Search/EmbeddingIndex.Persistence.cs` | 5 | | |

The issue also counts the claims: **93 comment lines naming a reference library, 10 of them citing
anything that would check them.** A separate count run for this spec, with a different pattern, found
129 and 12 — the two disagree on the population, not on the ratio, which is about one in ten either way.
The issue's numbers are the ones this lot reports against, and the discrepancy is recorded here rather than
resolved, because nothing in the work depends on it.

### The finding that shapes the lot

The first block read was `TokenizerJsonLoader.cs:8`, the longest in the tree at 61 lines. It states, among
other things, that a stock HuggingFace BERT `tokenizer.json` is **refused**, and that `VocabTxtLoader` is
the route instead.

**That fact is written nowhere else.** `BertPreTokenizer` does not appear in `docs/equivalence.md`, in any
of the 23 ADRs, or in any guide. Cutting the block to its budget without relocating it would delete the only
record of a user-facing refusal.

So the sweep is not mostly cutting. For this zone the comments **are** the record, and the work is moving
what deserves to survive somewhere a reader would look, then citing it from one line.

## Decisions

### D1 — the zone is all 124 blocks, in descending order of concentration

`BpeTokenizer.cs` is included: [#149](https://github.com/CyrilB1531/data.net/issues/149) merged, so nothing
is in flight against it. Work descends the table above, so that the two files carrying 37% of the blocks are
done while the triage is freshest, not last.

### D2 — three outcomes per block, and the triage is the work

Straight from the issue, and not to be softened:

1. **A corpus already answers the claim** — cite the file and the case. `tests/oracles/` is thick here, and
   this should be the common outcome.
2. **It is executable but nothing frozen answers it** — run it once and cite the output, or add the corpus
   case and cite that where the answer deserves freezing.
3. **Nothing reasonable checks it** — then it is an opinion wearing a measurement's clothes: cut it, or
   rewrite it as the opinion it is. A comment that cannot be checked is not thereby exempt; it is thereby
   not a claim.

### D3 — what outgrows the budget moves to the guide or to `equivalence.md`, and to **no** ADR

- **`docs/guides/embeddings.md`** takes what answers *"will this load my file?"* — the BERT refusal above is
  the type case.
- **`docs/equivalence.md`** takes what says *reproduced or refused, and how it diverges*. The file exists
  for exactly that and its rows are already the shape.
- **No ADR is written by this lot.** An ADR records a choice with a loser; this lot moves findings, not
  decisions. Writing one without a rejected alternative would be a document lying about its own nature.
  If a block turns out to hold a real decision that no ADR records, that is a finding to report, not a
  document to improvise here.

### D4 — one fact, one home, because #156 reads these same files

[#156](https://github.com/CyrilB1531/data.net/issues/156) audits the prose documents for exactly the failure
this lot could cause: a paragraph in two places, corrected in one. Three of the eight false claims found on
2026-08-13 arose that way.

So before a fact is moved into the guide or `equivalence.md`, **it is checked for already being there**, and
what is already there is cited rather than restated. This costs one grep per moved fact and is the whole
mitigation.

### D5 — the evidence is the pointers, not the line count

`python3 tools/check_comment_length.py | grep '^src/DataNet.Embeddings/'` printing nothing is the issue's
"done when", and it proves only that the prose got shorter. What proves nothing was destroyed is that
**every shortened block that lost a fact keeps a line naming where the fact went** — so the lot is reviewed
by following pointers, not by reading deletions.

A block whose prose was cut because it was tier 3 keeps no pointer, deliberately: there is nowhere for an
opinion to go. Those are the ones a reviewer should be given by name.

### D6 — no behaviour changes

Comments and documents only. **3 147 tests pass, unchanged**, and not one byte of `tests/oracles/` moves. A
test count that differs is a bug this lot introduced, not a new fact about the code.

## Documentation

- `docs/guides/embeddings.md` and `docs/equivalence.md` per D3.
- No ADR, per D3.

## Out of scope

`src/DataNet.Metrics` (#151) and `src/DataNet.Text`, the other zones of #134's sweep. `tests/`, `bench/`,
`samples/` and `tools/`, which are their own zones. The prose documents themselves (#156). And the 619
blocks outside this zone, which is what "zone" means.

## Risks

- **Deleting the only record of a fact.** D4 and D5 are the mitigations, and the BERT case shows the risk is
  not hypothetical. When in doubt the fact moves; a paragraph in the guide costs a reader nothing, and a
  deleted one costs the next maintainer a measurement.
- **Restating what a guide already says**, which is the failure #156 exists to clean up. One grep per moved
  fact.
- **The two big files are the ones this repository has churned most.** #118, #119, #120, #130, #143, #145,
  #121 and #149 all left prose there, and the issue warns to expect claims that were true of an earlier
  design. A claim that is now false is a defect to fix in passing and to name in the report, not a line to
  reformat.
