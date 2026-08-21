# 0050 — The SentencePiece-BPE lineage stays a BPE model, and metaspace becomes one transform

**Status:** accepted · **Date:** 2026-08-21 · **Amends:** [`0017`](0017-bpe-parity-scope.md) §3

## Context

[#175](https://github.com/CyrilB1531/lodestar/issues/175) opens on files a user actually
has: Llama-2 and Mistral v0.1, which neither tokenizer here loads.
[#315](https://github.com/CyrilB1531/lodestar/issues/315) is the design lot it demands
before any code — *"one tokenizer or two, and where `Metaspace` lives"* — because the
file is refused twice over, and a shape chosen while implementing one half is chosen
without the other in view.

[ADR 0017 §3](0017-bpe-parity-scope.md) named these two models and gave them nowhere to
go: *"a third pipeline, distinct from both the classic and byte-level lineages
`BpeTokenizer` implements and from the `Unigram` + `Metaspace` pipeline
`SentencePieceTokenizer` implements. Neither class reproduces it."*

### What the files actually declare

Read from two ungated mirrors before deciding, because the issue's own framing turned
out to be under-specified:

| | Llama-2 (`NousResearch/Llama-2-7b-hf`) | Mistral v0.1 (`mistralai/Mistral-7B-v0.1`) |
| --- | --- | --- |
| `normalizer` | `Sequence[Prepend "▁", Replace " "→"▁"]` | `null` |
| `pre_tokenizer` | `null` | `Metaspace{replacement "▁", prepend_scheme "first", split false}` |
| `model` | `BPE`, `fuse_unk: true`, `byte_fallback: true` | identical |

**The two models express the same intent through different blocks.** Llama-2 never says
`Metaspace`: it spells it out as a `Prepend` and a `Replace` in the *normalizer*, and its
pre-tokenizer is `null`. Mistral does the reverse. A design that teaches
`BpePreTokenizer` about `Metaspace` would load Mistral and **still refuse Llama-2** —
which is exactly the half-a-view failure #315 exists to prevent, and it would not have
been visible from the issue text.

What unifies them is `split: false`. Mistral's `Metaspace` does not split; it replaces
and prepends. It is a text transform rather than a splitting pre-tokenizer, which is
precisely why Llama-2's normalizer spelling is equivalent to it. Both say: *prepend `▁`,
replace spaces with `▁`.*

Two things checked and found not to be problems: `fuse_unk: true` is already carried by
`BpeVocabulary.FuseUnk` and pinned by `BpeFuseUnkTests`, so it is not a third refusal;
and `BpeVocabulary` is publicly constructible with `init` properties, which existing
tests already use, so each implementation lot can reach its own code without waiting for
the other's refusal to lift — [#208](https://github.com/CyrilB1531/lodestar/issues/208)'s
constraint that a lot must check its corpus reaches the code it is about.

## Decision

**1. The lineage stays a BPE model. No third tokenizer type.**
`LoadBpe` → `BpeVocabulary` → `BpeTokenizer`, because that is what `model.type` declares.
A reader who opens `tokenizer.json`, sees `"type": "BPE"` and calls `LoadBpe` is right,
and stays right.

The alternative — a `SentencePieceBpeTokenizer` named after the lineage rather than the
declared model — was argued from 0017 §3's own concern, that a reader picking the
nearest-sounding class gets silently wrong embeddings. It loses because that concern is
answered better by the file than by a class name: the file says BPE, and a type whose
name contradicts it moves the ambiguity rather than removing it. Two types would also
duplicate the merge loop, `fuse_unk`, added tokens and the byte-level machinery, all of
which this lineage shares unchanged.

**2. Metaspace is one internal transform, fed by either declaration.**
A `MetaspaceEscape` carrying the replacement character, the prepend scheme
(`never`/`first`/`always`) and `remove_extra_whitespaces`. It does one thing — prepend,
replace — and is testable alone. **The loader normalises both spellings into it**: the
`Metaspace` pre-tokenizer block and the `Prepend` + `Replace` normalizer sequence are two
writings of one value, and absorbing that variation is the loader's job, as it already is
for `Metaspace`'s pre-0.14 spelling.

`BpeVocabulary` gains a nullable `MetaspaceEscape?`; `null` is today's behaviour word for
word. `SentencePieceTokenizer` uses the same transform with `remove_extra_whitespaces`
set, which is what its current ten lines do and what neither of these two models
declares — so the flag is not decoration, it is the difference between the unigram path
and this one.

**3. `byte_fallback` is reproduced, not refused.**
0017 §3 refused it by name. That refusal ends for BPE models: an uncovered character
resolves into byte pieces rather than the unknown token. This is stated here as a
decision rather than deferred to the lot that implements it, because an ADR that kept the
refusal would describe an endpoint #175 contradicts.

**4. What stays refused, and this one is a decision too.**
A normalizer `Sequence` that is **not exactly** `Prepend` + `Replace` is refused, not
silently reduced to the two steps we reproduce. Three steps, refuse. This is 0017's rule
surviving while two of its clauses fall: refusing beats producing embeddings that are
quietly wrong, and loosening it here would empty it of meaning everywhere else.

## Consequences

**Sequencing: [#316](https://github.com/CyrilB1531/lodestar/issues/316), then
[#317](https://github.com/CyrilB1531/lodestar/issues/317), then
[#318](https://github.com/CyrilB1531/lodestar/issues/318).** The order is chosen by risk,
not by the parent's numbering. #316 carries an *extraction* — the transform leaves
`SentencePieceTokenizer` to be shared — and an extraction must return exactly the same
answers. The unigram oracles prove that for free, and only while nothing else moves in
the same lot. Landing it before any new behaviour keeps that witness.

**After #316 both files are still refused**, on `byte_fallback`; after #317 the refusal
goes. Each lot visibly moves the lock and none pretends to open the door alone. Saying so
here is what stops #316 reading as a failure.

**Testing.** #316: the unigram oracles unchanged, as the control; the BPE path exercised
through a directly constructed `BpeVocabulary`; the loader exercised by three synthetic
`tokenizer.json` fixtures — one declaring `Metaspace`, one declaring the normalizer
sequence, and **one with a three-step sequence that must be refused**. The third is the
one that matters: it proves the loosening is bounded. #317: `byte_fallback` on a
hand-built vocabulary. #318 is the only lot that needs the network.

**No benchmark row, in any of the four.** None of these is a performance lot, and a
comparison against Python belongs in the oracles rather than in a table of measurements.

**0017 is amended, not corrected.** Its §3 refused `byte_fallback` and recorded that
these two models had no path here; both clauses fall, and the index carries the relation
so a reader does not meet two accepted decisions where one denies what the other ships.
0017's body stands as written — an accepted decision is append-only, and what it said was
true when it was written.
