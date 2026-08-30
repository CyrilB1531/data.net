# 0062 — The two metaspace spellings part on the prepend, twice

**Status:** accepted · **Date:** 2026-08-30 · **Amends:** [`0050`](0050-the-sentencepiece-bpe-lineage-stays-a-bpe-model.md) §2

## Context

[0050 §2](0050-the-sentencepiece-bpe-lineage-stays-a-bpe-model.md) made the whitespace escape one
internal transform fed by either declaration:

> the `Metaspace` pre-tokenizer block and the `Prepend` + `Replace` normalizer sequence are two
> writings of one value, and absorbing that variation is the loader's job

That premise was read from the two files' fields — both say *prepend `▁`, replace spaces with
`▁`* — and not from `tokenizers` running. [#316](https://github.com/CyrilB1531/lodestar/issues/316)
built the transform on it, and the corpus that pins the lot is where it broke, in two places.

## What was measured

`tokenizers` 0.23.1, six pipelines over one model that splits at nothing else and declares the one
special token both target files declare, twelve texts each — `bpe_metaspace.json`. The pipelines
are compared to each other, with no Lodestar type involved.

**The guard.** `Metaspace` replaces, then prepends *unless the result already begins with the
replacement*; the normalizer `Sequence` runs `Prepend` before `Replace`, so it prepends before the
leading space has become a symbol and has nothing to guard on.

| text | `Metaspace{prepend_scheme: always}` | `Sequence[Prepend "▁", Replace " "→"▁"]` |
| --- | --- | --- |
| `the cat` | `▁the ▁cat` | `▁the ▁cat` |
| `" the cat"` | `▁the ▁cat` | `▁ ▁the ▁cat` |
| `▁the cat` | `▁the ▁cat` | `▁ ▁the ▁cat` |

Two of the corpus's nine texts without the added token fall on that side, and every other one is
identical — a leading space and a leading `▁` meet the guard alike, the first because the replace
has already run.

**The scheme.** `first` prepends to the opening *piece*, not to the whole text, and an added token
is a piece. The normalizer is not a pre-tokenizer and does not count pieces at all: it runs on
every gap the added tokens leave, so it prepends to each of them.

| text | `Metaspace{first}` | `Metaspace{always}` | the normalizer sequence |
| --- | --- | --- | --- |
| `<s>the cat` | `<s> the ▁cat` | `<s> ▁the ▁cat` | `<s> ▁the ▁cat` |
| `the cat<s>the cat` | `▁the ▁cat <s> the ▁cat` | `▁the ▁cat <s> ▁the ▁cat` | `▁the ▁cat <s> ▁the ▁cat` |
| `<s> the cat` | `<s> ▁the ▁cat` | `<s> ▁the ▁cat` | `<s> ▁ ▁the ▁cat` |

So `first` and `always` are two values, told apart by exactly the two texts where a token stands
before a gap — and the normalizer spelling sits on the `always` side, not the `first` side that
"a normalizer prepends once" suggested.

## Decision

**1. The equality of 0050 §2 holds, bounded, and the bounds are two fields the loader sets.**
`MetaspaceEscape` gains `SkipPrependWhenAlreadyPrefixed` — `true` for a `Metaspace` block, `false`
for the normalizer sequence and for the unigram path — and `Apply` takes the piece's position, so
`first` prepends to the opening piece alone. The normalizer sequence reads as `always`.
Everything else 0050 §2 says stands: one transform, one type, the loader absorbing the variation.
What falls is only its "two writings of one value" read without qualification.

The alternative — one behaviour for both spellings — was the cheaper diff and is wrong on both
counts: a shared guard breaks `prepend_replace_normalizer`, and a shared `first` breaks
`metaspace_always`. Two fields are what the reference itself carries.

**2. The scheme is read per piece, in `BpeTokenizer` rather than in the escape.**
The escape is a text transform and cannot know where its piece came from, so `Encode` tracks
whether anything has been emitted yet and hands that to `Apply`. An added token spends what
`first` owed the opening piece — measured above — and a gap a normalizer emptied does not, since
`tokenizers` drops an empty split.

**3. The divergence is closed rather than documented.**
Issue [#316](https://github.com/CyrilB1531/lodestar/issues/316) exists to load Llama-2 *and*
Mistral v0.1. A loader that reads Mistral's `Metaspace` block and answers with Llama-2's token
stream loads the file and produces different embeddings, which is the failure
[0017 §3](0017-bpe-parity-scope.md) refuses by name everywhere else. Mistral declares
`prepend_scheme: "first"` and three special tokens, so a prompt opening on `<s>` — which is every
prompt this lineage is given — is the case, not an edge of it. Shipping either divergence measured,
with a follow-up issue, was the option on the table; it loses because "refusing beats producing
embeddings that are quietly wrong" (0050 §4) has no weaker form for a file we accept.

## Consequences

`bpe_metaspace.json` grows an added token on its model and three texts that place it, and is
replayed whole — 72 pairs, every pipeline and every text. The corpus keeps the cross-checks that
establish both boundaries between its own cases with no Lodestar type involved, and the loader has
tests asserting the two spellings are reproduced *apart* where they part, so a field quietly
dropped fails there as well as against the reference.

The unigram path is untouched: it passes `always` and no guard, which is what
`SentencePieceTokenizer` has always done. Its own corpora carry no text beginning with a space or
the symbol, so they do not measure the guard either way — what keeps them the extraction's witness
(0050's Consequences) is that the values they run under are unchanged, not that they pin the new
field.

`docs/equivalence.md`'s `pre_tokenizers.Metaspace` row keeps one divergence, and it is on the
reading rather than the transform: `add_prefix_space: false` with no `prepend_scheme` is read here
as `never`, where `tokenizers` refuses the file. No oracle case can carry that shape, so
`BpeMetaspaceLoaderTests` pins it instead.

The decode side stays unreproduced and now says so where a caller reads it: a `Metaspace` decoder
block is accepted and not applied, so `Decode` returns the escaped text. This changes nothing for
the two lots that follow — both files are still refused on `byte_fallback` until #317 lands,
exactly as 0050's Consequences say, and #317 is where the decoder becomes a decision.
