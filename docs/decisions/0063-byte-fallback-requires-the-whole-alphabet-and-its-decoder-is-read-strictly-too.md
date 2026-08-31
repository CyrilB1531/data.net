# 0063 — `byte_fallback` requires the whole alphabet, and its decoder is read strictly too

**Status:** accepted · **Date:** 2026-08-31 · **Amends:** [`0050`](0050-the-sentencepiece-bpe-lineage-stays-a-bpe-model.md) §3

## Context

[0050 §3](0050-the-sentencepiece-bpe-lineage-stays-a-bpe-model.md) decided *that* `byte_fallback`
is reproduced rather than refused, for the SentencePiece-BPE lineage Llama-2 and Mistral v0.1
belong to. It did not decide *what* to reproduce, and
[#317](https://github.com/CyrilB1531/lodestar/issues/317) is explicit that a boolean does not
express it: an uncovered character resolves into `<0x..>` byte pieces in Python where these
tokenizers emitted the unknown piece, and reproducing that needs the byte pieces to exist in the
vocabulary and a resolution order defined — neither of which a boolean expresses.

This decision states what was measured against `tokenizers` 0.23.1 and what `LoadBpe` now
enforces because of it.

## What was measured

Probed against hand-written `tokenizer.json` files over a four-piece BPE (`a`, `b`, `c`, `ab`)
plus a controlled set of `<0xXX>` pieces, then pinned end to end by
`tests/oracles/bpe_byte_fallback.json`. Every line below is a measurement, not a reading of the
Rust.

1. **The unit is the symbol, and the symbol is a code point.** A symbol absent from the
   vocabulary becomes one piece per UTF-8 byte of it: `é` → `<0xC3> <0xA9>`, `日` →
   `<0xE6> <0x97> <0xA5>`, an emoji → four. A symbol present is never expanded.
2. **It is all-or-nothing per symbol.** With `<0xC3>` present and `<0xA9>` missing, `é` is
   `['<unk>']`, not `<0xC3>` followed by an unknown. Each symbol decides for itself.
3. **The spelling is uppercase hexadecimal.** `<0xC3>` is the piece; a vocabulary spelling it
   `<0xc3>` resolves nothing and falls to `<unk>`.
4. **The expansion runs before the merges, and byte pieces are ordinary symbols.** Declaring the
   merge `<0xC3> <0xA9>` gives an uncovered `é` → `['<0xC3><0xA9>']`; a post-pass over
   unmergeable symbols could not produce that.
5. **The decorated symbol is what gets expanded.** With `continuing_subword_prefix: "##"`, an
   uncovered `é` after `a` expands its `##`-decorated form, so the `##` is itself encoded as two
   `#` bytes; an `end_of_word_suffix` of `"</w>"` is encoded the same way, as its four bytes.
   Neither target model declares either, but this is the rule that makes both statable: expand
   the string the symbol already is, decoration included.
6. **`fuse_unk` never fuses a byte-resolved symbol.** `aXXb` under `byte_fallback` with only
   `<0x58>` present and `fuse_unk: true` is `['a', '<0x58>', '<0x58>', 'b']` — the two pieces
   never merge into one fused unknown. Symbols that still fall to the unknown token keep fusing
   among themselves as before.

**A vocabulary declaring the flag without all 256 pieces is not refused by the reference.**
`tokenizers` degrades silently: a symbol whose bytes are not all covered falls to `<unk>`. With
`unk_token` absent it goes further and **drops the symbol entirely**, letting its neighbours merge
across the hole — `aXb` becomes `['ab']` on a vocabulary carrying no byte pieces and no unknown
token, not `['a', 'X', 'b']` or an error.

## Decision

**1. A vocabulary declaring `byte_fallback: true` must carry all 256 `<0xXX>` pieces, or the load
is refused, naming the first one missing.**

This is stricter than `tokenizers`, deliberately. It also settles the rest of the measurements for
free: with the complete alphabet no symbol ever falls to the unknown token, because every
character's UTF-8 bytes are all present — the two degradations above become unreachable, and so
does the upstream bug below.

The alternative — accept the partial alphabet and reproduce the degradation — loses on
[0050 §4](0050-the-sentencepiece-bpe-lineage-stays-a-bpe-model.md)'s own rule: refusing beats
producing embeddings that are quietly wrong, and a silently dropped symbol whose neighbours then
merge is as wrong as it gets. Reproducing the *ordering* bug below on top of that would mean
writing a known-wrong stream into an oracle and defending it later.

This rests on an assumption, named rather than assumed: SentencePiece writes the 256 byte pieces
into the vocabulary when a model is trained with `byte_fallback` — that is where the pieces come
from at all — so a checkpoint of this lineage carries them. That was not verifiable from the
session that wrote the originating spec, since no model artifact is committed here
(CONTRIBUTING.md) and the network was unreachable. If a real file ever turns out to omit one, the
refusal is what surfaces it, by name, at load.

**2. The declared `decoder` block is read strictly, but only when the model declares
`byte_fallback`.**

For such a file, `LoadBpe` reproduces exactly two shapes: a bare `{"type": "ByteFallback"}`, and a
`Sequence` of **exactly** `[Replace, ByteFallback, Fuse, Strip]`, in that order — the chain
Llama-2 declares. Any other length, order, repetition or step type is refused by name, which is
[0050 §4](0050-the-sentencepiece-bpe-lineage-stays-a-bpe-model.md)'s rule applied to the decode
side: the reference runs a `Sequence`'s steps in the declared order, so a reordered or padded one
decodes differently, and canonicalizing it here would be silently wrong. This closes what
[0062](0062-the-two-metaspace-spellings-part-on-the-prepend-twice.md) left open for this lineage:
`Decode` now undoes the byte pieces, and — for the four-step `Sequence` — the whitespace escape
alongside them.

**The strict reading is scoped to `byte_fallback`, narrower than the originating spec proposed.**
The spec framed the boundary as "`byte_fallback` or the metaspace escape"; that is not what
shipped, because the wider boundary would have newly refused a file this package accepts today —
one carrying the metaspace escape *without* `byte_fallback`, whose `decoder` declares
`{"type": "Metaspace"}`. A `Metaspace` decoder is reproducible in principle, but its
`prepend_scheme` and `split` fields are not measured against the reference on the decode side, so
refusing a shape this package could in principle reproduce is as wrong as accepting one it cannot.
That case therefore keeps today's behaviour exactly: the decoder is accepted and not applied, and
`Decode` returns the escaped text — which is what 0062 already documents. **This closes what 0062
left open for `byte_fallback` files only, not in general**; a `Metaspace`-only file's decoder
remains open, for a lot that measures it.

## The upstream ordering bug — measured, and not reproduced

**A byte-resolved symbol following an unknown one comes out in the wrong order.** Measured on a
vocabulary carrying only `<0x58>`:

| text | vocabulary | `tokenizers` 0.23.1 | offsets |
| --- | --- | --- | --- |
| `XY` | only `<0x58>` | `['<0x58>', '<unk>']` | `(0,1) (1,2)` |
| `YX` | only `<0x58>` | `['<0x58>', '<unk>']` | `(0,1) (1,2)` |
| `YYX` | only `<0x58>` | `['<unk>', '<0x58>', '<unk>']` | `(0,1) (1,2) (2,3)` |

`XY` and `YX` produce the identical stream, and both attribute `<0x58>` to the offset of the `Y`,
not the `X`. The pending unknown token is flushed after the byte-fallback branch runs rather than
before it. It reproduces with `fuse_unk` off, so it is not the fusing.

**It is not reproduced.** It is recorded here so a later reader does not rediscover it as ours: it
is unreachable once the byte alphabet is complete, which is exactly what decision 1 above
guarantees — there is no buggy region left to hit, so there is nothing here to reproduce or
diverge from.

## Consequences

`tests/oracles/bpe_byte_fallback.json` pins the six measurements above, a text per byte width
(ASCII, `é`, `日`, an emoji, a control character), `fuse_unk` on and off over the same texts, and
a decode column the metaspace corpus does not carry, since here the decoder is declared and
reproduced. The refusals — a missing `<0x00>`, a missing `<0xFF>`, a lowercase `<0xc3>`, and a
decoder shape outside the two reproduced — are pinned by loader tests rather than by oracles,
because `tokenizers` accepts every one of those files; only this package's stricter rule refuses
them.

`docs/reference/embeddings/persistence/tokenizerjsonloader-loadbpe.md`,
`docs/reference/embeddings/tokenization/bpetokenizer-decode.md` and
`docs/reference/embeddings/tokenization/bpevocabulary.md` are corrected in the same commit as this
decision: the first no longer says Llama-2 and Mistral v0.1 are refused, the second narrows its
round-trip qualification to the file shape that still lacks a decoder, and the third carries the
`ByteFallback` property.

**0050 is amended, not corrected.** Its §3 decided *that* `byte_fallback` is reproduced and said
nothing about the incomplete-alphabet case or the decoder; this decision adds both, and the index
carries the relation since 0050 is accepted and immutable. 0050's body stands as written.
