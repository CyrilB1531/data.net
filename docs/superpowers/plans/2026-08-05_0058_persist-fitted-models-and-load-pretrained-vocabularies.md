# #58 Persistence and vocabulary loaders — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Give the library an I/O layer for the two things that need it — fitted vectorizers and pretrained vocabularies — with a bit-exact round trip, loaders that refuse what they cannot reproduce, and a format whose shape is decided by measurement rather than taste.

**Architecture:** Versioned JSON artifacts through `System.Text.Json`, with the idf vector as base64 raw IEEE-754 bits because JSON numbers measured four times the cost of the whole vocabulary. Three loaders — `vocab.txt`, `tokenizer.json`, `spiece.model` — the last through a hand-written protobuf reader rather than a dependency. Structural validation on load, because deserialization turns caller discipline into an out-of-bounds read.

**Tech Stack:** `System.Text.Json` (in-box on net10, package on netstandard2.0), a minimal hand-written protobuf reader, xunit, BenchmarkDotNet, Python `pickle` / HuggingFace `tokenizers` / `sentencepiece` for comparison and oracles.

**Spec:** `2026-08-05_0058_persist-fitted-models-and-load-pretrained-vocabularies.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `feat/58-persistence-loaders`. Never commit to `main`.
- **Zero new dependencies beyond `System.Text.Json`.** No `Newtonsoft.Json`, no
  `protobuf-net`, no `MessagePack`. The dependency-free core is a stated selling
  point.
- **Never `BinaryFormatter`; no polymorphic deserialization.** Loaded files are
  untrusted input.
- **No model weights** (ADR 0003). Vocabularies only.
- `ArtifactVersion` present from the first commit that writes a file.
- Both frameworks build and both test suites pass.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

build_all() { dotnet build -c Release; }
test_all()  { dotnet test -c Release; }
test_p()    { dotnet test -c Release --filter "FullyQualifiedName~Persistence"; }
```

---

### Task 1: Decide the artifact shape, and version it

**Files:**

- Create: `src/Shared/Persistence/` (writer/reader helpers, `ArtifactVersion`)
- Modify: `src/Directory.Packages.props`, `src/Directory.Build.props`

**Depends on:** nothing.
**Produces:** the one decision #62 will inherit rather than re-make.

- [ ] **Step 1: Add `System.Text.Json`, target-conditional**

In-box on net10; a package reference for `netstandard2.0` only. Note in the props
that this is the **one deliberate runtime dependency** of the persistence layer.

- [ ] **Step 2: A version field written by every artifact from the first commit**

A persisted file outlives the library that wrote it. Retrofitting a version means
guessing the shape of files already on disk.

- [ ] **Step 3: Bound the reader**

Maximum vocabulary size, maximum token length, maximum JSON depth. A hostile
`tokenizer.json` must fail with a clear exception rather than exhausting memory.

---

### Task 2: Structural validation, before anything can deserialize into it

**Files:**

- Modify: `src/DataNet.Text/Vectorization/CsrMatrix.cs`

**Depends on:** Task 1.
**Produces:** the guarantee that a malformed artifact cannot become an
out-of-bounds read.

- [ ] **Step 1: Validate on construction**

`RowPointers` monotonic, `RowPointers[^1] == Values.Length`, column indices in
range, lengths agreeing.

- [ ] **Step 2: Understand why this is in scope**

Today these are caller-discipline issues — the caller built the arrays.
Deserialization makes a *file* the source, and a wrong file becomes a memory
safety problem.

- [ ] **Step 3: A test per invariant, each with the exception it produces**

---

### Task 3: Vectorizer persistence, and the bit-exact round trip

**Files:**

- Create: `src/DataNet.Text/Persistence/` (save/load for `CountVectorizer`,
  `TfidfVectorizer`, `HashingVectorizer`)
- Create: `tests/DataNet.Text.Tests/Persistence/`

**Depends on:** Task 2.

- [ ] **Step 1: Persist options, vocabulary, idf and feature count**

- [ ] **Step 2: `HashingVectorizer` too, even though it is stateless**

Its **options** must round-trip, or a reloaded pipeline is silently
mis-configured — the worst kind of failure, because it produces plausible numbers.

- [ ] **Step 3: The round trip is bit-exact, not "within tolerance"**

```bash
test_p 2>&1 | tail -3
```

The reloaded model's `CsrMatrix` must match element by element on `Values`,
`ColumnIndices` and `RowPointers`. Tolerance would hide exactly the drift this
feature can introduce.

- [ ] **Step 4: Options round-trip test with non-defaults**

`NgramRange`, `MinDf`, `MaxDf`, `Analyzer`, `StopWords`, `TokenPattern` all
non-default, and the reloaded vectorizer behaving identically.

- [ ] **Step 5: One test per way it can fail**

Malformed, truncated, oversized, wrong version — each with a documented exception.

---

### Task 4: The loaders, and what they refuse

