# 0062 — The two metaspace spellings are one value, but for the prepend guard

**Status:** accepted · **Date:** 2026-08-30 · **Amends:** [`0050`](0050-the-sentencepiece-bpe-lineage-stays-a-bpe-model.md) §2

## Context

[0050 §2](0050-the-sentencepiece-bpe-lineage-stays-a-bpe-model.md) made the whitespace escape one
internal transform fed by either declaration:

> the `Metaspace` pre-tokenizer block and the `Prepend` + `Replace` normalizer sequence are two
> writings of one value, and absorbing that variation is the loader's job

That premise was read from the two files' fields — both say *prepend `▁`, replace spaces with
`▁`* — and not from `tokenizers` running. [#316](https://github.com/CyrilB1531/lodestar/issues/316)
built the transform on it, and the oracle corpus that pins the lot is where it broke.

## What was measured

`tokenizers` 0.23.1, six pipelines over one model, nine texts each — `bpe_metaspace.json`. The two
spellings are compared to each other with no Lodestar type involved:

| text | `Metaspace{prepend_scheme: first}` | `Sequence[Prepend "▁", Replace " "→"▁"]` |
| --- | --- | --- |
| `the cat` | `▁the ▁cat` | `▁the ▁cat` |
| `" the cat"` | `▁the ▁cat` | `▁ ▁the ▁cat` |
| `▁the cat` | `▁the ▁cat` | `▁ ▁the ▁cat` |

The two agree on every text that does not already begin with the replacement, and differ on every
text that does. Six of the corpus's 54 pairs fall on the second side; the other 48 are identical.

The reason is in the reference's own ordering. `Metaspace` replaces first and then prepends, and
guards that prepend on `starts_with(replacement)`; the normalizer `Sequence` runs `Prepend` before
`Replace`, so it prepends before the leading space has become a symbol and cannot guard on
anything. A leading space and a leading `▁` therefore meet the guard alike — the first because the
replace has already run, the second because it was written that way.

## Decision

**1. The equality of 0050 §2 holds, bounded, and the bound is a field.**
`MetaspaceEscape` gains a fourth value, `SkipPrependWhenAlreadyPrefixed`, set from which
declaration was read: `true` for a `Metaspace` block, `false` for the normalizer sequence and for
the unigram path, whose own oracles measure an unconditional prepend. Everything else 0050 §2 says
stands — one transform, one type, the loader absorbing the variation. What falls is only its "two
writings of one value" read without qualification.

The alternative — one guard for both spellings — was the cheaper diff and is wrong twice: it
breaks `prepend_replace_normalizer`, and on the unigram path it breaks the SentencePiece oracles
0050's Consequences name as the extraction's witness. A field is what the reference itself carries.

**2. The divergence is closed rather than documented.**
#316 exists to load Llama-2 *and* Mistral v0.1. A loader that reads Mistral's `Metaspace` block
and answers with Llama-2's token stream loads the file and produces different embeddings, which is
the failure [0017 §3](0017-bpe-parity-scope.md) refuses by name everywhere else. Shipping it
measured, with the corpus recording it and a follow-up issue, was the option on the table; it
loses because "refusing beats producing embeddings that are quietly wrong" (0050 §4) has no weaker
form for a file we accept.

## Consequences

`bpe_metaspace.json` is replayed whole — every pipeline, every text — where before six pairs were
measured as divergences instead. The corpus keeps the cross-check between its own two cases that
establishes the boundary without any Lodestar type, and the loader now has a test asserting the
two spellings are reproduced *apart* on a guarded text, so a guard quietly dropped fails there as
well as against the reference.

`docs/equivalence.md`'s `pre_tokenizers.Metaspace` row keeps one divergence, and it is on the
reading rather than the transform: `add_prefix_space: false` with no `prepend_scheme` is read here
as `never`, where `tokenizers` refuses the file. No oracle case can carry that shape, so
`BpeMetaspaceLoaderTests` pins it instead.

This changes nothing for #317 and #318: both files are still refused on `byte_fallback` until
#317, exactly as 0050's Consequences say.
