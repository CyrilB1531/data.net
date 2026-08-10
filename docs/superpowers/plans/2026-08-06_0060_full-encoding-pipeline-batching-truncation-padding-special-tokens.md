# #60 Full encoding pipeline — Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** The library keeps its own requirement — tokenization matching the model exactly — by owning the special tokens, truncation, padding and batching, so the two `/* … */` placeholders disappear from the guide.

**Architecture:** `SpecialTokenTemplate` carries the wrapping as data and resolves ids **by name** through the model's own vocabulary. `BatchEncoder` handles truncation, per-sub-batch padding and the mask. `EmbedBatch` is the whole chain, with `CancellationToken` throughout. The single-sequence path is repaired while it is open. Every claim gets its own proof mechanism, and one of them needs no reference implementation at all.

**Tech Stack:** C# (net10.0 + netstandard2.0), ONNX Runtime, HuggingFace `tokenizers` for oracles, two tiny ONNX graphs built by `tools/build_tiny_models.py`, BenchmarkDotNet.

**Spec:** `2026-08-06_0060_full-encoding-pipeline-batching-truncation-padding-special-tokens.md` (in `../specs/`).

## Global Constraints

- **Everything in English.**
- **Do not commit until the user asks.** Do not merge. Do not tag.
- Branch `feat/60-batch-encoding-pipeline`. Never commit to `main`.
- **No special-token id is ever hardcoded.** Ids come from the vocabulary by name.
  A vocabulary lacking a required token throws at construction.
- **No model weights** (ADR 0003). Fixtures are tiny synthetic ONNX graphs.
- Every new public type must be reachable from `samples/DataNet.Sample` (ADR 0009).
- Both frameworks build; both test suites pass.

### Reusable verification commands

```bash
cd /home/cyril/Documents/devs/data.net

build_all() { dotnet build -c Release; }
test_all()  { dotnet test -c Release; }
test_batch(){ dotnet test -c Release --filter "FullyQualifiedName~Batch"; }
```

---

### Task 1: The fixtures, and a second tiny model

**Files:**

- Modify: `tools/build_tiny_models.py`, `tools/README.md`

**Depends on:** nothing.
**Produces:** models whose *layout* can catch a hardcoded id.

- [ ] **Step 1: Say how the tiny models are built**

They are committed fixtures with no generation story written down, which makes
them unreproducible. Fix that first.

- [ ] **Step 2: Build a second model, for the multi-output case**

Task 4 fixes a defect that only shows on a model with more than one output. A
defect with no fixture is a defect that comes back.

- [ ] **Step 3: Place the special tokens at unusual ids**

`[CLS]` at 45, `[SEP]` at 46, `[PAD]` at 47.

This is the single most important construction in the branch: **any hardcoded
well-known id then fails every row of every case**. Building the fixture so it
*can* fail is the lesson #63 paid for.

---

### Task 2: Special tokens as data, resolved by name

**Files:**

- Create: `src/DataNet.Embeddings/Tokenization/EncodingOptions.cs`
  (`SpecialTokenTemplate`, `TruncationStrategy`)
- Create: `src/DataNet.Embeddings/Tokenization/EncodedBatch.cs`

**Depends on:** Task 1.

- [ ] **Step 1: `Bert`, `Roberta`, `T5`, `None`, and a user-written template**

- [ ] **Step 2: Resolve ids through `ISubwordTokenizer.TryGetId`**

Named, never numbered. A vocabulary that places `[CLS]` anywhere works.

- [ ] **Step 3: Throw at construction when a required token is missing**

Not at encode time, and never a fallback id. A plausible wrong id produces a
plausible wrong vector, which is the failure this whole branch exists to prevent.

---

### Task 3: Truncation, padding and the mask

**Files:**

- Create: `src/DataNet.Embeddings/Tokenization/BatchEncoder.cs`

**Depends on:** Task 2.

- [ ] **Step 1: Count `MaxLength` the way HuggingFace counts it**

Special tokens **inside** the budget. This is the detail most likely to be off by
two, and the corpus is what will catch it.

- [ ] **Step 2: `TruncationStrategy.None` refuses rather than truncates**

Silently dropping a document's tail is the behaviour that produces a wrong
embedding with no symptom.

- [ ] **Step 3: Pad each sub-batch to its own longest row**

Never to `MaxLength`. Padding to the maximum wastes the whole point of batching.

- [ ] **Step 4: Build the mask here, with padding zeroed**