**Files:**

- Create: `src/DataNet.Embeddings/Persistence/VocabTxtLoader.cs`
- Create: `src/DataNet.Embeddings/Persistence/TokenizerJsonLoader.cs`
- Create: `src/DataNet.Embeddings/Persistence/SentencePieceModelLoader.cs`

**Depends on:** Task 1.

- [ ] **Step 1: `vocab.txt` — one token per line, id = line number**

- [ ] **Step 2: `tokenizer.json` — WordPiece and Unigram**

Read `model.vocab`, `unk_token`, the continuation prefix, and the added/special
tokens.

- [ ] **Step 3: `spiece.model` — a hand-written protobuf reader**

Pieces, scores, ids, **types**. The types are what let #63's control filter stop
guessing from ids.

- [ ] **Step 4: Refuse, loudly, what cannot be reproduced**

A model trained as BPE/WORD/CHAR; `byte_fallback`; a normalizer or pre-tokenizer
outside the fixed pipeline; a `post_processor`; a special-token id outside the
vocabulary.

**A stock T5 or XLM-R `tokenizer.json` carries a `Precompiled` normalizer**, so
this path is the common case. Returning a vocabulary that tokenizes differently
from Python would be worse than refusing: the failure would be silent and the
embeddings wrong.

- [ ] **Step 5: Oracles from `tokenizers` and `sentencepiece`**

Exact string comparison, `1e-9` on the scores. Vocabularies only — no weights.

---

### Task 5: Measure the format against `pickle`, and change it if it loses

**Files:**

- Create: `bench/DataNet.Text.Benchmarks/PersistenceBenchmarks.cs`,
  `CrossLang/PersistenceCrossLang.cs`, `bench/python/bench_persistence.py`
- Create: `bench/corpus/generate_vocabs.py`

**Depends on:** Tasks 3 and 4.
**Produces:** the reason the format looks the way it does.

- [ ] **Step 1: Measure the naive format**

Expected: **losing to `pickle` in both directions** — around 1.66× on save, 2.25×
on load. Do not skip this; the redesign is only justified by the number.

- [ ] **Step 2: Profile, and find where the cost is**

Expected: the **idf vector**. Written as 30 000 JSON numbers it costs four times
what materialising the whole vocabulary costs.

- [ ] **Step 3: Move only the idf vector out of readable JSON**

One base64 string of raw IEEE-754 bits. Exactness *improves* — raw bits round-trip
by construction — and everything a person reads stays plain text.

- [ ] **Step 4: Re-measure**

| | before | after |
| --- | --- | --- |
| `Save` vs `pickle.dumps` | 0.60× | 2.33× |
| `Load` vs `pickle.loads` | 0.44× | 0.95× wall, 0.77× cpu |

- [ ] **Step 5: Keep only what measures, and record what did not**

Three smaller changes kept because they measured; **two discarded for showing
nothing**. Record the discarded pair in ADR 0011 so they are not retried.

- [ ] **Step 6: Report processor time as well as elapsed**

Elapsed alone flatters .NET, which collects on background threads at 1.1–1.2 cores
while CPython measures exactly 1.00. Reading only the wall column reported a
parity on `tfidf_load` that **disappears the moment two models load at once**.

Five of six rows win on both columns. **Record the sixth rather than hiding it.**

---

### Task 6: Documentation, and the honest framing

**Files:**

- Create: `docs/decisions/0011-persistence-format.md`
- Modify: `docs/guides/embeddings.md`, `docs/guides/vectorization.md`,
  `docs/guides/quickstart.md`, `docs/equivalence.md`, `README.md`, `CHANGELOG.md`,
  `THIRD-PARTY-NOTICES.md`, `bench/README.md`

**Depends on:** Task 5.

- [ ] **Step 1: ADR 0011 — the format, the measurements, the rejections**

- [ ] **Step 2: The `/* … */` disappears from both guides**

Replaced by a real one-liner. That placeholder was the issue's opening exhibit.

- [ ] **Step 3: An `equivalence.md` row per loader, naming the Python call**

- [ ] **Step 4: Say what the benchmark is not measuring**

HuggingFace and `sentencepiece` build a whole tokenizer where DataNet builds a
validated dictionary and stops. **Part of the loader margin is work not done**, and
a benchmark table that omits this is accurate and misleading.

- [ ] **Step 5: Full gate**

```bash
build_all && test_all 2>&1 | tail -3
dotnet format --verify-no-changes
npx --yes --ignore-scripts markdownlint-cli2@0.23.2 "README.md" "CONTRIBUTING.md" "docs/**/*.md" "tools/README.md" "bench/README.md"
```

Expected: 724 tests across both target frameworks, 0 warnings under
`TreatWarningsAsErrors`.

- [ ] **Step 6: Cover the overloads nobody was testing**

Before calling this done, check which public persistence overloads have no test.
A new API surface is exactly where coverage silently lags.
