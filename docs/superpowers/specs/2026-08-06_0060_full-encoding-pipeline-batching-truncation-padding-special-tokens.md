# Design — #60: the full encoding pipeline

**Date:** 2026-08-06 · **Issue:** #60 · **Branch:** `feat/60-batch-encoding-pipeline` ·
**Checkout:** `<repo>`

## Problem

`docs/guides/embeddings.md` states that the tokenization must match the model's
**exactly, otherwise the embeddings are wrong** — and three paragraphs later hands
the reader:

```csharp
long[] ids = /* wp.Encode(text).Ids, with [CLS]/[SEP] if the model expects them */;
long[] mask = /* 1 per real token, 0 for padding */;
```

The library states a requirement and then delegates it to the caller, at the exact
point where getting it wrong produces plausible, silently wrong vectors. On top of
that, `Embed` takes one sequence at a time, which leaves ONNX Runtime's batching
on the table.

## Decisions

### D1 — The encoder owns the special tokens, as data

`SpecialTokenTemplate` carries the wrapping: `Bert`, `Roberta`, `T5`, `None`, or
one you write.

**Tokens are named, never numbered.** The id comes from the model's own vocabulary
through `ISubwordTokenizer.TryGetId`, so a vocabulary placing `[CLS]` anywhere
works, and one lacking it **throws at construction** rather than embedding a
plausible wrong id.

The corpus proves this rather than asserting it: it puts `[CLS]` at 45, `[SEP]` at
46 and `[PAD]` at 47, so **any hardcoded well-known id fails every row of every
case**. A fixture that cannot catch the bug is the failure mode #63 just
documented; this one is built the other way round.

### D2 — Truncation and padding belong to the library

- `MaxLength` is counted **the way HuggingFace counts it**, with the special
  tokens inside the budget.
- `TruncationStrategy.None` **refuses** an over-long document instead of silently
  dropping its tail.
- Each sub-batch is padded to **its own** longest row, never to `MaxLength`.
- The mask is built here, with padding zeroed.

### D3 — `EmbedBatch` is the whole chain

The equivalent of
`SentenceTransformer.encode(texts, batch_size=…, normalize_embeddings=True)`, with
`CancellationToken` on every entry point — which `src/` had nowhere before.

### D4 — Fix the single-sequence path while it is open, and name the defects

Four, all reachable from the public API:

- the default output was `OutputMetadata.Keys.First()` — a **coin toss** on a
  multi-output model;
- rank was validated only for `== 2`, so rank 1 or 4 gave an out-of-range access
  or a silently wrong result;
- input names were taken on trust;
- three allocations per call.

### D5 — Every claim gets a mechanism, and the mechanisms differ on purpose

| Claim | How it is proven |
| --- | --- |
| ids and mask match HuggingFace | replay of `batch_encoding.json` — **equality, not tolerance** — six batches from `tokenizers.encode_batch` |
| padding never reaches a vector | a batched vector equals the single-sequence vector for the same text, **bit for bit**. No reference implementation involved |
| the four edges | nothing / one token / exactly the limit / one over it — and the **generator refuses to emit the fixture** if it stops straddling the limit |
| batching and bucketing are invisible | six `BatchSize` × `SortByLength` combinations, exact equality against the reference run |
| `net10.0` == `netstandard2.0` | vectorized and scalar pooling both compared to a scalar reference with `float` equality, in both test projects |
| the corpus and the model agree | all 64 rows of the embedding table compared through the ONNX model, closing the loop the two Python scripts leave open |

The second row is the one worth noticing: it needs **no reference at all**. A
property expressed as an invariant of our own output cannot be undermined by a
fixture that fails to exercise it.

### D6 — The generator refuses to emit a fixture that stopped testing anything

The edge cases are only meaningful while they straddle `MaxLength`. If a change
moves the limit, the corpus quietly becomes four ordinary cases. So the generator
checks and **fails** instead.

### D7 — The sample shows the batch API, because the gate is what proves it ships

Per ADR 0009, a public type unreachable from `samples/` is a type whose packaging
is unverified.

## Out of scope

- Anything beyond the fixed normalizer/pre-tokenizer pipeline the loaders accept.
- Model weights: the fixtures are two tiny ONNX graphs, built by
  `tools/build_tiny_models.py`.

## What "done" means

The two placeholders gone from the guide; ids, mask and vectors matching
HuggingFace by equality; the four single-sequence defects fixed; the batch path
measured, with what the measurement cannot see stated alongside it.