---

### Task 4: Repair the single-sequence path while it is open

**Files:**

- Modify: `src/DataNet.Embeddings/Onnx/OnnxTextEmbedder.cs`
- Modify: `src/DataNet.Embeddings/Pooling/Pooling.cs`

**Depends on:** Task 3.

- [ ] **Step 1: The default output is a coin toss — fix it**

`OutputMetadata.Keys.First()` on a multi-output model. Task 1's second fixture is
what makes this testable.

- [ ] **Step 2: Rank was validated only for `== 2`**

Rank 1 or 4 gave an out-of-range access or a **silently wrong result**. Validate
the shape and say what is supported.

- [ ] **Step 3: Stop taking input names on trust**

- [ ] **Step 4: Remove the three allocations per call**

- [ ] **Step 5: Pool a whole batch in one call**

And add the components in parallel where it measures.

---

### Task 5: `EmbedBatch`, the whole chain

**Files:**

- Modify: `src/DataNet.Embeddings/Onnx/OnnxTextEmbedder.cs`

**Depends on:** Task 4.

- [ ] **Step 1: The equivalent of `SentenceTransformer.encode(...)`**

`batch_size`, `normalize_embeddings`, optional length bucketing.

- [ ] **Step 2: `CancellationToken` on every entry point**

`src/` had none anywhere. A batch embedding call is the first operation here long
enough to need one.

---

### Task 6: Prove each claim with the right mechanism

**Files:**

- Modify: `tools/generate_oracles.py`
- Create: `tests/oracles/batch_encoding.json`
- Create: tests under `tests/DataNet.Embeddings.Tests/`

**Depends on:** Task 5.
**Produces:** the part of the branch a reviewer should read first.

- [ ] **Step 1: Freeze what HuggingFace makes of a batch, before asserting anything**

Six batches from `tokenizers.encode_batch`. Corpus first, then the C#.

- [ ] **Step 2: Replay by equality, not tolerance**

Ids and masks are integers. A tolerance here would be meaningless and would hide a
one-off.

- [ ] **Step 3: Check the replay can fail**

Mutate the encoder — drop a special token — and confirm the replay goes red. #63
was filed because a test could not fail; do not add another.

- [ ] **Step 4: Prove padding never reaches a vector, without a reference**

A batched vector must equal the single-sequence vector for the same text, **bit
for bit**. This needs no Python at all: it is an invariant of our own output, so
no fixture gap can undermine it.

- [ ] **Step 5: The four edges, and a generator that refuses to lie**

Nothing / one token / exactly the limit / one over it. Then make the **generator
fail** if those cases stop straddling `MaxLength` — otherwise a later change turns
four edge cases into four ordinary ones, silently.

- [ ] **Step 6: Batching and bucketing are invisible**

Six `BatchSize` × `SortByLength` combinations, exact equality against the
reference run.

- [ ] **Step 7: The two builds agree**

Vectorized and scalar pooling both compared to a scalar reference with `float`
equality, run in both test projects.

- [ ] **Step 8: Close the loop between the corpus and the model**

All 64 rows of the embedding table compared **through the ONNX model**. The two
Python scripts each prove half; this is what joins them.

---

### Task 7: Sample, benchmark, guide

**Files:**

- Modify: `samples/DataNet.Sample/Lot3Embeddings.cs`,
  `samples/DataNet.DocSnippets/SnippetContext.cs`
- Create: `bench/DataNet.Text.Benchmarks/BatchEmbeddingBenchmarks.cs`
- Modify: `docs/guides/embeddings.md`, `docs/guides/performance.md`,
  `docs/equivalence.md`, `CHANGELOG.md`

**Depends on:** Task 6.

- [ ] **Step 1: The sample uses the batch API**

The packaging gate is what proves a public type actually ships (ADR 0009).

- [ ] **Step 2: Measure the batch path, and say what the measurement cannot see**

A tiny synthetic model does not predict a real encoder's throughput. State that
next to the numbers rather than letting them be read as a model-agnostic claim.

- [ ] **Step 3: Delete both placeholders from the guide**

`/* with [CLS]/[SEP] if the model expects them */` and the mask comment. They were
the issue's opening exhibit.

- [ ] **Step 4: Full gate, then read SonarCloud**

```bash
build_all && test_all 2>&1 | tail -3
dotnet format --verify-no-changes
```

A green build is not a clean Sonar. Expect findings on a change this size, and
clear them before the pull request rather than after.
