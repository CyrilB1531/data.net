# 0438 — `Lodestar.Embeddings` against `Microsoft.ML.Tokenizers`

**Issue:** [#438](https://github.com/CyrilB1531/lodestar/issues/438) ·
**Status:** accepted · **Date:** 2026-08-31

## Problem

The second of [#438](https://github.com/CyrilB1531/lodestar/issues/438)'s four boxes.
[The Fuzzy lot](2026-08-31_0438_lodestar-fuzzy-against-its-dotnet-incumbents.md) established the
shape; this applies it to `Lodestar.Embeddings`, whose named incumbent is first-party.

Half of that box is already answered: #438 says `TensorPrimitives.CosineSimilarity` against our
kernel is "the same measurement as V6" in #437, which
[decision 0060](../../decisions/0060-tensorprimitives-beats-our-kernel-and-the-knn-is-still-not-redundant.md)
recorded and `bench/README.md` section 14 documents. What is unmeasured is the other half:
`Microsoft.ML.Tokenizers`.

## Scope

One class, `TokenizerIncumbentBenchmarks`, over the two models both libraries implement from the
same artefact:

| model | ours | theirs | artefact |
| --- | --- | --- | --- |
| WordPiece | `WordPieceTokenizer` | `Microsoft.ML.Tokenizers.WordPieceTokenizer` | `vocab_30k.txt` / `tokenizer_30k_wordpiece.json` |
| SentencePiece unigram | `SentencePieceTokenizer` | `Microsoft.ML.Tokenizers.SentencePieceTokenizer` | `spiece_30k.model` |

BPE is out of scope: `Microsoft.ML.Tokenizers` builds its BPE from a `vocab.json` plus a
`merges.txt`, and the corpus holds a HuggingFace `tokenizer.json`. Comparing those would mean
generating a second artefact for the incumbent's benefit, which is a different lot's decision.

`Microsoft.ML.Tokenizers` 2.0.0, pinned exactly, referenced by `bench/` and by nothing under
`src/`.

## The precondition, and what it caught

Both sides were checked to return **identical ids** on 200 documents of the corpus before the class
was written. They do — for both models — but not at the first attempt:
`Microsoft.ML.Tokenizers.SentencePieceTokenizer` prepends a beginning-of-sentence piece by default,
so every document differed by one leading id. `Create(stream, false, false)` turns both sentence
markers off and the ids match exactly.

That is precisely the kind of difference an unchecked table measures without noticing: a per-call
constant on one side only, small enough to look like noise and large enough to be a different
function. Section 14's agreement-first rule earning itself again.

## The numbers stay out of `performance.md`

Same reasoning as the Fuzzy lot: the run available here is a shared container, ADR 0051 withdrew a
1.61× taken that way, and section 14 records the container inverting every `TensorPrimitives`
ratio. The named-machine page and the nightly take it from here; the class is in `bench-map.json`,
selected by any change under `src/Lodestar.Embeddings/Tokenization/` or `.../Persistence/`.

## What the container run showed

`Microsoft.ML.Tokenizers` wins both rows, and not narrowly: 2.1× on WordPiece and 12× on
SentencePiece. The allocation is the part that does not depend on the host —
**118.84 MB against 3.55 MB, and 519.51 MB against 3.09 MB**, for the same 5 000 documents and the
same ids.

Filed as [#498](https://github.com/CyrilB1531/lodestar/issues/498). Half a gigabyte to encode 5 000
short documents is a per-piece allocation somewhere in the encode path rather than a constant
factor, and the incumbent doing the same work in 3 MB is the evidence that it is not inherent to
the model.

This is the second negative of the two boxes closed so far, and #438 was explicit that a negative
is the point of running the measurement at all.

## Testing

- `tools/check_bench_map.py` refuses a `[Benchmark]` class the map does not name; the class is
  mapped and the check passes.
- The build is clean at `AnalysisMode=All` with warnings as errors.
- The class runs to completion under `--job short`, which proves the harness rather than the
  numbers.
- The id-agreement check is a precondition of the lot rather than a committed test: it compares two
  third-party-shaped APIs over a git-ignored corpus, and `tests/oracles/` already holds what the
  ids must be.
