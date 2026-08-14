# 0123 — Decide, in writing, whether `dropout` is ever reproduced

**Issue:** [#123](https://github.com/CyrilB1531/data.net/issues/123) · **Date:** 2026-08-14 ·
**Branch:** `docs/123-decide-dropout` · **Lot 6 of, and last in,** [#105](https://github.com/CyrilB1531/data.net/issues/105)

## Context

`LoadBpe` refuses a `model` declaring a non-zero `dropout`. #105 said the refusal "may not be worth"
reversing and that **deciding it explicitly is part of the issue**. This is that decision.

The measurements are done and are not re-run here — a parallel session produced them and they are committed
beside this file as `2026-08-14_0123_dropout-research-handover.md`. Three of them matter:

- **`dropout` acts at encode time, not at load.** Twelve `encode("abc")` calls on the *same* tokenizer at
  `dropout=0.5` gave three distinct token streams. The refusal is a choice about behaviour, not an
  impossibility.
- **`tokenizers` 0.23.1 exposes no seed at all** — none on the module, none on `models.BPE`, and Python's
  `random.seed` does not reach it because the randomness is Rust's `thread_rng`. Verified again for this
  spec: two runs after `random.seed(42)` diverge, and diverge differently from the handover's own pair.
- **No model that could be read declares a non-null dropout: 23 read, 19 of them BPE, zero.** The handover
  states the limit rather than hiding it: five of the eight unreadable ship no `tokenizer.json` at all, and
  those five are the NMT and multilingual lineage subword regularization comes from. This evidences a
  convention, not an absence.

## Decisions

### D1 — a distributional comparison **does** count as proof here

The obvious objection to reproducing dropout is that this repository proves every algorithm by replaying
values frozen from the reference, and a reference with no seed cannot be frozen. That is true of *exact*
values only.

**Comparing distributions is accepted as proof.** Over many encodes of a fixture, the set of reachable
segmentations and their frequencies are a property of the algorithm, and a corpus can hold them. It is a
weaker instrument than the `1e-9` comparison the rest of the suite uses — it proves a distribution rather
than a behaviour, needs a large sample for a tight bound, and would land in the one CI job already known to
be flaky — but it is an instrument, and ruling it out by silence would have been the easy answer rather than
the true one.

**So the structural objection falls, and this decision may not rest on it.** Saying "no deterministic
tokenizer reproduces it" is not a reason to refuse; it is a description of dropout.

### D2 — the refusal stands, and it now rests on demand rather than on impossibility

What is left, once D1 removes the impossibility argument, is cost against use:

- **Nobody ships it.** Zero of the 23 models read declare a non-null dropout, and the convention is
  explicable: dropout is a *training-time* augmentation — it exists to make a model robust to alternative
  segmentations while it learns — and inference wants the segmentation the model was trained to expect.
- **The instrument is expensive and weak.** A distributional corpus large enough to bound the comparison
  tightly, replayed in a job whose flakiness this repository already documents, to prove a setting no
  measured file uses.

So `LoadBpe` keeps refusing a non-zero `dropout`. **The reason changes**: not that it cannot be reproduced,
but that reproducing it would buy a behaviour no surveyed model asks for, at the price of the only
statistical test in an otherwise exact suite.

This is reversible on evidence. A file that declares it, or a user who asks, reopens the question — and #123
says plainly that the implementation would then be a fresh issue rather than this one.

### D3 — the exception says what to do instead, as ADR 0017 §3 does for Llama-2

The current message says dropout "drops merges at random during tokenization, which no deterministic
tokenizer reproduces". Under D1 that is no longer the reason, and under D2 it is not the whole story: it
tells a user what DataNet will not do without telling them what to do.

The replacement route is that **`dropout` is a training-time setting and inference does not want it**:
setting the field to `null` loads the file and changes nothing about what the model was trained to produce.
That is not a workaround, it is what the field means outside training, and it is the sentence the exception
should carry.

### D4 — the decision is ADR 0034

`.next-adr` reports 0034 free, checked across every worktree, branch and sibling clone on this machine.

The ADR records what D1 rules — that distributional proof is admissible — because that ruling outlives this
setting: it is the repository's standing answer for any behaviour with no reproducible seed, and the next
one to arrive should find it decided rather than argue it again.

### D5 — no code changes beyond the message

`EnsureBpeModelSettingsAreReproduced` keeps its shape, its exempt `0.0`, and its `S1244` suppression. What
changes is the second half of the exception and the doc comment above it, both of which currently assert the
reason D1 discards.

`docs/equivalence.md`'s `LoadBpe` row keeps "a **non-zero** `dropout`" among the refusals and gains the
route, in the same clause.

**#105 closes with this**, its six lots done.

## Documentation

`docs/decisions/0034-dropout-is-refused-for-want-of-a-user.md`, `docs/equivalence.md`, and the two comments
in `TokenizerJsonLoader.cs`. No guide change: the guide's refusal list already names `dropout`, and it gains
the route in the same sentence.

## Out of scope

Implementing BPE-dropout, which D2 declines and #123 says would be a fresh issue. Revisiting the exempt
`0.0`, settled in #118. The five models that ship no `tokenizer.json`, which cannot declare the field.

## Risks

- **D1 is the load-bearing ruling and it is wider than this lot.** Admitting distributional proof affects
  every future decision about a non-deterministic behaviour, which is why it goes in the ADR rather than in
  this spec alone.
- **"Nobody ships it" is a survey, not a census.** HuggingFace cannot search inside `tokenizer.json`; 23
  repositories is what was read. D2 is written to be reversed by a single counter-example, and says so.
