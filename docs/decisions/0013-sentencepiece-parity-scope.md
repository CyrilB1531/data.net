# 0013 — What `SentencePieceTokenizer` promises, and where it knowingly differs

**Status:** accepted, superseded in part by [`0014`](0014-precompiled-normalizer.md) · **Date:** 2026-08-06

> Section 1 below no longer holds: `0014` implemented the precompiled
> normalizer, the fixture keeps its stock `nmt_nfkc` map, and the oracle
> covers the stock XLM-R pipeline rather than its vocabulary alone. That is
> the revision this decision asked for in its last consequence. Section 2,
> the unknown-piece penalty floor, stands.

## Context

`SentencePieceTokenizer` names ALBERT, T5, camemBERT and XLM-R in its
documentation. [#63](https://github.com/CyrilB1531/data.net/issues/63) asked for
that claim to be *earned*: a control-piece filter that guessed by id worked only
for vocabularies laying `<unk>`, `<s>` and `</s>` out at 0, 1 and 2, and the
oracle — a 984-byte self-trained model doing exactly that — could not see the
failure. The filter now reads `SentencePieceVocabulary.Types`, and the oracle
now includes XLM-R's own vocabulary.

Building that oracle surfaced two things the parity claim has to state rather
than imply.

## Decision

### 1. The oracle covers the XLM-R *vocabulary*, not the stock XLM-R *pipeline*

`xlm-roberta-base` ships two things this library treats differently:

| | Stock file | What DataNet does |
| --- | --- | --- |
| Vocabulary — 250 000 pieces, their scores, their types | `sentencepiece.bpe.model` | reproduced exactly |
| Normalizer — `nmt_nfkc` plus a 237 KB precompiled character map | same file | **refused** by `SentencePieceModelLoader` |

Two further facts make a straight copy of the stock file useless as a fixture:

- its **own** layout is the raw one (`<unk>`=0, `<s>`=1, `</s>`=2, no `<pad>`,
  no `<mask>`) — the fairseq numbering lives in HuggingFace's tokenizer wrapper,
  not in the file, so the stock file exercises precisely the case the old guess
  got right;
- it cannot be loaded at all, because of the normalizer.

`tools/fetch_xlmr_vocab.py` therefore **re-emits** the vocabulary:
`tests/oracles/xlmr_fairseq.model` carries the same pieces, scores and types at
the ids HuggingFace gives them (`<s>`=0, `<pad>`=1, `</s>`=2, `<unk>`=3,
`<mask>`=250001), with the normalizer set to `identity`. The reference values in
`xlmr_fairseq.json` come from `sentencepiece` reading that same file, so both
sides normalize identically and the comparison stays honest.

What this proves: over a real 250 002-piece multilingual vocabulary in the
layout the issue is about, DataNet's Viterbi segmentation is identical to
`sentencepiece`'s, and no control or unknown piece is ever matched as text — not
even for input that names all five markers literally.

What it does not prove: that DataNet reproduces stock XLM-R end to end. It does
not, and says so — `nmt_nfkc` is refused rather than approximated, because a
tokenizer that normalizes differently produces different embeddings while
looking like it works. Implementing the precompiled character map is a separate
piece of work.

### 2. The unknown-piece penalty has a floor at 0

`sentencepiece` scores an uncovered character at `min_score - 10`, where
`min_score` is the lowest piece score in the vocabulary. DataNet computes
`min(0, min_score) - 10`.

The two agree for every real model — SentencePiece scores are log-probabilities,
so `min_score` is negative and the floor never binds. They differ only for a
hand-built vocabulary whose scores are all positive, where DataNet penalises the
unknown piece *more* than `sentencepiece` would (`-10` against, say, `+5 - 10`).

**Keep the floor.** It is the safe direction — a heavier penalty can only make
the unknown piece lose a comparison it would otherwise have won — and it keeps
the constructor total: initialising to `double.MaxValue` instead would give a
vocabulary with no matchable piece an unknown score of `double.MaxValue - 10`,
which is a positive reward, not a penalty.

## Consequences

- `docs/equivalence.md` states which models the tokenizer row is verified
  against, instead of an unqualified "exact parity".
- The 5.3 MB fixture is a redistributed third-party resource. It is the
  vocabulary only — never weights, per
  [`0003`](0003-provenance-and-licensing.md) — and is attributed in
  `THIRD-PARTY-NOTICES.md`. `xlm-roberta-base` is MIT-licensed.
- The fixture is an *input* to `tools/generate_oracles.py`, like `tiny_sp.model`
  before it: the `Oracles are reproducible` job regenerates the JSON from it and
  needs no network. Rebuilding the fixture itself is a deliberate act
  (`tools/fetch_xlmr_vocab.py`), pinned to a SHA-256 of the upstream download.
- If the precompiled normalizer is ever implemented, this decision should be
  revisited: the stock file would then load, and the oracle could replay stock
  XLM-R rather than its vocabulary.
