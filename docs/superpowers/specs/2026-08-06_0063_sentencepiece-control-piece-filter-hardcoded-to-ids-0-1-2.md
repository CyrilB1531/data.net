# Design — #63: the control-piece filter, and the test that could not fail

**Date:** 2026-08-06 · **Issue:** #63 · **Branch:** `fix/63-vacuous-control-piece-test` ·
**Checkout:** `<repo>`

## Where the issue stands

`SentencePieceTokenizer` decided which entries were *control pieces* with a
hardcoded id test (`p.Id is 0 or 1 or 2`). A vocabulary placing `<unk>`, `<pad>`
or `<mask>` elsewhere left those strings **matchable as ordinary text**, competing
in the Viterbi search against real pieces — silently, with no exception, just a
different segmentation and therefore a different embedding.

XLM-R is the case: two id conventions circulate for the same vocabulary, and the
HuggingFace/fairseq one (`<s>`=0, `<pad>`=1, `</s>`=2, `<unk>`=3, `<mask>`=250001)
is the one a user is most likely to have.

**Most of this was resolved by #66**, which landed after the issue was filed:
control pieces are now driven by `SentencePieceVocabulary.Types` and
`IsMatchable(id)`, no id is hardcoded in the new constructor, the id-based one is
`[Obsolete]` with removal stated for v2.0.0, and the loaders landed with a
hand-written protobuf reader.

**Three criteria remain open**, and one turns out to be wrong.

## The two findings this branch is actually about

### F1 — The regression test could not fail

`Controls_outside_the_first_three_ids_are_still_excluded` encoded `"as"` and
asserted `"<s>"` was not among the tokens. A SentencePiece piece only ever matches
where its literal characters occur, and `"as"` preprocesses to `"▁as"`, which
**contains no `<`**. The marker could not have been emitted either way.

Proven by mutation, not by reading: with `IsMatchable` forced to `return true` —
the exclusion removed entirely — **the test still passed**.

The only thing that caught the break was
`SentencePieceModelLoaderTests.Control_and_unknown_pieces_are_excluded_from_matching`,
which restates `IsMatchable`'s own logic against `Types`. The end-to-end property
— *`Encode` never emits a control piece* — was covered by nothing.

### F2 — The oracle could not see the bug either

`tests/oracles/sentencepiece.json` was frozen from `tiny_sp.model`: 984 bytes,
self-trained, `<unk>`/`<s>`/`</s>` at 0/1/2 — **exactly the layout the id guess got
right**. "Exact parity" was asserted over a fixture unable to contradict it.

## Decisions

### D1 — Feed the regression test an input where the marker can appear

`"a<s>s"` rather than `"as"`, so the marker's own string is present and the
assertion can fail. Also assert the control id is absent from `Ids`.

### D2 — The oracle gets XLM-R's own vocabulary

Neither transformation `tools/fetch_xlmr_vocab.py` applies is cosmetic:

- **The stock `sentencepiece.bpe.model` is laid out `<unk>`=0, `<s>`=1,
  `</s>`=2**, with no `<pad>` and no `<mask>`. The fairseq numbering everyone
  actually meets lives in HuggingFace's tokenizer wrapper, **not in the file**.
  Committing the stock file would add 5 MB of fixture exercising the case that
  already worked.
- **It is trained with `nmt_nfkc`**, which `SentencePieceModelLoader` refuses on
  purpose. Left alone it would not load at all.

So the script re-emits the vocabulary: the same 250 000 pieces, scores and types,
at the ids HuggingFace gives them, normalizer set to `identity`. Reference values
come from `sentencepiece` reading **that same file**, so both sides normalize
identically.

That makes the claim precise — parity over the XLM-R **vocabulary**, not over the
stock XLM-R **pipeline** — and ADR 0013 plus the `equivalence.md` rows say exactly
that instead of an unqualified "exact parity".

Inputs include `un texte avec <s>, </s>, <pad>, <unk> et <mask> dedans`, which is
the only kind that can fail.

### D3 — The cost is stated, not buried

`tests/oracles/xlmr_fairseq.model` is **5.3 MB — by far the largest file in the
repository** (the next is 984 bytes). Vocabulary only, never weights (ADR 0003),
MIT-licensed and attributed. Like `tiny_sp.model` it is an *input* to
`generate_oracles.py`: committed, pinned to the upstream SHA-256, replayed without
network.

### D4 — A fixture-free mirror of the same property

`A_fairseq_layout_matches_none_of_its_five_markers` — 18 pieces, microseconds.
Markers scored 0 (the best in the vocabulary) and every input character covered by
a normal piece, so **an id from the marker set in the output means the marker was
matched as text, and nothing else**.

Stated separately from the corpus replay so it survives a regenerated corpus.

### D5 — The `minScore` criterion is retracted, not quietly dropped

The issue asked for `minScore` to be initialised to `double.MaxValue`. Retracted
with the reasoning recorded — an acceptance criterion that turns out to be wrong
should be withdrawn in the open, or the next reader re-raises it.

### D6 — No production code changes

Everything here is tests, fixtures and documentation. If a fix were needed, it
would mean #66 left the bug in — and it did not.

## What "done" means

The regression test able to fail, proven by mutation; the oracle carrying a
vocabulary the id guess would get wrong; `equivalence.md` and ADR 0013 stating
what parity actually covers; the retraction recorded.
