# Design — #58: persist fitted models, load pretrained vocabularies

**Date:** 2026-08-05 · **Issue:** #58 · **Branch:** `feat/58-persistence-loaders` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## Problem

There is **zero I/O in `src/`**. Two consequences, both pushing work onto the
caller that the library is better placed to do.

1. **A fitted model does not survive the process.** `TfidfVectorizer.Fit()` learns
   a vocabulary and an idf vector; both die with the AppDomain. Train-then-score —
   the normal split in any real pipeline — is impossible without the caller
   reimplementing serialization over `GetFeatureNames()` and `Idf`.
2. **Vocabularies must be parsed by hand.** The embeddings guide opens with a
   `Dictionary` literal whose `/* … */` is a 30 000-entry `vocab.txt`. The
   SentencePiece section is worse: it asks the reader to parse a protobuf.

There is a visible second-order effect. Because callers build the piece table
themselves, `SentencePieceTokenizer` cannot rely on any id convention, and its
control-token filter is **hardcoded to ids 0/1/2**. A real loader carries that
information instead of guessing it.

## Decisions

### D1 — Loaders that refuse what they cannot reproduce

`VocabTxtLoader`, `TokenizerJsonLoader` (WordPiece and Unigram) and
`SentencePieceModelLoader`.

They **fail loudly** rather than returning a vocabulary that tokenizes differently
from Python: a model trained as BPE/WORD/CHAR, `byte_fallback`, a normalizer or
pre-tokenizer outside the fixed pipeline, a `post_processor`, a special-token id
pointing outside the vocabulary.

A stock T5 or XLM-R `tokenizer.json` carries a `Precompiled` normalizer, so **this
is the common case, not an edge one**. Silently ignoring it would produce wrong
embeddings with no symptom.

### D2 — A hand-written protobuf reader, not a dependency

`spiece.model` is protobuf. `DataNet.Text` ships with no dependencies and that is a
stated selling point; taking `protobuf-net` to read four fields would trade the
selling point for convenience. Pieces, scores, ids and types are all this needs.

### D3 — JSON, versioned from the first commit

`System.Text.Json` — in-box on net10, a package reference on `netstandard2.0`, and
the one deliberate runtime dependency of the persistence layer.

**`ArtifactVersion` from the first commit.** A persisted artifact outlives the
library version that wrote it. It stays at 1: 0.2.0 is the last published release
and all persistence is new here, so nothing in the wild carries an older shape.

No `BinaryFormatter`, no polymorphic deserialization. Loaded files are untrusted
input.

### D4 — Bound what is read, and validate structure on load

Maximum vocabulary size, maximum token length, maximum JSON depth. A malformed or
hostile `tokenizer.json` must fail with a clear exception, not OOM.

`CsrMatrix` currently accepts raw arrays without checking that `RowPointers` is
monotonic, that `RowPointers[^1] == Values.Length`, or that column indices are in
range. Deserialization turns that from a caller-discipline issue into an
**out-of-bounds read**, so structural validation is part of this work.

### D5 — The format is measured against `pickle`, and changed when it loses

The first cross-language measurement had the round trip **losing to `pickle` in
both directions** — 1.66× on save, 2.25× on load. Profiling put the cost on the
idf vector: written as 30 000 JSON numbers it cost four times what materialising
the whole vocabulary cost.

So **only the idf vector leaves readable JSON**, as one base64 string of raw
IEEE-754 bits. Exactness *improves* — raw bits round-trip by construction — and
everything a person reads stays plain text.

| | before | after |
| --- | --- | --- |
| `Save` vs `pickle.dumps` | 0.60× | **2.33×** |
| `Load` vs `pickle.loads` | 0.44× | **0.95×** wall, 0.77× cpu |

Three smaller changes followed the same rule of being kept only because they
measured. **Two were measured and discarded** for showing nothing — recorded, so
they are not retried.

### D6 — Benchmarks report processor time, not only elapsed

Elapsed time alone flatters .NET, which collects on background threads at
1.1–1.2 cores while CPython measures exactly 1.00. Reading only the wall column
reported a parity on `tfidf_load` that **disappears the moment two models load at
once**.

Five of six rows win on both columns; the sixth is recorded rather than hidden.

### D7 — Say what the comparison is not measuring

HuggingFace and `sentencepiece` build a whole tokenizer where DataNet builds a
validated dictionary and stops. Part of the loader margin is **work not done**, and
the README says so.

### D8 — Bit-exact round trip, not "within tolerance"

`fit → save → load → transform` must produce a `CsrMatrix` whose `Values`,
`ColumnIndices` and `RowPointers` are identical element by element. Options
round-trip completely, `HashingVectorizer` included — it is stateless, but a
reloaded pipeline with the wrong options is **silently mis-configured**, which is
the failure this criterion exists to prevent.

## Out of scope

- A binary format. Only if a benchmark shows JSON is a real bottleneck; it did
  not.
- Parsing the full normalizer/pre-tokenizer graph (D1 refuses instead).
- `EmbeddingIndex` persistence (#62), which becomes a thin addition once the
  format and the version header are settled here.

## What "done" means

Bit-exact round trip; loaders replaying corpora frozen from HuggingFace
`tokenizers` and `sentencepiece`; a documented exception per malformed input, each
with a test; ADR 0011 recording the format, what was measured and what was
rejected; the `/* … */` gone from both guides.
