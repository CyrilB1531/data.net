# Design — #75: read the precompiled normalizer instead of refusing it

**Date:** 2026-08-06 · **Issue:** #75 · **Branch:** `feat/75-precompiled-normalizer` ·
**Checkout:** `/home/cyril/Documents/devs/data.net`

## The premise, measured before any design

Five real models, checked first:

`t5-small`, `albert-base-v2`, `camembert-base`, `xlm-roberta-base` and
`google/mt5-small` all declare `nmt_nfkc` and carry a **byte-identical**
237 539-byte `precompiled_charsmap` (same SHA-256). **None of them loaded.**

The only `spiece.model` this library could read was the one it had trained itself.
That is the real state of the "SentencePiece supported" claim.

## The route decision, and the measurement that settles it

Two routes:

- **A** — interpret the precompiled blob.
- **B** — reimplement `nmt_nfkc` on top of the runtime's NFKC.

The issue recommended A for coverage. True, but not decisive. What decides is this
measurement, taken with the whitespace flags off so only the map speaks, over all
149 251 assigned code points:

| `nmt_nfkc` vs Python's NFKC | Count | Examples |
| --- | ---: | --- |
| Dropped by the map, kept by NFKC | 30 | U+0001…U+001F |
| Turned into a space by the map | 15 | U+0009, U+1680, U+200B…U+200F, U+FEFF, U+FFFD, and U+2581 `▁` itself |
| **Kept by the map, changed by NFKC** | **136** | U+32FF `㋿`, U+A7F2…, the U+10780 block |

**181 divergences, 0.121 %** — and the third family is the argument. Those 136
code points were added to Unicode **after the map was compiled** (U+32FF in 12.1,
the rest in 14). The map is frozen at the Unicode version of the `sentencepiece`
build that produced it; `string.Normalize(FormKC)` follows the runtime's ICU.

So **Route B could not be byte-exact by construction, not by effort**: the gap
grows with every Unicode release, and differs between .NET versions for the same
input and the same file.

Recorded in ADR 0014.

## Decisions

### D1 — Implement the darts-clone trie walk

`PrecompiledNormalizer` reads the double-array trie and the NUL-terminated
replacements its values index, applying the same longest-match walk as
`sentencepiece`'s `Normalizer`.

### D2 — Validate as a Python prototype before any C# is written

The same walk reproduced `sp.normalize` on **all 1 112 064 code points**, 25
hand-picked sequences and 20 000 random strings, with no mismatch.

Getting the trie walk wrong in C# and debugging it against a 237 KB blob is a bad
place to be. Establishing the algorithm in the language that can talk to
`sentencepiece` directly costs an afternoon and removes the whole class of
uncertainty.

### D3 — Nothing is decided from `normalizer_spec.name`

What is refused becomes the case that cannot be **applied**, rather than the case
that was not **enumerated**:

- a normalizer named with no charsmap to apply it with;
- a charsmap that will not parse — refused **whole**, because half-normalized text
  is the same silent failure with a better disguise;
- `NFKC` in a `tokenizer.json`, which asks for the runtime's tables where the
  model asked for a frozen map.

### D4 — `tokenizer.json` reads `Precompiled` through the same class

It is the same blob, base64-encoded. Routing both formats through one
implementation means **the two can no longer disagree about the same model** —
which is the issue's "revisited in the same breath" criterion.

### D5 — Correct what the documentation said

Several places state that these normalizers are refused. They now load, and the
guides, `equivalence.md` and ADR 0013 say so.

## Out of scope

- Custom TSV normalizers not present in any of the five models.
- The whitespace flags, which are orthogonal and already handled.

## What "done" means

The five real models loading; the walk validated against `sp.normalize` over the
whole code-point space; refusals keyed on applicability rather than on a name;
both loaders sharing the implementation; ADR 0014 recording the Route A/B
measurement.
