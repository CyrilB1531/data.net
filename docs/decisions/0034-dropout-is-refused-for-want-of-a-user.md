# 0034 — Distributional proof is admissible, and `dropout` is still refused

**Status:** accepted · **Date:** 2026-08-14

## Context

`TokenizerJsonLoader.LoadBpe` refuses a BPE `model` declaring a non-zero `dropout`, and has since #59. The
refusal was never written down as a decision; the exception carried its own justification, and
[#105](https://github.com/CyrilB1531/data.net/issues/105) said explicitly that deciding it was part of the
work. This records the decision, and one ruling that is wider than it.

`dropout` is BPE-dropout's regularizer: during tokenization each merge is skipped with that probability, so
one input has many segmentations. It exists to make a model robust to alternatives while it **trains**.

### What was measured

On `tokenizers` 0.23.1, in `.venv-oracles`, on 2026-08-14:

- **It acts at encode time.** Twelve `encode("abc")` calls on the *same* tokenizer at `dropout=0.5` gave
  three distinct token streams. Refusing is a choice about behaviour, not an impossibility.
- **There is no seed.** No seed attribute on the `tokenizers` module, none on `models.BPE`, and Python's
  `random.seed` does not reach it: the randomness is Rust's `thread_rng`. Two runs after `random.seed(42)`
  produce different sequences, and a third run differs again.
- **No model that could be read declares one.** 23 repositories fetched, 19 of them BPE: zero non-null
  `dropout`. Five of the eight that could not be read ship no `tokenizer.json` at all — and those five are
  the NMT and multilingual lineage subword regularization comes from, so they cannot declare the field.
  HuggingFace cannot search inside `tokenizer.json`, so this is a convention evidenced, not an absence
  proved.

## Decision

### 1. A distributional comparison counts as proof in this repository

Every algorithm here is proven by replaying values frozen from the reference and comparing at `1e-9`. A
reference with no seed cannot be frozen that way, and the temptation is to conclude that such behaviour is
unprovable and therefore out of scope forever.

**That conclusion is rejected.** Over many encodes of a fixture, the set of reachable segmentations and
their frequencies are a property of the algorithm, and a corpus can hold them. It is a weaker instrument
than the exact comparison: it proves a distribution rather than a behaviour, it needs a large sample for a
tight bound, and it would run in the one job this repository already documents as occasionally flaky. It is
nonetheless an instrument, and this is the repository's standing answer for any behaviour whose reference
cannot be pinned.

**Consequence, and it is the point of writing this down:** "no deterministic tokenizer reproduces it" is a
description of dropout, not a reason to refuse it. Any future refusal resting on that sentence is resting on
nothing.

### 2. `dropout` is refused for want of a user, not for want of a proof

With the impossibility argument gone, what remains is cost against use:

- **Nobody ships it.** Zero of the 23 models read. The convention is explicable rather than accidental —
  dropout is training-time augmentation, and inference wants the segmentation the model was trained to
  expect.
- **The instrument is expensive and weak**, per §1, and would be the only statistical test in an otherwise
  exact suite.

So the refusal stands, and its reason is demand rather than impossibility. **A single file that declares it,
or one user who asks, reopens the question** — and the implementation would then be its own issue, not this
one.

### 3. The exception names the route instead of the impossibility

A refusal that only says what this library will not do leaves the reader stuck.
[ADR 0017 §3](0017-bpe-parity-scope.md) is the standard: it names Llama-2 and Mistral v0.1 and says where to
go instead.

Here the route is that **`dropout` is a training-time setting and inference does not want it**: setting the
field to `null` loads the file and changes nothing about what the model was trained to produce. That is not
a workaround — it is what the field means outside training.

## Consequences

- `EnsureBpeModelSettingsAreReproduced` keeps its shape, its exempt `0.0` (settled in
  [#118](https://github.com/CyrilB1531/data.net/issues/118)) and its `S1244` suppression. Its message and
  its doc comment stop asserting the reason §1 discards.
- `docs/equivalence.md`'s `LoadBpe` row keeps a **non-zero** `dropout` among the refusals and carries the
  route in the same clause.
- **A distributional corpus is now a thing this repository may build**, and the next behaviour with no
  reproducible seed should cite §1 rather than re-argue it.
- [#105](https://github.com/CyrilB1531/data.net/issues/105) closes with this, its six lots done.

## Alternatives rejected

- **Reproduce it under a caller-supplied seed.** Buildable, and §1 says provable. Declined under §2: it
  buys a behaviour no surveyed model asks for, at the price of the only statistical test in an exact suite.
  This is the alternative to revisit first if a real file appears.
- **Accept the file and ignore the field.** The failure this repository exists to avoid: a vocabulary that
  loads cleanly and produces embeddings for a model nobody trained.
- **Accept it with a tolerance around zero.** #118 settled the exact-zero exemption; a tolerance would
  accept small non-zero dropouts, which is the thing being refused.
